using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>What happened during one run, used for the result screen, missions and mastery.</summary>
    public sealed class RunStats
    {
        public int coins, gems, flips, boosts, milestones, nearMisses, rings, frenzies, bestMultiplier = 1;
        public float airTime, bestAir;
        public readonly List<string> lines = new List<string>();
        public int bonusCoins;
        public void Reset() { coins = gems = flips = boosts = milestones = nearMisses = rings = frenzies = 0; bestMultiplier = 1; airTime = bestAir = 0f; bonusCoins = 0; lines.Clear(); }
    }

    /// <summary>
    /// The rules that run inside one race: free boost meter, coin combo, power-up timers, air time, flips, distance
    /// milestones. Plain class (no Unity objects besides reading the car); the manager owns it and applies what it returns.
    /// </summary>
    public sealed class RunDirector
    {
        public enum BoostResult { None, Free, Paid, Refused }

        /// <summary>Moments the audio, animation and VFX layers react to. The director only raises them; it plays nothing.</summary>
        public enum Cue { Start, Boost, Shield, Magnet, Double, SlowMo, Ghost, Gem, Flip, BigAir, Combo, Chest, NewBest, NearMiss, Ring, FrenzyOn, FrenzyOff }
        public event Action<Cue> CueRaised;
        private void Raise(Cue c) => CueRaised?.Invoke(c);

        private readonly GameplayTuning _t;
        public readonly RunStats Stats = new RunStats();

        public float Boost01;
        public int Combo, Multiplier = 1;
        public float MagnetLeft, DoubleLeft, SlowLeft, GhostLeft, BoostLeft;
        public bool Shield;
        /// <summary>0 to 1 while charging; while frenzying it is the time left as a fraction.</summary>
        public float Frenzy01, FrenzyLeft;

        private float _comboLeft, _cooldown, _airFor, _flipAccum, _lastRot, _nextMilestone;
        private bool _wasGrounded = true, _recordPaid;
        private int _boostLevel = 1;

        /// <summary>A reward or event worth telling the player: label and the coins it gave (0 = just a notice).</summary>
        public event Action<string, int> Bonus;

        public RunDirector(GameplayTuning tuning) { _t = tuning; }

        public bool Boosting => BoostLeft > 0f;
        public bool Frenzying => FrenzyLeft > 0f;
        public bool Ghost => GhostLeft > 0f || Frenzying;           // frenzy makes ink free
        public float BoostSpeedBonus => (Boosting ? _t.boostSpeedBonus : 0f) + (Frenzying ? _t.frenzySpeedBonus : 0f);
        public float TimeScale => SlowLeft > 0f ? _t.slowMoScale : 1f;
        public bool MagnetOn => MagnetLeft > 0f;

        public void Begin(int boostLevel, float previousBest)
        {
            Stats.Reset();
            _boostLevel = Mathf.Max(1, boostLevel);
            Boost01 = 0f; Combo = 0; Multiplier = 1; _comboLeft = 0f;
            MagnetLeft = DoubleLeft = SlowLeft = GhostLeft = BoostLeft = 0f; Shield = false; Frenzy01 = FrenzyLeft = 0f;
            _cooldown = 0f; _airFor = 0f; _flipAccum = 0f; _wasGrounded = true;
            _nextMilestone = _t.milestoneEvery; _recordPaid = previousBest <= 1f;   // no "record" bonus on the very first run
            _previousBest = previousBest;
        }
        private float _previousBest;

        /// <summary>Call every frame while racing. dt is unscaled so power-up times stay in real seconds under slow-mo.</summary>
        public void Tick(float dt, RoadCar car, float distance)
        {
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            BoostLeft = Mathf.Max(0f, BoostLeft - dt);
            MagnetLeft = Mathf.Max(0f, MagnetLeft - dt);
            DoubleLeft = Mathf.Max(0f, DoubleLeft - dt);
            SlowLeft = Mathf.Max(0f, SlowLeft - dt);
            GhostLeft = Mathf.Max(0f, GhostLeft - dt);

            if (FrenzyLeft > 0f)
            {
                FrenzyLeft = Mathf.Max(0f, FrenzyLeft - dt);
                Frenzy01 = FrenzyLeft / Mathf.Max(0.1f, _t.frenzyDuration);
                if (FrenzyLeft <= 0f) { Frenzy01 = 0f; Raise(Cue.FrenzyOff); }
            }

            if (Combo > 0)
            {
                _comboLeft -= dt;
                if (_comboLeft <= 0f) { Combo = 0; Multiplier = 1; }
            }

            if (car != null && car.Body != null) TrackAir(dt, car);

            // Distance milestones: a chest every N metres, a bonus for passing the old record.
            if (_t.milestoneEvery > 0f && distance >= _nextMilestone)
            {
                int index = ++Stats.milestones;
                _nextMilestone += _t.milestoneEvery;
                int coins = _t.milestoneCoins * index;
                Pay("CHEST " + Mathf.RoundToInt(distance) + " m", coins); Raise(Cue.Chest);
                if (UnityEngine.Random.value < _t.milestoneGemChance) { Stats.gems++; Bonus?.Invoke("GEM FOUND", 0); }
            }
            if (!_recordPaid && distance > _previousBest)
            {
                _recordPaid = true;
                Pay("NEW RECORD", _t.recordBonus); Raise(Cue.NewBest);
            }
        }

        private void TrackAir(float dt, RoadCar car)
        {
            bool grounded = car.IsGrounded;
            float rot = car.Body.rotation;
            if (!grounded)
            {
                _airFor += dt;
                _flipAccum += Mathf.DeltaAngle(_lastRot, rot);
                if (_airFor <= _t.airMaxPaidSeconds) { Boost01 = Mathf.Min(1f, Boost01 + _t.meterPerAirSecond * dt); AddFrenzy(_t.frenzyPerAirSecond * dt); }   // a long fall is not a trick
            }
            else if (!_wasGrounded) Land();
            _lastRot = rot;
            _wasGrounded = grounded;
        }

        private void Land()
        {
            if (_airFor >= _t.airMinSeconds)
            {
                Stats.airTime += _airFor; Stats.bestAir = Mathf.Max(Stats.bestAir, _airFor);
                int coins = Mathf.RoundToInt((Mathf.Min(_airFor, _t.airMaxPaidSeconds) - _t.airMinSeconds + 0.5f) * _t.airCoinsPerSecond);
                Pay("AIR TIME " + _airFor.ToString("0.0") + " s", coins);
                if (_airFor >= 1.6f) Raise(Cue.BigAir);
            }
            int flips = Mathf.FloorToInt(Mathf.Abs(_flipAccum) / _t.flipDegrees);
            if (flips > 0)
            {
                Stats.flips += flips;
                Boost01 = Mathf.Min(1f, Boost01 + _t.meterPerFlip * flips);
                Pay(flips > 1 ? flips + "x FLIP" : "FLIP", _t.flipCoins * flips);
                AddFrenzy(_t.frenzyPerFlip * flips);
                Raise(Cue.Flip);
            }
            _airFor = 0f; _flipAccum = 0f;
        }

        /// <summary>The car crashed or respawned: whatever the air state was does not count.</summary>
        public void Interrupt() { _airFor = 0f; _flipAccum = 0f; _wasGrounded = true; }

        private void Pay(string label, int coins)
        {
            Stats.bonusCoins += coins;
            Stats.lines.Add(coins > 0 ? label + "  +" + coins : label);
            Bonus?.Invoke(label, coins);
        }

        /// <summary>A coin was picked up. Returns what it is worth with combo and double coins applied.</summary>
        /// <summary>Value multiplier of the current track surface (set by the manager).</summary>
        public float CoinFactor = 1f;

        public int CollectCoin()
        {
            Combo++;
            _comboLeft = _t.comboWindow;
            int oldMult = Multiplier;
            Multiplier = Mathf.Clamp(1 + Combo / Mathf.Max(1, _t.comboPerStep), 1, _t.maxMultiplier);
            if (Multiplier > oldMult) Raise(Cue.Combo);
            AddFrenzy(_t.frenzyPerCoin * Multiplier);
            Stats.bestMultiplier = Mathf.Max(Stats.bestMultiplier, Multiplier);
            Boost01 = Mathf.Min(1f, Boost01 + _t.meterPerComboCoin * Multiplier);
            int value = Mathf.Max(1, Mathf.RoundToInt(_t.coinValue * Multiplier * (DoubleLeft > 0f ? 2 : 1) * (Frenzying ? _t.frenzyCoinMultiplier : 1) * CoinFactor));
            Stats.coins += value;
            return value;
        }

        public void CollectPower(Collectible.Type type)
        {
            switch (type)
            {
                case Collectible.Type.Shield: Shield = true; Bonus?.Invoke("SHIELD", 0); Raise(Cue.Shield); break;
                case Collectible.Type.Magnet: MagnetLeft = _t.magnetTime; Bonus?.Invoke("MAGNET", 0); Raise(Cue.Magnet); break;
                case Collectible.Type.Double: DoubleLeft = _t.doubleTime; Bonus?.Invoke("DOUBLE COINS", 0); Raise(Cue.Double); break;
                case Collectible.Type.SlowMo: SlowLeft = _t.slowMoTime; Bonus?.Invoke("SLOW MOTION", 0); Raise(Cue.SlowMo); break;
                case Collectible.Type.Ghost: GhostLeft = _t.ghostTime; Bonus?.Invoke("GHOST ROAD: FREE INK", 0); Raise(Cue.Ghost); break;
                case Collectible.Type.Gem: Stats.gems++; Bonus?.Invoke("GEM", 0); Raise(Cue.Gem); break;
                case Collectible.Type.Ring:
                    Stats.rings++;
                    BoostLeft = Mathf.Max(BoostLeft, _t.ringBoostSeconds);
                    AddFrenzy(_t.frenzyPerRing);
                    Pay("RING", _t.ringCoins);
                    Raise(Cue.Ring);
                    break;
            }
        }

        /// <summary>Cost in coins the next boost would charge: 0 when the free meter is full.</summary>
        public int NextBoostCost => Boost01 >= 1f ? 0 : _t.boostCoinCost;
        public bool BoostReady(int coinsAvailable) => _cooldown <= 0f && (Boost01 >= 1f || coinsAvailable >= _t.boostCoinCost);

        /// <summary>Starts a boost if allowed. coinsPaid is what the caller must deduct from the wallet.</summary>
        public BoostResult TryBoost(int coinsAvailable, out int coinsPaid)
        {
            coinsPaid = 0;
            if (_cooldown > 0f) return BoostResult.None;
            BoostResult result;
            if (Boost01 >= 1f) { Boost01 = 0f; result = BoostResult.Free; }
            else if (coinsAvailable >= _t.boostCoinCost) { coinsPaid = _t.boostCoinCost; result = BoostResult.Paid; }
            else return BoostResult.Refused;
            BoostLeft = _t.boostDuration + _t.boostDurationPerLevel * (_boostLevel - 1);
            _cooldown = BoostLeft + _t.boostCooldown;
            Stats.boosts++;
            Raise(Cue.Boost);
            return result;
        }

        /// <summary>Charges the frenzy meter; when it fills, frenzy starts at once (ignored while one is running).</summary>
        public void AddFrenzy(float amount)
        {
            if (Frenzying || amount <= 0f) return;
            Frenzy01 = Mathf.Min(1f, Frenzy01 + amount);
            if (Frenzy01 < 1f) return;
            FrenzyLeft = _t.frenzyDuration;
            Stats.frenzies++;
            Bonus?.Invoke("FRENZY!", 0);
            Raise(Cue.FrenzyOn);
        }

        /// <summary>A hazard passed close to the car without hitting it: a skill bonus.</summary>
        public void NearMiss()
        {
            Stats.nearMisses++;
            AddFrenzy(_t.frenzyPerNearMiss);
            Pay("NEAR MISS", _t.nearMissCoins);
            Raise(Cue.NearMiss);
        }

        /// <summary>A crash happened: a shield soaks it up once. Returns true when the hit was absorbed.</summary>
        public bool AbsorbHit()
        {
            if (!Shield) return false;
            Shield = false;
            Bonus?.Invoke("SHIELD SAVED YOU", 0);
            return true;
        }

        /// <summary>Short text for the HUD: active power-ups with seconds left. Rebuilt only when it changes.</summary>
        public string PowerText()
        {
            var sb = new System.Text.StringBuilder(64);
            if (Shield) sb.Append("SHIELD  ");
            if (MagnetLeft > 0f) sb.Append("MAGNET ").Append(Mathf.CeilToInt(MagnetLeft)).Append("  ");
            if (DoubleLeft > 0f) sb.Append("x2 COINS ").Append(Mathf.CeilToInt(DoubleLeft)).Append("  ");
            if (SlowLeft > 0f) sb.Append("SLOW ").Append(Mathf.CeilToInt(SlowLeft)).Append("  ");
            if (Frenzying) sb.Append("FRENZY ").Append(Mathf.CeilToInt(FrenzyLeft)).Append("  ");
            if (GhostLeft > 0f) sb.Append("GHOST ").Append(Mathf.CeilToInt(GhostLeft)).Append("  ");
            return sb.ToString().TrimEnd();
        }
    }
}
