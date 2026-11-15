using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Follows the car with look-ahead and keeps the parallax background in step.
    /// Extracted from RoadGameManager unchanged (same offsets, clamps and smoothing rates).
    /// </summary>
    public sealed class GameCameraRig
    {
        private readonly Camera _cam;
        private readonly BackgroundDirector _background;
        private Vector3 _shakeOffset;
        private float _shakeAmp, _shakeLeft, _shakeTotal;

        public GameCameraRig(Camera cam, BackgroundDirector background)
        {
            _cam = cam;
            _background = background;
        }

        /// <summary>Call from LateUpdate. When <paramref name="following"/> is false only the background is updated.</summary>
        /// <summary>Shakes the camera: amplitude in world units, fading out over the duration (real time, so it also works in slow motion).</summary>
        public void Shake(float amplitude, float seconds)
        {
            if (amplitude < _shakeAmp * (_shakeLeft / Mathf.Max(0.01f, _shakeTotal))) return;   // never replace a bigger shake that is still running
            _shakeAmp = amplitude; _shakeLeft = _shakeTotal = Mathf.Max(0.05f, seconds);
        }

        public void Tick(RoadCar car, bool following)
        {
            if (_cam != null) _cam.transform.position -= _shakeOffset;   // remove last frame's shake so smoothing follows the true position
            _shakeOffset = Vector3.zero;
            if (_shakeLeft > 0f)
            {
                _shakeLeft -= Time.unscaledDeltaTime;
                float k = Mathf.Max(0f, _shakeLeft / _shakeTotal);
                _shakeOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * (_shakeAmp * k);
            }
            TickFollow(car, following);
            if (_cam != null) _cam.transform.position += _shakeOffset;
        }

        private void TickFollow(RoadCar car, bool following)
        {
            if (following && car != null && _cam != null)
            {
                Vector3 currentCam = _cam.transform.position;
                Vector3 carPos = car.Body.position;

                // Camera X follows car smoothly with forward look-ahead
                float targetX = carPos.x + 2.5f;
                // Camera Y is stabilized: follows road or car gently, clamped so the background never whips during a fall
                float targetY = Mathf.Clamp(carPos.y + 1.1f, -2.5f, 8.5f);

                float newX = Mathf.Lerp(currentCam.x, targetX, 7.5f * Time.deltaTime);
                float newY = Mathf.Lerp(currentCam.y, targetY, 3.5f * Time.deltaTime);

                _cam.transform.position = new Vector3(newX, newY, -10f);
                _background.Follow(_cam);
            }
            else if (_background != null && _cam != null)
            {
                _background.Follow(_cam);
            }
        }

        /// <summary>Jump straight to the car (session start) with no smoothing.</summary>
        public void Snap(RoadCar car)
        {
            if (car == null || _cam == null) return;
            Vector3 pos = car.Body.position;
            pos.z = -10f;
            pos.y = Mathf.Clamp(pos.y + 1.1f, -2.5f, 8.5f);
            pos.x += 2.5f;
            _cam.transform.position = pos;
        }
    }
}
