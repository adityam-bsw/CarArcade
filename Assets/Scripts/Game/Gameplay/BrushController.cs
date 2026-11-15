using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace RoadBuilderGame
{
    /// <summary>
    /// Turns pointer input into road strokes. Extracted from RoadGameManager unchanged: same smoothing,
    /// same spacing, same ink rules. Ink lives in the manager; this class asks for it and spends it
    /// through callbacks so it never owns economy state.
    /// </summary>
    public sealed class BrushController
    {
        private readonly Camera _cam;
        private readonly RoadBuilder _road;

        private Vector2 _lastStrokeWorld;
        private Vector2 _smoothedBrushWorld;

        public bool IsDrawing { get; private set; }

        /// <summary>Current ink available to spend.</summary>
        public Func<float> InkAvailable;
        /// <summary>Called with the ink amount a committed road segment cost.</summary>
        public Action<float> SpendInk;

        // Tuning, copied from the manager's serialized fields every frame so Inspector edits keep working.
        public float SmoothingSpeed = 22f;
        public float DragSpacing = 0.28f;
        public float InkCostPerMeter = 1.2f;

        public BrushController(Camera cam, RoadBuilder road)
        {
            _cam = cam;
            _road = road;
        }

        /// <summary>Forget the current stroke without ending the road segment (used on life loss / new session).</summary>
        public void Cancel() { IsDrawing = false; }

        /// <summary>Read pointer input for this frame and advance the stroke.</summary>
        public void Tick()
        {
            // Don't draw road strokes if pointer is over any UI button
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screenPos = Vector2.zero;
            bool down = false;
            bool held = false;
            bool up = false;

            // 1. New Input System pointer
            var pointer = Pointer.current;
            if (pointer != null)
            {
                screenPos = pointer.position.ReadValue();
                down = pointer.press.wasPressedThisFrame;
                held = pointer.press.isPressed;
                up = pointer.press.wasReleasedThisFrame;
            }

            // 2. Legacy / mouse fallback (editor clicks, mouse, touches)
            if (!down && !held && !up)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    down = true;
                    held = true;
                    screenPos = Input.mousePosition;
                }
                else if (Input.GetMouseButton(0))
                {
                    held = true;
                    screenPos = Input.mousePosition;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    up = true;
                    screenPos = Input.mousePosition;
                }
            }

            if (!down && !held && !up) return;

            Vector2 world = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -_cam.transform.position.z));
            Process(world, down, held, up);
        }

        /// <summary>Feed a pointer event in world space (tests and the debug auto-driver use this instead of a real pointer).</summary>
        public void Inject(Vector2 world, bool down, bool held, bool up) { Process(world, down, held, up); }

        private void Process(Vector2 world, bool down, bool held, bool up)
        {
            if (down)
            {
                StartStroke(world);
            }
            else if (held && IsDrawing)
            {
                ContinueStroke(world);
            }
            else if (up && IsDrawing)
            {
                EndStroke();
            }
        }

        private void StartStroke(Vector2 worldPos)
        {
            if (InkAvailable() < 4f) return;

            // Only start a stroke if it is allowed by forward-only / anti-overlap rules
            if (!_road.BeginNewSegment(worldPos))
            {
                IsDrawing = false;
                return;
            }

            IsDrawing = true;
            _smoothedBrushWorld = worldPos;
            _lastStrokeWorld = _road.LastPoint;
            if (SoundManager.Instance) SoundManager.Instance.PlayDraw();
            if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(_lastStrokeWorld, new Color(0.9f, 0.95f, 1f), 3);
        }

        private void ContinueStroke(Vector2 worldPos)
        {
            if (InkAvailable() < 2f)
            {
                EndStroke();
                return;
            }

            // Smooth the pointer position using an exponential spring filter so micro-jitters vanish
            float alpha = 1f - Mathf.Exp(-SmoothingSpeed * Time.deltaTime);
            _smoothedBrushWorld = Vector2.Lerp(_smoothedBrushWorld, worldPos, alpha);

            float d = Vector2.Distance(_lastStrokeWorld, _smoothedBrushWorld);
            if (d >= DragSpacing)
            {
                if (_road.AddPointToCurrent(_smoothedBrushWorld))
                {
                    Vector2 actualAdded = _road.LastPoint;
                    float committedDist = Vector2.Distance(_lastStrokeWorld, actualAdded);
                    SpendInk(committedDist * InkCostPerMeter);
                    _lastStrokeWorld = actualAdded;
                    if (SoundManager.Instance) SoundManager.Instance.PlayDraw();
                    if (ParticleManager.Instance) ParticleManager.Instance.SpawnBurst(actualAdded, new Color(0.85f, 0.95f, 1f), 2);
                }
            }
            else
            {
                // Keep stretching the elastic live tip between knot commits for 60fps silkiness
                _road.UpdateLiveStrokeTip(_smoothedBrushWorld);
            }
        }

        private void EndStroke()
        {
            IsDrawing = false;
            _road.EndSegment();
        }
    }
}
