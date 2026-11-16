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

    /// <summary>
    /// Data for a catalogue vehicle: one assembled sprite (wheels drawn in it) plus the physics numbers.
    /// The car drives on two invisible wheels placed under the sprite.
    /// </summary>
    /// <summary>A wheel cut out of a vehicle picture: round sprite plus its centre and radius in picture pixels (from the top-left).</summary>
    [System.Serializable]
    public class WheelArt
    {
        public Sprite sprite;
        public float cx, cy, r;
    }

    [System.Serializable]
    public class VehicleRig
    {
        public string id;
        public Sprite body;
        public float width = 2.4f;
        public float mass = 1.45f;
        public float torque = 34f;
        public float speedMultiplier = 1f;
        [Tooltip("Optional: wheels cut out of the picture. With two or more, the outer two become the physics wheels and spin.")]
        public WheelArt[] wheels;
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

        /// <summary>Catalogue vehicles scale the manager's base speed by this.</summary>
        public float SpeedMultiplier { get; private set; } = 1f;
        public Rigidbody2D Body { get; private set; }
        public bool HeadHit { get; set; }

        private ContactFilter2D _groundFilter = new ContactFilter2D { useTriggers = false, useLayerMask = false };

        /// <summary>True while a wheel or the body touches something solid (pickups, which are triggers, do not count).</summary>
        public bool IsGrounded
        {
            get
            {
                if (Body == null) return true;
                if (_wheels != null) foreach (var w in _wheels) if (w != null && w.IsTouching(_groundFilter)) return true;
                return Body.IsTouching(_groundFilter);
            }
        }

        /// <summary>Boost: raises forward speed to at least <paramref name="speed"/> without the unstick hop.</summary>
        public void BoostKick(float speed)
        {
            if (Body == null) return;
            var v = Body.linearVelocity;
            if (v.x < speed) Body.linearVelocity = new Vector2(speed, v.y);
        }

        // ---- Stage-event hooks (Gravity Shift, Wind Canyon, Tiny/Giant, Fragile). Plain data in, nothing about events in here.
        /// <summary>Extra acceleration in m/s^2 applied every physics step (wind). Zero is free.</summary>
        public Vector2 WindAccel;
        /// <summary>Gravity multiplier on the car (body and wheels only; the world gravity is never touched).</summary>
        public float GravityMul { get; private set; } = 1f;
        /// <summary>Uniform vehicle scale (1 = normal). Colliders, wheels, joints, mass and torque follow it.</summary>
        public float Scale { get; private set; } = 1f;
        private float _impactMps;

        /// <summary>Largest collision speed along a contact normal since the last call (m/s), then reset. Wheels and body both report.</summary>
        public float ConsumeImpact() { float v = _impactMps; _impactMps = 0f; return v; }
        internal void NoteImpact(float normalSpeed) { if (normalSpeed > _impactMps) _impactMps = normalSpeed; }

        public void SetGravityMul(float g)
        {
            if (Mathf.Approximately(g, GravityMul) || Body == null) { GravityMul = g; return; }
            GravityMul = g;
            Body.gravityScale = g;
            if (_wheels != null) for (int i = 0; i < _wheels.Length; i++) if (_wheels[i] != null) _wheels[i].gravityScale = g;
        }

        /// <summary>
        /// Physics-safe uniform scale: body and wheel transforms scale (their colliders follow), mass and torque scale with the area (s^2) so the car keeps its
        /// feel, the wheel joints are rebuilt on the scaled anchors and the wheels are re-seated under the body. Reverts exactly with SetScale(1).
        /// </summary>
        public void SetScale(float s)
        {
            s = Mathf.Clamp(s, 0.3f, 3f);
            if (Body == null || Mathf.Approximately(s, Scale)) return;
            float r = s / Scale;
            float mr = r * r;
            Scale = s;
            Body.transform.localScale = new Vector3(s, s, 1f);
            Body.mass *= mr;
            maxTorquePerWheel *= mr;
            _wheelRadius = _baseWheelRadius * s;
            _backAnchor *= r; _frontAnchor *= r;
            if (r > 1f) Body.position += Vector2.up * ((r - 1f) * (_baseWheelRadius + 0.35f));      // a growing car is lifted so the bigger wheels do not start inside the road
            Quaternion rot = Quaternion.Euler(0f, 0f, Body.rotation);
            for (int i = 0; i < 2 && _wheels != null && i < _wheels.Length; i++)
            {
                var w = _wheels[i];
                if (w == null) continue;
                w.transform.localScale = new Vector3(s, s, 1f);
                w.mass *= mr;
                Vector2 pos = Body.position + (Vector2)(rot * (i == 0 ? _backAnchor : _frontAnchor));
                w.position = pos; w.transform.position = pos;
                w.linearVelocity = Body.linearVelocity;
            }
            if (_joints != null) for (int i = 0; i < _joints.Length; i++) if (_joints[i] != null) { _joints[i].enabled = false; _joints[i].enabled = true; }   // rebuilds the joint on the scaled anchor
        }

        private Rigidbody2D[] _wheels;
        private WheelJoint2D[] _joints;
        private float _baseWheelRadius = 0.42f;
        private float _wheelRadius = 0.42f;
        private Vector2 _backAnchor;
        private Vector2 _frontAnchor;
        private SpriteRenderer[] _renderers;
        private bool _invisibleWheels;
        private WheelArt _artBack, _artFront;
        private float _artScale;
        private bool _isInvincible;
        private float _invincibleTimer;
        private float _lowSpeedTimer;

        /// <param name="art">Smooth uses the smooth body and its own wheel when SmoothSliceArt has them; anything missing falls back to the original sprites. Size and colliders are identical either way.</param>
        public static RoadCar Create(Vector2 position, VehicleType type, ArtStyle art = ArtStyle.Pixel)
        {
            var root = new GameObject("Car_" + type);
            var car = root.AddComponent<RoadCar>();
            car.vehicleType = type;
            car.Build(position, type, art);
            return car;
        }

        /// <summary>Builds a catalogue vehicle from its rig data.</summary>
        public static RoadCar Create(Vector2 position, VehicleRig rig)
        {
            var root = new GameObject("Car_" + rig.id);
            var car = root.AddComponent<RoadCar>();
            car.Build(position, rig);
            return car;
        }

        void Build(Vector2 position, VehicleRig rig)
        {
            float w = Mathf.Max(1f, rig.width);
            Vector2 sz = rig.body != null ? rig.body.bounds.size : new Vector2(2.4f, 0.9f);
            float h = w * sz.y / Mathf.Max(0.01f, sz.x);
            float wr = Mathf.Clamp(h * 0.3f, 0.22f, 0.5f);
            maxTorquePerWheel = rig.torque;
            SpeedMultiplier = rig.speedMultiplier;
            _invisibleWheels = true;
            var wheels = rig.wheels;
            if (wheels != null && wheels.Length >= 2 && rig.body != null)
            {
                // Wheels cut from the picture: physics wheels sit exactly where the drawn ones are and the cut-out discs spin on top.
                WheelArt back = wheels[0], front = wheels[0];
                foreach (var wa in wheels) { if (wa.cx < back.cx) back = wa; if (wa.cx > front.cx) front = wa; }
                float k = w / rig.body.rect.width;
                Vector2 Anchor(WheelArt wa) => new Vector2((wa.cx - rig.body.rect.width * 0.5f) * k, (rig.body.rect.height * 0.5f - wa.cy) * k);
                _artBack = back; _artFront = front; _artScale = k;
                BuildCore(position, rig.body, null, w, h, Mathf.Clamp(back.r * k, 0.18f, 1f), rig.mass, Anchor(back), Anchor(front));
                return;
            }
            // No cut-outs: wheel centres sit a little inside the ends, bottoms level with the bottom of the picture.
            float ay = -h * 0.5f + wr;
            BuildCore(position, rig.body, null, w, h, wr, rig.mass,
                new Vector2(-0.30f * w, ay), new Vector2(0.30f * w, ay));
        }

        void Build(Vector2 position, VehicleType type, ArtStyle art)
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

            Sprite bodySprite = Resources.Load<Sprite>(bodyPath);
            Sprite wheelSprite = null;
            if (art == ArtStyle.Smooth)
            {
                var smooth = SmoothSliceArt.Load();
                if (smooth != null)
                {
                    // Per-vehicle art; each missing piece falls back to the original sprite (and the shared Wheel).
                    if (smooth.Body(type) != null) bodySprite = smooth.Body(type);
                    wheelSprite = smooth.Wheel(type);
                }
            }
            if (wheelSprite == null) wheelSprite = Resources.Load<Sprite>(wheelPath);
            BuildCore(position, bodySprite, wheelSprite, w, h, wr, mass, backAnchor, frontAnchor);
        }

        // Shared by every car: creating them per car leaked two materials per run.
        private static PhysicsMaterial2D _tyreMat, _bodyMat;
        private static PhysicsMaterial2D TyreMaterial => _tyreMat != null ? _tyreMat : (_tyreMat = new PhysicsMaterial2D("Tyre") { friction = 2.4f, bounciness = 0.04f });
        private static PhysicsMaterial2D BodyMaterial => _bodyMat != null ? _bodyMat : (_bodyMat = new PhysicsMaterial2D("CarBody") { friction = 0.0f, bounciness = 0.02f });

        void BuildCore(Vector2 position, Sprite bodySprite, Sprite wheelSprite, float w, float h, float wr, float mass,
            Vector2 backAnchor, Vector2 frontAnchor)
        {
            _wheelRadius = wr; _baseWheelRadius = wr;

            // High-grip rubber tyres & zero-friction skidplate on chassis so the bumper never snags in dips!
            var tyreMat = TyreMaterial;
            var bodyMat = BodyMaterial;

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
            bodyGo.AddComponent<CarImpactSensor>().car = this;

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
            go.AddComponent<CarImpactSensor>().car = this;

            // Catalogue vehicles have their wheels drawn into the body picture, so their physics wheels stay invisible.
            var art = name == "BackWheel" ? _artBack : (name == "FrontWheel" ? _artFront : null);
            if (art != null && art.sprite != null)
            {
                float d = art.sprite.rect.width * _artScale;      // the cut-out is a square of whole pixels
                var sr = SpriteFactory.CreateSprite("WheelSprite", go.transform, art.sprite, Color.white, 6);
                Vector2 b = art.sprite.bounds.size;
                sr.transform.localScale = new Vector3(d / b.x, d / b.y, 1f);
            }
            else if (sprite != null || !_invisibleWheels)
            {
                Sprite s = sprite != null ? sprite : SpriteFactory.Circle;
                var sr = SpriteFactory.CreateSprite("WheelSprite", go.transform, s, Color.white, 6);
                float size = radius * 2f * 1.03f;
                Vector2 b = s.bounds.size;
                sr.transform.localScale = new Vector3(size / b.x, size / b.y, 1f);
            }

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

        /// <summary>Cheat god mode: a steady subtle cyan tint instead of the hit-flash (set by the manager; never flickers).</summary>
        public bool GodVisual
        {
            get => _godVisual;
            set
            {
                if (_godVisual == value) return;
                _godVisual = value;
                foreach (var r in _renderers) if (r != null) r.color = value ? new Color(0.78f, 1f, 1f, 1f) : Color.white;
            }
        }
        private bool _godVisual;

        private void Update()
        {
            if (_godVisual) { _invincibleTimer -= Time.deltaTime; if (_invincibleTimer <= 0f) _isInvincible = false; return; }
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

            if (WindAccel.x != 0f || WindAccel.y != 0f)
            {
                Body.AddForce(WindAccel * Body.mass);
                for (int i = 0; i < _wheels.Length; i++) if (_wheels[i] != null) _wheels[i].AddForce(WindAccel * _wheels[i].mass);
            }

            if (!MotorOn)
            {
                if (_joints != null)
                {
                    for (int i = 0; i < _joints.Length; i++)
                        if (_joints[i] != null) _joints[i].useMotor = false;
                }
                return;
            }

            float targetDeg = -TargetSpeed / Mathf.Max(0.25f * Scale, _wheelRadius) * Mathf.Rad2Deg;      // the 0.25 floor scales with the car (a Tiny wheel is smaller than 0.25 m)

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

        /// <summary>Game over: motor off and the whole rig stops simulating, so nothing keeps rolling under the result screen. A fresh car is built for revive or retry.</summary>
        public void Freeze()
        {
            MotorOn = false;
            if (_joints != null) for (int i = 0; i < _joints.Length; i++) if (_joints[i] != null) _joints[i].useMotor = false;
            if (_wheels != null) for (int i = 0; i < _wheels.Length; i++) if (_wheels[i] != null) { _wheels[i].linearVelocity = Vector2.zero; _wheels[i].angularVelocity = 0f; _wheels[i].simulated = false; }
            if (Body != null) { Body.linearVelocity = Vector2.zero; Body.angularVelocity = 0f; Body.simulated = false; }
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

    /// <summary>Reports the speed of every solid collision along its contact normal to the car (Fragile Vehicle reads it). No allocation.</summary>
    public class CarImpactSensor : MonoBehaviour
    {
        public RoadCar car;

        void OnCollisionEnter2D(Collision2D c)
        {
            if (car == null || c.contactCount == 0) return;
            car.NoteImpact(Mathf.Abs(Vector2.Dot(c.relativeVelocity, c.GetContact(0).normal)));
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
