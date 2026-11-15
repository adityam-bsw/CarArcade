using System;
using System.Collections.Generic;

namespace RoadBuilderGame
{
    /// <summary>
    /// RO-B0 announcer (voice-lines.md sections 4 to 6): one voice channel, priority P0 to P4 (lower number wins), same-key cooldowns, a 0.4 s global gap, banter budgets,
    /// telegraph lock and subtitles. Low priority is dropped, never delayed. No recordings exist yet, so a cue is a subtitle plus its tell SFX (which is the audio layer's job);
    /// when <see cref="PlayVoice"/> finds a recorded clip the subtitle uses its length instead. Pure C#, unscaled time, no allocation after construction.
    /// </summary>
    public sealed class Announcer
    {
        /// <summary>Plays the tell or result SFX for the cue id (the audio layer maps ids to sounds). Always called, even when the line is dropped, for P0 and P1.</summary>
        public Action<string> PlaySfx = id => { };
        /// <summary>Tries to play a recorded line for the cue id; returns its length in seconds, or 0 when no clip exists.</summary>
        public Func<string, float> PlayVoice = id => 0f;
        /// <summary>text, seconds, urgent</summary>
        public Action<string, float, bool> Subtitle = (t, s, u) => { };
        /// <summary>A telegraph is armed or active (projectile tell, event warn, Eater outside Calm, boss pattern): P3 and P4 are dropped.</summary>
        public Func<bool> ThreatActive = () => false;
        public bool SubtitlesOn = true, VoiceOn = true;

        public const float GlobalGap = 0.4f, WaitUnder = 0.3f, StaleP3 = 0.5f;
        private float _clock, _playEnd = -10f, _gapEnd = -10f, _lastBanter = -100f, _lastReactive = -100f;
        private int _playPri = 9, _banterUsed;
        private string _pendingId; private float _pendingUntil;
        private readonly Dictionary<string, float> _lastKey = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _lastVariant = new Dictionary<string, int>();
        private readonly Pcg32 _rng = new Pcg32(0xA11CE5u);

        public string LastSubtitle { get; private set; }
        public int Spoken { get; private set; }
        public int Dropped { get; private set; }
        public bool Playing => _clock < _playEnd;

        public void Reset() { _clock = 0f; _playEnd = _gapEnd = -10f; _lastBanter = _lastReactive = -100f; _playPri = 9; _banterUsed = 0; _pendingId = null; _lastKey.Clear(); _lastVariant.Clear(); Spoken = Dropped = 0; LastSubtitle = null; }

        /// <summary>Death or restart: all voice stops and the queue clears (rule 6).</summary>
        public void Clear() { _playEnd = -10f; _playPri = 9; _pendingId = null; }

        public void Tick(float unscaledDt)
        {
            _clock += unscaledDt;
            if (_pendingId != null && _clock >= _gapEnd && _clock <= _pendingUntil) { var id = _pendingId; _pendingId = null; Say(id); }
            else if (_pendingId != null && _clock > _pendingUntil) { _pendingId = null; Dropped++; }
        }

        /// <summary>A cue id fired by the game. Ids without a row are sound-only and pass straight to the SFX channel.</summary>
        public bool Say(string cueId)
        {
            if (string.IsNullOrEmpty(cueId)) return false;
            var row = VoiceLines.Find(cueId);
            if (row == null) { PlaySfx(cueId); return false; }
            bool safety = row.Priority <= 1;
            if (safety) PlaySfx(cueId);                                  // the tell SFX is its own channel and never depends on the voice (rule 4)
            if (!Allowed(row)) { Dropped++; if (!safety) { } return false; }

            float len = VoiceOn ? PlayVoice(cueId) : 0f;
            if (!safety) PlaySfx(cueId);
            int v = PickVariant(row);
            float secs = len > 0f ? len : row.MaxSeconds;
            _playEnd = _clock + secs; _gapEnd = _playEnd + GlobalGap; _playPri = row.Priority; _lastKey[cueId] = _clock;
            if (row.Priority == 4) { _lastBanter = _clock; _banterUsed++; } else if (row.Priority == 3) _lastReactive = _clock;
            LastSubtitle = row.Text[v]; Spoken++;
            if (SubtitlesOn) Subtitle(row.Text[v], Math.Max(1f, secs + 0.3f), row.Priority == 0);
            return true;
        }

        private bool Allowed(VoiceLine row)
        {
            if (_lastKey.TryGetValue(row.Id, out var t) && _clock - t < row.Cooldown) return false;     // same key cooldown
            int pri = row.Priority;
            if (pri >= 3 && ThreatActive()) return false;                                                  // telegraph lock; no joke masks a hazard
            if (pri == 3 && _clock - _lastReactive < 6f) return false;
            if (pri == 4 && (_clock - _lastBanter < 15f || _banterUsed >= 6)) return false;
            bool busy = _clock < _playEnd;
            if (busy)
            {
                if (pri > _playPri) return false;                                                           // lower priority is dropped, not delayed
                if (pri == _playPri && pri > 0)
                {
                    if (_playEnd - _clock < WaitUnder) { _pendingId = row.Id; _pendingUntil = _playEnd + GlobalGap + 0.3f; }
                    return false;
                }
                return true;                                                                                // higher priority (or P0 vs P0, a different tell) cuts the current line
            }
            return _clock >= _gapEnd || pri == 0;                                                           // global gap, except P0
        }

        private int PickVariant(VoiceLine row)
        {
            int n = row.Text.Length; if (n == 1) return 0;
            int i = _rng.NextInt(n);
            if (_lastVariant.TryGetValue(row.Id, out var last) && last == i) i = (i + 1) % n;              // never the same variant twice in a row
            _lastVariant[row.Id] = i; return i;
        }
    }
}
