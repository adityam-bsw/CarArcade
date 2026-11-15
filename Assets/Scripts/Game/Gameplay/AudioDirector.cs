using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Decides what the player hears: music per screen and world, sound effects and robot lines for every run cue.
    /// Plain class owned by the manager. It reads events and calls <see cref="SoundManager"/>; it holds no game rules.
    /// </summary>
    public sealed class AudioDirector
    {
        private SoundManager S => SoundManager.Instance;
        private bool _lowInkSaid;

        public void Bind(RunDirector run) { run.CueRaised += OnCue; }

        /// <summary>Music follows the screen: menus play the menu loop, the world door and the race play that world's track.</summary>
        public void OnScreen(ScreenState state, string worldId)
        {
            var s = S; if (s == null) return;
            switch (state)
            {
                case ScreenState.StageSelect:
                case ScreenState.Playing:
                    s.PlayMusic(string.IsNullOrEmpty(worldId) ? "menu" : worldId);
                    s.DuckMusic(false);
                    break;
                case ScreenState.GameOver:
                    s.DuckMusic(true);
                    break;
                default:
                    s.PlayMusic("menu");
                    s.DuckMusic(false);
                    break;
            }
            if (state != ScreenState.Playing) s.SetMusicIntensity(0f);
            if (state == ScreenState.HomeDashboard) s.Say("welcome", false, 60f);
            if (state == ScreenState.Rewards) s.Say("daily", false, 60f);
        }

        /// <summary>The player picked another world on the world screen: preview its track.</summary>
        public void OnWorldChosen(string worldId)
        {
            if (S != null && !string.IsNullOrEmpty(worldId)) S.PlayMusic(worldId);
        }

        public void RunStart()
        {
            _lowInkSaid = false;
            var s = S; if (s == null) return;
            s.PlaySfx("go", 0.8f);
            s.Say("start", true);
        }

        public void Crash(bool shielded)
        {
            var s = S; if (s == null) return;
            if (shielded) { s.PlaySfx("shield", 0.9f); return; }
            s.PlaySfx("crash", 0.9f);
            s.Say("crash", false, 6f);
        }

        public void GameOver(bool newBest)
        {
            var s = S; if (s == null) return;
            s.SetEngineRunning(false, 0f);
            s.SetMusicIntensity(0f);
            s.PlaySfx("gameover", 0.9f);
            s.Say(newBest ? "newbest" : "gameover", true);
        }

        public void MeteorWarning() { var s = S; if (s == null) return; s.PlaySfx("meteor_warn", 0.8f); s.Say("meteor", false, 20f); }
        /// <summary>A stage-event cue by registry id (Docs/design/voice-lines.md 3.3). Placeholder sounds until real audio exists; unknown ids are silent.</summary>
        public void StageCue(string cueId)
        {
            var s = S; if (s == null) return;
            StageCues.Placeholder(cueId, out string sfx, out string voice, out float vol);
            if (sfx != null) s.PlaySfx(sfx, vol);
            if (voice != null) s.Say(voice, false, 20f);
        }

        public void MeteorImpact() { var s = S; if (s != null) s.PlaySfx("explosion", 0.9f); }

        /// <summary>Call every frame while racing with the ink tank as a fraction (0 to 1).</summary>
        public void InkLevel(float ink01)
        {
            var s = S; if (s == null) return;
            if (ink01 < 0.2f && !_lowInkSaid)
            {
                _lowInkSaid = true;
                s.PlaySfx("lowink", 0.8f);
                s.Say(ink01 <= 0.02f ? "noink" : "lowink", false, 14f);
            }
            else if (ink01 > 0.35f) _lowInkSaid = false;
        }

        private void OnCue(RunDirector.Cue cue)
        {
            var s = S; if (s == null) return;
            switch (cue)
            {
                case RunDirector.Cue.Boost: s.PlaySfx("boost", 0.9f); s.Say("boost", false, 12f); break;
                case RunDirector.Cue.Shield: s.PlaySfx("shield", 0.8f); s.Say("shield"); break;
                case RunDirector.Cue.Magnet: s.PlaySfx("magnet", 0.8f); s.Say("magnet"); break;
                case RunDirector.Cue.Double: s.PlaySfx("x2", 0.8f); s.Say("double"); break;
                case RunDirector.Cue.SlowMo: s.PlaySfx("slowmo", 0.8f); s.Say("slowmo"); break;
                case RunDirector.Cue.Ghost: s.PlaySfx("ghost", 0.8f); s.Say("ghost"); break;
                case RunDirector.Cue.Gem: s.PlaySfx("gem", 0.8f); break;
                case RunDirector.Cue.Ring: s.PlaySfx("ring", 0.8f, 0.05f); break;
                case RunDirector.Cue.Flip: s.PlaySfx("flip", 0.8f); s.Say("flip", false, 15f); break;
                case RunDirector.Cue.BigAir: s.Say("bigair", false, 20f); break;
                case RunDirector.Cue.Combo: s.PlaySfx("combo_up", 0.7f); s.Say("combo", false, 25f); break;
                case RunDirector.Cue.Chest: s.PlaySfx("chest", 0.9f); s.Say("chest", false, 10f); break;
                case RunDirector.Cue.NewBest: s.PlaySfx("newbest", 0.9f); s.Say("newbest", true); break;
                case RunDirector.Cue.NearMiss: s.PlaySfx("nearmiss", 0.9f); s.Say("nearmiss", false, 10f); break;
                case RunDirector.Cue.FrenzyOn: s.PlaySfx("frenzy_on", 1f); s.Say("frenzy", true); s.SetMusicIntensity(1f); break;
                case RunDirector.Cue.FrenzyOff: s.PlaySfx("frenzy_off", 0.8f); s.SetMusicIntensity(0f); break;
            }
        }
    }
}
