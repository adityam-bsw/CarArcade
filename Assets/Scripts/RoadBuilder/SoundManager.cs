using UnityEngine;

namespace RoadBuilderGame
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _engineSource;
        private AudioSource _drawSource;

        private AudioClip _coinClip;
        private AudioClip _drawClip;
        private AudioClip _crashClip;
        private AudioClip _engineClip;
        private AudioClip _heartClip;
        private AudioClip _clickClip;
        private AudioClip _upgradeClip;

        public void Init()
        {
            Instance = this;
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            _engineSource = gameObject.AddComponent<AudioSource>();
            _engineSource.loop = true;
            _engineSource.playOnAwake = false;
            _engineSource.volume = 0.25f;

            _drawSource = gameObject.AddComponent<AudioSource>();
            _drawSource.playOnAwake = false;
            _drawSource.volume = 0.4f;

            _coinClip = Resources.Load<AudioClip>("RoadBuilder/Audio/coin");
            _drawClip = Resources.Load<AudioClip>("RoadBuilder/Audio/draw");
            _crashClip = Resources.Load<AudioClip>("RoadBuilder/Audio/crash");
            _engineClip = Resources.Load<AudioClip>("RoadBuilder/Audio/engine");
            _heartClip = Resources.Load<AudioClip>("RoadBuilder/Audio/heart");
            _clickClip = Resources.Load<AudioClip>("RoadBuilder/Audio/click");
            _upgradeClip = Resources.Load<AudioClip>("RoadBuilder/Audio/upgrade");

            if (_engineClip != null)
            {
                _engineSource.clip = _engineClip;
            }
        }

        public void PlayCoin()
        {
            if (_coinClip != null) _sfxSource.PlayOneShot(_coinClip, 0.75f);
        }

        public void PlayDraw()
        {
            if (_drawClip != null && !_drawSource.isPlaying)
            {
                _drawSource.pitch = Random.Range(0.9f, 1.15f);
                _drawSource.PlayOneShot(_drawClip, 0.5f);
            }
        }

        public void PlayCrash()
        {
            if (_crashClip != null) _sfxSource.PlayOneShot(_crashClip, 0.9f);
        }

        public void PlayHeart()
        {
            if (_heartClip != null) _sfxSource.PlayOneShot(_heartClip, 0.85f);
        }

        public void PlayClick()
        {
            if (_clickClip != null) _sfxSource.PlayOneShot(_clickClip, 0.6f);
        }

        public void PlayUpgrade()
        {
            if (_upgradeClip != null) _sfxSource.PlayOneShot(_upgradeClip, 0.8f);
        }

        public void SetEngineRunning(bool running, float speedRatio)
        {
            if (_engineSource == null) return;
            if (running)
            {
                if (!_engineSource.isPlaying && _engineClip != null) _engineSource.Play();
                _engineSource.pitch = Mathf.Lerp(0.8f, 1.8f, speedRatio);
                _engineSource.volume = Mathf.Lerp(0.2f, 0.45f, speedRatio);
            }
            else
            {
                if (_engineSource.isPlaying) _engineSource.Stop();
            }
        }
    }
}
