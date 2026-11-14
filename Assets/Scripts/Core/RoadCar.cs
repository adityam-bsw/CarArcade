using UnityEngine;

namespace RoadBuilderGame
{
    public enum VehicleType
    {
        ClassicRed = 0,
        SportCyan = 1,
        MonsterTruck = 2
    }

    [System.Serializable]
    public class VehicleData
    {
        public VehicleType type;
        public string displayName;
        public float baseSpeed;
        public float maxSpeed;
        public float acceleration;
        public float bodyWidth;
        public float bodyHeight;
        public float wheelRadius;
        public float wheelMass;
        public float suspensionFreq;
        public int costCoins;
    }

    public class RoadCar : MonoBehaviour
    {
        public VehicleType vehicleType = VehicleType.ClassicRed;

        [Header("Drive & Hill Climbing")]
        public float TargetSpeed = 4.5f;
        public bool MotorOn;
        public float maxTorquePerWheel = 34f;
        public float torqueGain = 1.35f;
        [Tooltip("Extra forward traction assist when climbing steep hills")]
        public float hillClimbAssistForce = 14f;

        public Rigidbody2D Body { get; private set; }
        public bool HeadHit { get; set; }

        private Rigidbody2D[] _wheels;
        private WheelJoint2D[] _joints;
        private float _wheelRadius = 0.42f;
        private Vector2 _backAnchor;
        private Vector2 _frontAnchor;
        private SpriteRenderer[] _renderers;
        private bool _isInvincible;
        private float _invincibleTimer;
        private float _lowSpeedTimer;

        public static RoadCar Create(Vector2 position, VehicleType type)
        {
            var root = new GameObject("Car_" + type);
            var car = root.AddComponent<RoadCar>();
            car.vehicleType = type;
            car.Build(position, type);
            return car;
        }

        void Build(Vector2 position, VehicleType type)
        {
            string bodyPath = "RoadBuilder/CarBody";
            string wheelPath = "RoadBuilder/Wheel";
            float w = 2.4f, h = 0.9f, wr = 0.42f;
            float mass = 1.45f;
            Vector2 backAnchor = new Vector2(-0.72f, -0.42f);
            Vector2 frontAnchor = new Vector2(0.72f, -0.42f);
            maxTorquePerWheel = 34f;

            if (type == VehicleType.SportCyan)
            {
                bodyPath = "RoadBuilder/SportCarBody";
                w = 2.4f; h = 0.8f; wr = 0.40f;
                mass = 1.3f;
                maxTorquePerWheel = 38f;
                backAnchor = new Vector2(-0.70f, -0.38f);
                frontAnchor = new Vector2(0.70f, -0.38f);
            }
            else if (type == VehicleType.MonsterTruck)
            {
                bodyPath = "RoadBuilder/MonsterTruckBody";
                w = 2.6f; h = 1.1f; wr = 0.52f;
                mass = 2.0f;
                maxTorquePerWheel = 52f;
                backAnchor = new Vector2(-0.78f, -0.48f);
                frontAnchor = new Vector2(0.78f, -0.48f);
            }

            _wheelRadius = wr;

            Sprite bodySprite = Resources.Load<Sprite>(bodyPath);
            Sprite wheelSprite = Resources.Load<Sprite>(wheelPath);

            // High-grip rubber tyres & zero-friction skidplate on chassis so the bumper never snags in dips!
            var tyreMat = new PhysicsMaterial2D("Tyre") { friction = 2.4f, bounciness = 0.04f };
            var bodyMat = new PhysicsMaterial2D("CarBody") { friction = 0.0f, bounciness = 0.02f };

            // ---- Body ----
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(transform, false);
            bodyGo.transform.position = position;
            Body = bodyGo.AddComponent<Rigidbody2D>();
            Body.useAutoMass = false;
            Body.mass = mass;
            Body.angularDamping = 0.8f;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.centerOfMass = new Vector2(0f, -0.25f);

            // Chamfered / boat-hulled bottom collider (lifted front & rear bumper approaches)
            // Prevents the front nose or tail from digging into concave road valleys!
            Vector2[] shape =
            {
                N(0.18f, 0.28f, w, h),
                N(0.82f, 0.28f, w, h),
                N(0.96f, 0.45f, w, h),
                N(0.96f, 0.58f, w, h),
                N(0.82f, 0.58f, w, h),
                N(0.68f, 0.95f, w, h),
                N(0.38f, 0.95f, w, h),
                N(0.25f, 0.58f, w, h),
                N(0.04f, 0.58f, w, h),
                N(0.04f, 0.45f, w, h)
            };
            var poly = bodyGo.AddComponent<PolygonCollider2D>();
            poly.points = shape;
            poly.sharedMaterial = bodyMat;

            var bodySr = SpriteFactory.CreateSprite("BodySprite", bodyGo.transform,
                bodySprite != null ? bodySprite : SpriteFactory.Box(new Color(0.9f, 0.2f, 0.2f)), Color.white, 5);
            FitWidth(bodySr, w, h);

            // Roof Sensor
            var head = new GameObject("RoofSensor");
            head.transform.SetParent(bodyGo.transform, false);
            head.transform.localPosition = N(0.53f, 0.93f, w, h);
            var headCol = head.AddComponent<CircleCollider2D>();
            headCol.isTrigger = true;
            headCol.radius = 0.20f;
            head.AddComponent<CarRoofSensor>().car = this;

            // ---- Wheels ----
            _backAnchor = backAnchor;
            _frontAnchor = frontAnchor;
            _joints = new WheelJoint2D[2];
            _wheels = new[]
            {
                CreateWheel("BackWheel", position, backAnchor, wr, wheelSprite, tyreMat, out _joints[0]),
                CreateWheel("FrontWheel", position, frontAnchor, wr, wheelSprite, tyreMat, out _joints[1])
            };

            _renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        Rigidbody2D CreateWheel(string name, Vector2 bodyPos, Vector2 anchor, float radius, Sprite sprite, PhysicsMaterial2D mat, out WheelJoint2D joint)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = bodyPos + anchor;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.useAutoMass = false;
            rb.mass = 0.55f;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = radius;
            col.sharedMaterial = mat;

            Sprite s = sprite != null ? sprite : SpriteFactory.Circle;
            var sr = SpriteFactory.CreateSprite("WheelSprite", go.transform, s, Color.white, 6);
            float size = radius * 2f * 1.03f;
            Vector2 b = s.bounds.size;
            sr.transform.localScale = new Vector3(size / b.x, size / b.y, 1f);

            joint = Body.gameObject.AddComponent<WheelJoint2D>();
            joint.connectedBody = rb;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = anchor;
            joint.connectedAnchor = Vector2.zero;
            joint.enableCollision = false;
            joint.useMotor = true;
            joint.motor = new JointMotor2D { motorSpeed = -600f, maxMotorTorque = maxTorquePerWheel * 12f };
            joint.suspension = new JointSuspension2D { angle = 90f, frequency = 6.5f, dampingRatio = 0.8f };
            return rb;
        }

        public void TriggerInvincibility(float duration = 2.0f)
        {
            _isInvincible = true;
            _invincibleTimer = duration;
        }

        public bool IsInvincible => _isInvincible;

        private void Update()
        {
            if (_isInvincible)
            {
                _invincibleTimer -= Time.deltaTime;
                bool flash = (Mathf.FloorToInt(Time.time * 12f) % 2) == 0;
                foreach (var r in _renderers)
                {
                    if (r != null)
                    {
                        var c = r.color;
                        c.a = flash ? 0.35f : 1f;
                        r.color = c;
                    }
                }

                if (_invincibleTimer <= 0f)
                {
                    _isInvincible = false;
                    foreach (var r in _renderers)
                    {
                        if (r != null)
                        {
                            var c = r.color;
                            c.a = 1f;
                            r.color = c;
                        }
                    }
                }
            }
        }

        void FixedUpdate()
        {
            if (_wheels == null || Body == null) return;

            if (!MotorOn)
            {
                if (_joints != null)
                {
                    for (int i = 0; i < _joints.Length; i++)
                        if (_joints[i] != null) _joints[i].useMotor = false;
                }
                return;
            }

            float targetDeg = -TargetSpeed / Mathf.Max(0.25f, _wheelRadius) * Mathf.Rad2Deg;

            // 1. Drive both WheelJoint2D motors AND direct wheel torque for unstoppable hill climbing
            if (_joints != null)
            {
                for (int i = 0; i < _joints.Length; i++)
                {
                    if (_joints[i] != null)
                    {
                        _joints[i].useMotor = true;
                        _joints[i].motor = new JointMotor2D
                        {
                            motorSpeed = targetDeg,
                            maxMotorTorque = maxTorquePerWheel * 15f
                        };
                    }
                }
            }

            bool anyWheelGrounded = false;
            foreach (var w in _wheels)
            {
                if (w != null)
                {
                    float errRad = (targetDeg - w.angularVelocity) * Mathf.Deg2Rad;
                    w.AddTorque(Mathf.Clamp(errRad * torqueGain, -maxTorquePerWheel, maxTorquePerWheel));
                    if (w.IsTouchingLayers()) anyWheelGrounded = true;
                }
            }

            // 2. Hill-Climb Traction Assist & Anti-Stall Boost:
            // When wheels or chassis touch the road and forward speed drops below TargetSpeed,
            // apply smooth forward drive along the car's right vector so steep hills & valleys never stall the car!
            bool bodyTouching = Body.IsTouchingLayers();
            float forwardVel = Vector2.Dot(Body.linearVelocity, Body.transform.right);

            if ((anyWheelGrounded || bodyTouching) && forwardVel < TargetSpeed * 0.85f)
            {
                float speedDeficit = Mathf.Clamp01((TargetSpeed - forwardVel) / Mathf.Max(1f, TargetSpeed));
                Vector2 driveDir = ((Vector2)Body.transform.right + Vector2.right * 0.35f).normalized;
                Body.AddForce(driveDir * (hillClimbAssistForce * speedDeficit * Body.mass), ForceMode2D.Force);
            }

            // 3. Automatic Unstick Pulse:
            // If the car has been wedged in a deep valley (speed < 0.6 m/s) for > 0.45 seconds,
            // give it a smooth upward + forward hop to clear the dip automatically!
            if (MotorOn && Body.linearVelocity.magnitude < 0.65f)
            {
                _lowSpeedTimer += Time.fixedDeltaTime;
                if (_lowSpeedTimer > 0.45f)
                {
                    _lowSpeedTimer = 0f;
                    NudgeUnstick();
                }
            }
            else
            {
                _lowSpeedTimer = 0f;
            }
        }

        /// <summary>
        /// Gives the car an immediate upward + forward hop and levels its pitch so it pops out of any dip or wedge.
        /// </summary>
        public void NudgeUnstick()
        {
            if (Body == null) return;
            Body.linearVelocity = new Vector2(Mathf.Max(TargetSpeed * 0.75f, 3.8f), 4.2f);
            Body.angularVelocity = 0f;
            // Level out extreme pitch if wedged nose-up or nose-down
            float zAngle = Body.rotation;
            Body.rotation = Mathf.LerpAngle(zAngle, 0f, 0.55f);
        }

        public void RespawnSafely(Vector2 safeRoadSurfacePos)
        {
            HeadHit = false;
            _lowSpeedTimer = 0f;
            Vector2 targetBodyPos = safeRoadSurfacePos + new Vector2(0f, 1.15f);

            if (Body != null)
            {
                Body.linearVelocity = Vector2.zero;
                Body.angularVelocity = 0f;
                Body.position = targetBodyPos;
                Body.rotation = 0f;
                Body.transform.position = targetBodyPos;
                Body.transform.rotation = Quaternion.identity;
            }

            if (_wheels != null && _wheels.Length >= 2)
            {
                Vector2 backPos = targetBodyPos + _backAnchor;
                Vector2 frontPos = targetBodyPos + _frontAnchor;

                _wheels[0].linearVelocity = Vector2.zero;
                _wheels[0].angularVelocity = 0f;
                _wheels[0].position = backPos;
                _wheels[0].rotation = 0f;
                _wheels[0].transform.position = backPos;
                _wheels[0].transform.rotation = Quaternion.identity;

                _wheels[1].linearVelocity = Vector2.zero;
                _wheels[1].angularVelocity = 0f;
                _wheels[1].position = frontPos;
                _wheels[1].rotation = 0f;
                _wheels[1].transform.position = frontPos;
                _wheels[1].transform.rotation = Quaternion.identity;
            }

            TriggerInvincibility(2.5f);
        }

        public bool IsUpsideDown => Body.transform.up.y < -0.2f;

        static Vector2 N(float nx, float ny, float w, float h) =>
            new Vector2((nx - 0.5f) * w, (ny - 0.5f) * h);

        static void FitWidth(SpriteRenderer sr, float width, float height)
        {
            Vector2 b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(width / b.x, height / b.y, 1f);
        }
    }

    public class CarRoofSensor : MonoBehaviour
    {
        public RoadCar car;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (car != null && !car.IsInvincible && other.GetComponent<RoadChunk>() != null)
                car.HeadHit = true;
        }
    }
}
