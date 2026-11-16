using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadBuilderGame
{
    /// <summary>
    /// The one cheat / dev panel (replaces the old DBG overlay). IMGUI on purpose: it needs no prefab, stays out of the normal UI flow and cannot leak into screens.
    /// A small CHEAT button shows on Home and in the HUD; it opens a scrollable panel (drag to scroll on touch) with big touch targets.
    /// Created by RoadGameManager only when GameplayTuning.cheatsEnabled is true. That flag MUST be false (or this class compiled out) for a Play Store build.
    /// Every action goes through RoadGameManager.Cheat*; nothing is saved here and no PlayerPrefs are used.
    /// </summary>
    public sealed class CheatPanel : MonoBehaviour
    {
        private RoadGameManager _g;
        private bool _open;
        private string _section = "jump";
        private Vector2 _scroll;
        private string _metres = "1000";
        private bool _confirmReset;
        private string _note = "";

        private static readonly int[] Quick = { 500, 1000, 2000, 3000, 5000, 8000, 10000, 15000, 20000 };
        private static readonly string[] Sections = { "jump", "events", "hazards", "player", "economy", "vehicles", "worlds", "roads", "art", "save" };
        private static readonly string[] SectionNames = { "JUMP", "EVENTS", "HAZARDS", "PLAYER", "ECONOMY", "VEHICLES", "WORLDS", "ROADS", "ART", "SAVE" };
        private static readonly float[] Scales = { 0.25f, 0.5f, 1f, 2f, 3f, 4f };
        private static readonly string[] ScaleNames = { "0.25x", "0.5x", "1x", "2x", "3x", "4x" };

        private IReadOnlyList<StageEventDefinition> _events;
        private string[] _eventNames;
        private List<CatalogEntry> _veh, _wld, _srf;
        private IReadOnlyList<EaterVariantSO> _eaters;
        private IReadOnlyList<ProjectileDefinitionSO> _projs;
        private BossTierSO[] _tiers;
        private GUIStyle _btn, _lbl, _head, _box, _tog, _field, _on;
        private Texture2D _bg, _btnBg, _onBg, _hdBg, _chipBg;
        private GUIStyle _chip; private Font _uiFont;
        private bool _dragging; private float _dragStartY, _dragScroll0; private Vector2 _dragStart;

        /// <summary>Set by the UI layer: the themed display font's source font, so the chip uses the game's lettering (Game cannot see the UI theme).</summary>
        public static System.Func<Font> FontProvider;

        public void Init(RoadGameManager g) { _g = g; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f9Key.wasPressedThisFrame) Toggle();
        }

        private void Toggle()
        {
            _open = !_open;
            if (_open) Cache();
        }

        private void Cache()
        {
            _events = _g.CheatEventList();
            _eventNames = new string[_events.Count];
            for (int i = 0; i < _events.Count; i++) _eventNames[i] = _events[i].id;
            _veh = _g.CheatCatalog(CatalogKind.Vehicle); _wld = _g.CheatCatalog(CatalogKind.World); _srf = _g.CheatCatalog(CatalogKind.Surface);
            _eaters = EaterVariantSO.Defaults(); _projs = WorldProjectiles.Defaults(); _tiers = BossTierSO.LoadAll();
        }

        private static Texture2D Solid(Color c) { var t = new Texture2D(1, 1, TextureFormat.RGBA32, false); t.SetPixel(0, 0, c); t.Apply(); t.hideFlags = HideFlags.HideAndDontSave; return t; }

        private void Styles()
        {
            if (_btn != null) return;
            _bg = Solid(new Color(0.08f, 0.09f, 0.14f, 0.96f)); _btnBg = Solid(new Color(0.20f, 0.24f, 0.36f, 1f)); _onBg = Solid(new Color(0.16f, 0.55f, 0.38f, 1f)); _hdBg = Solid(new Color(0.95f, 0.65f, 0.15f, 1f));
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, wordWrap = true, alignment = TextAnchor.MiddleCenter };
            _btn.normal.background = _btnBg; _btn.hover.background = _btnBg; _btn.active.background = _hdBg; _btn.normal.textColor = Color.white; _btn.hover.textColor = Color.white; _btn.active.textColor = Color.black;
            _on = new GUIStyle(_btn); _on.normal.background = _onBg; _on.hover.background = _onBg;
            _head = new GUIStyle(_btn); _head.normal.background = _hdBg; _head.hover.background = _hdBg; _head.normal.textColor = Color.black; _head.hover.textColor = Color.black;
            _lbl = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, fontStyle = FontStyle.Bold }; _lbl.normal.textColor = new Color(1f, 0.88f, 0.55f);
            _box = new GUIStyle(GUI.skin.box); _box.normal.background = _bg;
            _field = new GUIStyle(GUI.skin.textField) { fontSize = 22, alignment = TextAnchor.MiddleCenter }; _field.normal.textColor = Color.white;
            _tog = _btn;
        }

        private void OnGUI()
        {
            if (_g == null || !_g.CheatsOn) return;
            bool show = _g.CheatOnHome || _g.CheatPlaying;
            if (!show && !_open) return;
            Styles();
            float s = Mathf.Clamp(Mathf.Max(Screen.width, Screen.height) / 1100f, 1f, 3f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1f));
            float vw = Screen.width / s, vh = Screen.height / s;
            var safe = Screen.safeArea; float left = safe.xMin / s + 6f;
            if (_uiFont == null && FontProvider != null) _uiFont = FontProvider();

            // The toggle is a small, semi-transparent chip (about 70 x 36 dp): top-left under PAUSE in a run, bottom-left on Home. Drawn unscaled, in screen pixels.
            var m = GUI.matrix; GUI.matrix = Matrix4x4.identity;
            float u = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 411f);            // pixels per dp on a 411 dp short-side phone
            float cs = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);                  // the UI canvas scale: PAUSE sits at (40, 128) in a 214 x 80 box
            float bw = 70f * u, bh = 36f * u;
            float bx = _g.CheatPlaying ? safe.xMin + 40f * cs : safe.xMin + 12f * u;
            float by = _g.CheatPlaying ? Screen.height - safe.yMax + 220f * cs : Screen.height - safe.yMin - bh - 12f * u;
            if (_chip == null) { _chip = new GUIStyle(_btn) { fontSize = 14, fontStyle = FontStyle.Bold }; _chipBg = Solid(new Color(0.12f, 0.15f, 0.24f, 0.55f)); _chip.normal.background = _chipBg; _chip.hover.background = _chipBg; _chip.normal.textColor = new Color(1f, 1f, 1f, 0.8f); _chip.hover.textColor = Color.white; if (_uiFont != null) _chip.font = _uiFont; }
            _chip.fontSize = Mathf.RoundToInt(13f * u);
            bool hit = GUI.Button(new Rect(bx, by, bw, bh), _open ? "CLOSE" : "CHEAT", _open ? _head : _chip);
            GUI.matrix = m;
            if (hit) Toggle();
            if (!_open) return;

            float pw = vw > vh ? Mathf.Min(vw * 0.62f, 640f) : vw - 12f;
            var panel = new Rect(left + 118f > vw - pw ? 6f : left + 118f, 8f, Mathf.Min(pw, vw - 12f), vh - 16f);   // beside the chip
            if (vw <= vh) panel = new Rect(6f, vh * 0.42f + 60f, vw - 12f, vh * 0.58f - 68f);
            GUI.Box(panel, GUIContent.none, _box);
            GUILayout.BeginArea(new Rect(panel.x + 8f, panel.y + 8f, panel.width - 16f, panel.height - 16f));
            GUILayout.Label(Status(), _lbl);
            // section tabs: two rows
            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                for (int i = row * 5; i < row * 5 + 5; i++)
                    if (GUILayout.Button(SectionNames[i], _section == Sections[i] ? _head : _btn, GUILayout.Height(44f))) { _section = Sections[i]; _scroll = Vector2.zero; _confirmReset = false; }
                GUILayout.EndHorizontal();
            }
            if (_note.Length > 0) GUILayout.Label(_note, _lbl);

            HandleDrag(new Rect(panel.x, panel.y, panel.width, panel.height));
            _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.ExpandHeight(true));
            switch (_section)
            {
                case "jump": Jump(); break;
                case "events": Events(); break;
                case "hazards": Hazards(); break;
                case "player": Player(); break;
                case "economy": Economy(); break;
                case "vehicles": Catalog(_veh, CatalogKind.Vehicle, _g.CheatVehicleId); break;
                case "worlds": Catalog(_wld, CatalogKind.World, _g.CheatWorldId); break;
                case "roads": Catalog(_srf, CatalogKind.Surface, _g.CheatSurfaceId); break;
                case "art": Art(); break;
                default: Save(); break;
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // Touch drag scrolls the list: buttons would swallow the drag, so a drag past 12 px takes over and the release is swallowed.
        private void HandleDrag(Rect area)
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown && area.Contains(e.mousePosition)) { _dragging = false; _dragStart = e.mousePosition; }
            else if (e.type == EventType.MouseDrag)
            {
                if (!_dragging && Mathf.Abs(e.mousePosition.y - _dragStart.y) > 12f) { _dragging = true; GUIUtility.hotControl = 0; }
                if (_dragging) { _scroll.y -= e.delta.y; e.Use(); }
            }
            else if (e.rawType == EventType.MouseUp && _dragging) { _dragging = false; e.Use(); }
        }

        private string Status()
        {
            return (_g.CheatPlaying ? "RUN " + Mathf.RoundToInt(_g.CheatDistance) + " m, hearts " + _g.CheatLives : "NOT IN A RUN (start one for run cheats)") + "  |  " + _g.CheatArtLabel;
        }

        private bool B(string t, float h = 46f) { return GUILayout.Button(t, _btn, GUILayout.Height(h)); }
        private bool Run() { if (_g.CheatPlaying) return true; _note = "Start a run first."; return false; }

        private bool Toggle(string label, bool on) { return GUILayout.Button((on ? "ON  " : "OFF  ") + label, on ? _on : _btn, GUILayout.Height(46f)); }

        private void Jump()
        {
            GUILayout.Label("Jump to a distance (flat runway, ink and hearts restored, events and boss tier for that distance)", _lbl);
            GUILayout.BeginHorizontal();
            _metres = GUILayout.TextField(_metres, 6, _field, GUILayout.Height(50f), GUILayout.Width(180f));
            if (B("GO", 50f) && Run() && int.TryParse(_metres, out int m)) _g.CheatJump(m);
            GUILayout.EndHorizontal();
            for (int i = 0; i < Quick.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                for (int k = i; k < i + 3 && k < Quick.Length; k++) if (B(Quick[k] + " m", 52f) && Run()) { _metres = Quick[k].ToString(); _g.CheatJump(Quick[k]); }
                GUILayout.EndHorizontal();
            }
        }

        private void Events()
        {
            if (B("END ACTIVE EVENT / HAZARDS") && Run()) _g.CheatEndEvents();
            GUILayout.Label("Start an event (" + _events.Count + ")", _lbl);
            for (int i = 0; i < _events.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int k = i; k < i + 2 && k < _events.Count; k++) if (B(_eventNames[k], 44f) && Run()) _g.CheatStartEvent(_events[k]);
                GUILayout.EndHorizontal();
            }
        }

        private void Hazards()
        {
            if (B("STOP ALL HAZARDS") && Run()) _g.CheatStopHazards();
            GUILayout.Label("Road-Eater (world variant)", _lbl);
            for (int i = 0; i < _eaters.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int k = i; k < i + 2 && k < _eaters.Count; k++) if (B(_eaters[k].displayName + " (" + _eaters[k].worldId + ")", 44f) && Run()) _g.CheatEater(_eaters[k].worldId);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("RO-B0 boss tier", _lbl);
            for (int i = 0; i < _tiers.Length; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int k = i; k < i + 2 && k < _tiers.Length; k++) if (B(_tiers[k].displayName + " (" + _tiers[k].minDistanceM + " m)", 44f) && Run()) _g.CheatBoss(_tiers[k]);
                GUILayout.EndHorizontal();
            }
            GUILayout.Label("Projectile (" + _projs.Count + ")", _lbl);
            for (int i = 0; i < _projs.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                for (int k = i; k < i + 2 && k < _projs.Count; k++) if (B(_projs[k].displayName + " (" + _projs[k].worldId + ")", 44f) && Run()) _g.CheatProjectile(_projs[k]);
                GUILayout.EndHorizontal();
            }
        }

        private void Player()
        {
            if (Toggle("God mode", _g.CheatGod)) _g.CheatGod = !_g.CheatGod;
            if (Toggle("Infinite ink", _g.CheatInfInk)) _g.CheatInfInk = !_g.CheatInfInk;
            if (Toggle("Infinite boost", _g.CheatInfBoost)) _g.CheatInfBoost = !_g.CheatInfBoost;
            if (B("REFILL ink, hearts, boost") && Run()) _g.CheatRefill();
            GUILayout.Label("Hearts", _lbl);
            GUILayout.BeginHorizontal();
            for (int n = 1; n <= 5; n++) if (B(n.ToString(), 46f)) _g.CheatSetHearts(n);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (int n = 6; n <= 9; n++) if (B(n.ToString(), 46f)) _g.CheatSetHearts(n);
            GUILayout.EndHorizontal();
            GUILayout.Label("Speed x" + _g.CheatSpeedValue.ToString("0.##"), _lbl);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Scales.Length; i++) if (GUILayout.Button(ScaleNames[i], Mathf.Approximately(_g.CheatSpeedValue, Scales[i]) ? _on : _btn, GUILayout.Height(46f))) _g.CheatSpeedValue = Scales[i];
            GUILayout.EndHorizontal();
            GUILayout.Label("Time scale x" + _g.CheatTimeValue.ToString("0.##"), _lbl);
            GUILayout.BeginHorizontal();
            for (int i = 0; i < Scales.Length; i++) if (GUILayout.Button(ScaleNames[i], Mathf.Approximately(_g.CheatTimeValue, Scales[i]) ? _on : _btn, GUILayout.Height(46f))) _g.CheatTimeValue = Scales[i];
            GUILayout.EndHorizontal();
        }

        private void Economy()
        {
            GUILayout.Label("Coins", _lbl);
            GUILayout.BeginHorizontal();
            if (B("+100")) _g.CheatAddCoins(100); if (B("+1000")) _g.CheatAddCoins(1000); if (B("+10000")) _g.CheatAddCoins(10000); if (B("+100000")) _g.CheatAddCoins(100000);
            GUILayout.EndHorizontal();
            GUILayout.Label("Gems", _lbl);
            GUILayout.BeginHorizontal();
            if (B("+10")) _g.CheatAddGems(10); if (B("+100")) _g.CheatAddGems(100); if (B("+1000")) _g.CheatAddGems(1000);
            GUILayout.EndHorizontal();
            GUILayout.Label("Scrap", _lbl);
            GUILayout.BeginHorizontal();
            if (B("+10")) _g.CheatAddScrap(10); if (B("+100")) _g.CheatAddScrap(100); if (B("+1000")) _g.CheatAddScrap(1000);
            GUILayout.EndHorizontal();
            if (B("UNLOCK ALL vehicles, worlds, roads")) { _g.CheatUnlockAll(); _note = "Everything unlocked."; }
        }

        private void Catalog(List<CatalogEntry> list, CatalogKind kind, string selected)
        {
            if (list == null) return;
            GUILayout.Label((kind == CatalogKind.Vehicle ? "Vehicle" : kind == CatalogKind.World ? "World" : "Road") + " (" + list.Count + "): USE = unlock and switch now" + (kind == CatalogKind.Vehicle ? "" : ""), _lbl);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i]; bool unlocked = _g.CheatIsUnlocked(kind, e.id);
                GUILayout.BeginHorizontal();
                GUILayout.Label((e.id == selected ? "> " : "") + e.displayName, _lbl, GUILayout.Width(240f));
                if (!unlocked && GUILayout.Button("UNLOCK", _btn, GUILayout.Height(44f), GUILayout.Width(110f))) _g.CheatUnlock(kind, e.id);
                if (unlocked) GUILayout.Label("open", _lbl, GUILayout.Width(110f));
                if (GUILayout.Button("USE", e.id == selected ? _on : _btn, GUILayout.Height(44f))) _g.CheatSelect(kind, e.id);
                GUILayout.EndHorizontal();
            }
        }

        private void Art()
        {
            GUILayout.Label("Art style for vehicles, pickups, worlds and roads (rebuilds the run)", _lbl);
            if (B("DEFAULT (smooth where it exists)")) _g.CheatSetArt(null);
            if (B("SMOOTH")) _g.CheatSetArt(ArtStyle.Smooth);
            if (B("PIXEL")) _g.CheatSetArt(ArtStyle.Pixel);
        }

        private void Save()
        {
            GUILayout.Label("Reset save: deletes coins, unlocks, progress and best distance, then restarts.", _lbl);
            if (!_confirmReset) { if (B("RESET SAVE...")) _confirmReset = true; return; }
            GUILayout.Label("Really reset everything?", _lbl);
            GUILayout.BeginHorizontal();
            if (B("YES, RESET")) { _confirmReset = false; _g.CheatResetSave(); }
            if (B("CANCEL")) _confirmReset = false;
            GUILayout.EndHorizontal();
        }
    }
}
