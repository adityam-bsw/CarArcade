using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Cheat / dev actions for the in-game panel (<see cref="CheatPanel"/>). Everything here is inert unless GameplayTuning.cheatsEnabled is true
    /// (it must be false, or the panel compiled out, in a release build: Docs/design/PLAY_STORE_READINESS.md). Toggles are session-only: no PlayerPrefs, nothing saved
    /// except what the normal profile already holds (coins, gems, scrap, unlocks).
    /// </summary>
    public partial class RoadGameManager
    {
        public bool CheatsOn => _tuning != null && _tuning.cheatsEnabled;

        private bool _cGod, _cInk, _cBoost, _cEaterOwn;
        private float _cSpeed = 1f, _cTime = 1f;
        private ArtStyle? _cArt;
        private List<StageEventDefinition> _cEvents;

        public bool CheatGod { get => CheatsOn && _cGod; set => _cGod = value; }
        public bool CheatInfInk { get => CheatsOn && _cInk; set => _cInk = value; }
        public bool CheatInfBoost { get => CheatsOn && _cBoost; set => _cBoost = value; }
        public float CheatSpeed => CheatsOn ? _cSpeed : 1f;
        public float CheatTime => CheatsOn ? _cTime : 1f;
        public float CheatSpeedValue { get => _cSpeed; set => _cSpeed = Mathf.Clamp(value, 0.25f, 4f); }
        public float CheatTimeValue { get => _cTime; set { _cTime = Mathf.Clamp(value, 0.25f, 4f); if (_state == ScreenState.Playing && !_paused) ApplyTimeScale(_run.TimeScale * CheatTime); } }
        public ArtStyle? CheatArtOverride => CheatsOn ? _cArt : null;

        public bool CheatPlaying => _state == ScreenState.Playing && _car != null;
        public bool CheatOnHome => _state == ScreenState.HomeDashboard;
        public float CheatDistance => _distance;
        public int CheatLives => currentLives;
        public string CheatWorldId => _selWorldId;
        public string CheatVehicleId => _selVehicleId;
        public string CheatSurfaceId => _selSurfaceId;
        public string CheatArtLabel => _cArt.HasValue ? _cArt.Value.ToString() : "Default (Smooth)";

        // ---------------------------------------------------------------- jump
        /// <summary>Lays a flat runway at <paramref name="metres"/> and replays the director there. Past the slice gate the endless ladder takes over from that distance, so events, boss tier and stage index are the ones for that distance.</summary>
        public void CheatJump(float metres)
        {
            if (!CheatsOn || !CheatPlaying || _stage == null || !_stage.Active) return;
            metres = Mathf.Max(0f, metres);
            if (!_stage.OnEndless && _endlessLadder != null && metres >= FinishGateM() - 20f)
            {
                _sliceCleared = true;
                _worldCtx = BuildWorldContext(_endlessLadder);
                _stage.ContinueEndless(_endlessLadder, metres, _worldCtx);
            }
            JumpRun(metres);
            var rec = _profile.lastStageRun; rec.debugJumped = true; _profile.lastStageRun = rec;
            ShowStageBanner("CHEAT JUMP " + Mathf.RoundToInt(metres) + " m");
        }

        // ---------------------------------------------------------------- stage events
        public IReadOnlyList<StageEventDefinition> CheatEventList()
        {
            if (_cEvents != null) return _cEvents;
            _cEvents = new List<StageEventDefinition>();
            var seen = new HashSet<string>();
            foreach (var d in Resources.LoadAll<StageEventDefinition>("Stage"))
                if (d != null && !string.IsNullOrEmpty(d.id) && seen.Add(d.id)) _cEvents.Add(d);
            _cEvents.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            return _cEvents;
        }

        /// <summary>Replaces the live plan with this one event 110 m ahead and drives to its warning, so the real signals, behaviour and banners run.</summary>
        public void CheatStartEvent(StageEventDefinition d)
        {
            if (!CheatsOn || !CheatPlaying || d == null || !_stage.Active) return;
            float start = _distance + 110f;
            _stage.Director.DebugInject(d, start, true);
            JumpRun(start - 95f);
            ShowStageBanner("CHEAT EVENT " + d.id);
        }

        /// <summary>Ends whatever event is active (jumps past its end so the real end path runs).</summary>
        public void CheatEndEvents()
        {
            if (!CheatsOn || !CheatPlaying || !_stage.Active) return;
            float to = _distance;
            var ev = _stage.Director.Plan.Events;
            for (int i = 0; i < ev.Count; i++) if (ev[i].Phase == StagePhase.Active || ev[i].Phase == StagePhase.Warning) to = Mathf.Max(to, ev[i].EndM + 2f);
            if (to > _distance) JumpRun(to);
            _cEaterOwn = false; _eater.Stop(); _boss.Stop(); _announcer?.Clear();
        }

        // ---------------------------------------------------------------- hazards
        public void CheatEater(string worldId)
        {
            if (!CheatsOn || !CheatPlaying || !_stage.Active) return;
            _boss.Stop(); _eater.Stop(); _stagePresenter?.Reset();         // the cheat hazard has no stage event: drop the stale 'N M LEFT' pill
            _hazardWorldId = worldId;
            _eater.Configure(EaterVariantSO.ForWorld(worldId));
            float cx = _car.Body.position.x;
            _eater.Begin(cx, 0x00E47E8u ^ (uint)Time.frameCount);       // own origin = the car now, so its timers and timeout (spawn + 750 m) are relative to the current distance
            _eater.Model.TimeoutOverrideS = 750f;
            _cEaterOwn = true;
            _eater.StartChase(cx);
        }

        public void CheatBoss(BossTierSO tier)
        {
            if (!CheatsOn || !CheatPlaying || !_stage.Active || tier == null) return;
            _eater.Stop(); _stagePresenter?.Reset();
            if (string.IsNullOrEmpty(_hazardWorldId)) _hazardWorldId = HazardWorlds.Resolve(_selWorldId, IsCountrysideWorld(), _stageOn);
            _boss.Start(_car.Body.position, tier.minDistanceM, _hazardWorldId);
        }

        public void CheatProjectile(ProjectileDefinitionSO def)
        {
            if (!CheatsOn || !CheatPlaying || !_stage.Active || def == null) return;
            _boss.CheatFire(def, _car.Body.position, _car.Body.linearVelocity);
        }

        public void CheatStopHazards() { if (!CheatsOn || !CheatPlaying) return; _cEaterOwn = false; _eater.Stop(); _boss.Stop(); _announcer?.Clear(); _stagePresenter?.Reset(); }

        // ---------------------------------------------------------------- player
        public void CheatSetHearts(int n)
        {
            if (!CheatsOn) return;
            n = Mathf.Clamp(n, 1, 9);
            maxLives = Mathf.Max(maxLives, n);
            currentLives = n;
            if (CheatPlaying) UpdateHeartsUI();
            SyncModel();
        }

        public void CheatRefill() { if (!CheatsOn || !CheatPlaying) return; currentInk = maxInk; currentLives = maxLives; _run.Boost01 = 1f; UpdateHeartsUI(); UpdateInkUI(); }

        public void CheatAddCoins(int n) { if (!CheatsOn) return; totalCoins += n; _profile.coins = totalCoins; SavePlayerData(); SyncModel(); }
        public void CheatAddGems(int n) { if (!CheatsOn) return; _profile.gems += n; SavePlayerData(); SyncModel(); }
        public void CheatAddScrap(int n) { if (!CheatsOn) return; _profile.scrap += n; SavePlayerData(); SyncModel(); }

        // ---------------------------------------------------------------- unlocks and selection
        public bool CheatIsUnlocked(CatalogKind kind, string id)
        {
            switch (kind)
            {
                case CatalogKind.Vehicle: return _unlockedVehicleIds.Contains(id);
                case CatalogKind.World: return _profile.unlockedWorldIds != null && _profile.unlockedWorldIds.Contains(id);
                default: return _profile.unlockedSurfaceIds != null && _profile.unlockedSurfaceIds.Contains(id);
            }
        }

        public void CheatUnlock(CatalogKind kind, string id, bool save = true)
        {
            if (!CheatsOn || string.IsNullOrEmpty(id)) return;
            switch (kind)
            {
                case CatalogKind.Vehicle: _unlockedVehicleIds.Add(id); break;
                case CatalogKind.World: if (_profile.unlockedWorldIds == null) _profile.unlockedWorldIds = new List<string>(); if (!_profile.unlockedWorldIds.Contains(id)) _profile.unlockedWorldIds.Add(id); break;
                default: if (_profile.unlockedSurfaceIds == null) _profile.unlockedSurfaceIds = new List<string>(); if (!_profile.unlockedSurfaceIds.Contains(id)) _profile.unlockedSurfaceIds.Add(id); break;
            }
            if (save) { SavePlayerData(); SyncModel(); }
        }

        public void CheatUnlockAll()
        {
            if (!CheatsOn) return;
            var cat = AssetCatalog.Load(); if (cat == null) return;
            foreach (var e in cat.entries) if (e != null && e.playable) CheatUnlock(e.kind, e.id, false);
            SavePlayerData(); SyncModel();
        }

        /// <summary>Playable catalogue entries of a kind (the cheat panel lists these).</summary>
        public List<CatalogEntry> CheatCatalog(CatalogKind kind)
        {
            var l = new List<CatalogEntry>(); var cat = AssetCatalog.Load();
            if (cat != null) foreach (var e in cat.entries) if (e != null && e.kind == kind && e.playable) l.Add(e);
            return l;
        }

        /// <summary>Unlocks and selects a vehicle, world or surface. During a run the run is rebuilt at the same distance (car, road, background) with hearts, ink and session coins kept.</summary>
        public void CheatSelect(CatalogKind kind, string id)
        {
            if (!CheatsOn) return;
            CheatUnlock(kind, id, false);
            SelectCatalogItem(kind, id);
            if (CheatPlaying) CheatRebuildRun();
        }

        private void CheatRebuildRun()
        {
            float d = _distance; int lives = currentLives; int coins = sessionCoins; float ink01 = maxInk > 0f ? currentInk / maxInk : 1f;
            SwitchScreen(ScreenState.Playing);              // full run start: car, road skin, background, pickups, hazards for the new selection
            sessionCoins = coins;
            if (d > 1f) CheatJump(d);
            currentLives = Mathf.Min(lives, maxLives); currentInk = maxInk * ink01;
            UpdateHeartsUI(); UpdateInkUI();
        }

        // ---------------------------------------------------------------- art style, save
        public void CheatSetArt(ArtStyle? style)
        {
            if (!CheatsOn) return;
            _cArt = style;
            ThemeArtStyles.Override(EnvironmentTheme.Country, style); ThemeArtStyles.Override(EnvironmentTheme.Desert, style); ThemeArtStyles.Override(EnvironmentTheme.CyberNight, style);
            _worldSkins.Clear(); _surfaceSkins.Clear();
            if (CheatPlaying) CheatRebuildRun();
            else { ApplyWorld(); ApplySurface(); if (_state == ScreenState.HomeDashboard || _state == ScreenState.GarageMenu || _state == ScreenState.StageSelect) SetupShowcaseCar(); }
        }

        /// <summary>Writes a fresh default profile and restarts from the Boot scene.</summary>
        public void CheatResetSave()
        {
            if (!CheatsOn) return;
            SaveService.Save(new PlayerProfile());
            Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
