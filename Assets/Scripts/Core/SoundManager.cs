using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// All sound output: one-shot SFX by name, looping engine, crossfading music (one track per world) and the robot announcer.
    /// Knows nothing about gameplay; <c>AudioDirector</c> (Game layer) tells it what to play.
    /// Clips live in Resources/RoadBuilder/Audio/{Sfx,Music,VO}. A missing clip is silently skipped.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private const string Root = "RoadBuilder/Audio/";
        public float MusicVolume = 0.5f;
        public float VoVolume = 1f;

        private AudioSource _sfxSource, _engineSource, _drawSource, _voSource;
        private AudioSource _musicA, _musicB;
        private AudioSource _musicLive;               // the source currently fading in / playing
        private string _musicKey = "";
        private float _duck = 1f, _duckTarget = 1f;   // music duck while the announcer talks or a stinger plays
        private float _pitchTarget = 1f, _unduckAt;
        private readonly Dictionary<string, AudioClip> _sfx = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AudioClip[]> _vo = new Dictionary<string, AudioClip[]>();
        private readonly Dictionary<string, float> _voLast = new Dictionary<string, float>();
        private float _voBusyUntil;
        private int _lastVoIndex = -1;
        private string _lastVoKey = "";

        public void Init()
        {
            Instance = this;
            _sfxSource = NewSource(false, 1f);
            _engineSource = NewSource(true, 0.25f);
            _drawSource = NewSource(false, 0.4f);
            _voSource = NewSource(false, 1f);
            _musicA = NewSource(true, 0f);
            _musicB = NewSource(true, 0f);
            _musicA.ignoreListenerPause = _musicB.ignoreListenerPause = true;

            var eng = Resources.Load<AudioClip>(Root + "Sfx/engine") ?? Resources.Load<AudioClip>(Root + "engine");
            if (eng != null) _engineSource.clip = eng;
        }

        private AudioSource NewSource(bool loop, float volume)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.volume = volume;
            s.spatialBlend = 0f;
            return s;
        }

        // ---------------------------------------------------------------- SFX

        private AudioClip Sfx(string key)
        {
            if (_sfx.TryGetValue(key, out var c)) return c;
            c = Resources.Load<AudioClip>(Root + "Sfx/" + key) ?? Resources.Load<AudioClip>(Root + key);
            _sfx[key] = c;
            return c;
        }

        /// <summary>Plays a sound effect by file name. <paramref name="pitchJitter"/> varies the pitch a little so repeats do not sound identical.</summary>
        public void PlaySfx(string key, float volume = 1f, float pitchJitter = 0f)
        {
            var clip = Sfx(key);
            if (clip == null || _sfxSource == null) return;
            // One shared source: pitch applies to everything playing on it, so only jitter on rare sounds.
            _sfxSource.pitch = pitchJitter > 0f ? 1f + Random.Range(-pitchJitter, pitchJitter) : 1f;
            _sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayCoin() => PlaySfx("coin", 0.7f, 0.04f);
        public void PlayCrash() => PlaySfx("crash", 0.9f);
        public void PlayHeart() => PlaySfx("heart", 0.85f);
        public void PlayClick() => PlaySfx("click", 0.6f);
        public void PlayUpgrade() => PlaySfx("upgrade", 0.8f);

        public void PlayDraw()
        {
            var clip = Sfx("draw");
            if (clip != null && !_drawSource.isPlaying)
            {
                _drawSource.pitch = Random.Range(0.9f, 1.15f);
                _drawSource.PlayOneShot(clip, 0.5f);
            }
        }

        public void SetEngineRunning(bool running, float speedRatio)
        {
            if (_engineSource == null) return;
            if (running)
            {
                if (!_engineSource.isPlaying && _engineSource.clip != null) _engineSource.Play();
                _engineSource.pitch = Mathf.Lerp(0.8f, 1.8f, speedRatio);
                _engineSource.volume = Mathf.Lerp(0.2f, 0.45f, speedRatio);
            }
            else if (_engineSource.isPlaying) _engineSource.Stop();
        }

        // ---------------------------------------------------------------- Music

        /// <summary>Crossfades to the track for this key (a world id or "menu"). Same key again is a no-op.</summary>
        public void PlayMusic(string key, float fadeSeconds = 1.2f)
        {
            if (key == _musicKey) return;
            var clip = Resources.Load<AudioClip>(Root + "Music/" + key);
            if (clip == null) clip = Resources.Load<AudioClip>(Root + "Music/menu");   // unknown world: fall back to the menu loop
            if (clip == null) return;
            _musicKey = key;
            var next = _musicLive == _musicA ? _musicB : _musicA;
            next.clip = clip;
            next.volume = 0f;
            next.pitch = 1f;
            next.Play();
            _fadeFrom = _musicLive;
            _musicLive = next;
            _fade = 0f;
            _fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
        }

        private AudioSource _fadeFrom;
        private float _fade = 1f, _fadeSeconds = 1f;

        /// <summary>Music is quieter while true (announcer talking, game over).</summary>
        public void DuckMusic(bool duck) => _duckTarget = duck ? 0.35f : 1f;

        /// <summary>0 = normal, 1 = frenzy: music speeds up a little and gets brighter.</summary>
        public void SetMusicIntensity(float intensity01)
        {
            _pitchTarget = 1f + 0.08f * Mathf.Clamp01(intensity01);
        }

        public void StopMusic(float fadeSeconds = 0.6f)
        {
            _musicKey = "";
            _fadeFrom = _musicLive;
            _musicLive = null;
            _fade = 0f;
            _fadeSeconds = Mathf.Max(0.05f, fadeSeconds);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_unduckAt > 0f && Time.unscaledTime >= _unduckAt) { _unduckAt = 0f; _duckTarget = 1f; }
            _duck = Mathf.MoveTowards(_duck, _duckTarget, dt * 2.5f);
            if (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + dt / _fadeSeconds);
                if (_fadeFrom != null) _fadeFrom.volume = MusicVolume * _duck * (1f - _fade);
                if (_fade >= 1f && _fadeFrom != null) { _fadeFrom.Stop(); _fadeFrom = null; }
            }
            if (_musicLive != null)
            {
                _musicLive.volume = MusicVolume * _duck * Mathf.Min(1f, _fade);
                _musicLive.pitch = Mathf.MoveTowards(_musicLive.pitch, _pitchTarget, dt * 0.2f);
            }
        }

        // ---------------------------------------------------------------- Robot announcer

        private AudioClip[] VoClips(string key)
        {
            if (_vo.TryGetValue(key, out var set)) return set;
            var list = new List<AudioClip>();
            for (int i = 0; i < 8; i++)
            {
                var c = Resources.Load<AudioClip>(Root + "VO/" + key + "_" + i);
                if (c == null) break;
                list.Add(c);
            }
            _vo[key] = set = list.ToArray();
            return set;
        }

        /// <summary>
        /// The robot says a line. A line is skipped while another one is playing unless <paramref name="important"/>, and the same
        /// line never repeats within <paramref name="cooldown"/> seconds, so the announcer stays a treat and not noise.
        /// </summary>
        public bool Say(string key, bool important = false, float cooldown = 9f)
        {
            if (_voSource == null) return false;
            float now = Time.unscaledTime;
            if (!important && now < _voBusyUntil) return false;
            if (_voLast.TryGetValue(key, out var last) && now - last < cooldown) return false;
            var set = VoClips(key);
            if (set.Length == 0) return false;
            int i = Random.Range(0, set.Length);
            if (set.Length > 1 && key == _lastVoKey && i == _lastVoIndex) i = (i + 1) % set.Length;
            _lastVoKey = key; _lastVoIndex = i;
            _voLast[key] = now;
            _voSource.Stop();
            _voSource.clip = set[i];
            _voSource.volume = VoVolume;
            _voSource.Play();
            _voBusyUntil = now + set[i].length + 0.4f;
            DuckMusic(true);
            _unduckAt = now + set[i].length + 0.2f;
            return true;
        }
    }
}
