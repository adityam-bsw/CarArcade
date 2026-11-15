using System;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum MissionKind { Distance = 0, Coins = 1, Flips = 2, AirTime = 3, Boosts = 4, Gems = 5, Chests = 6 }

    /// <summary>
    /// Daily reward, daily missions, gems and vehicle mastery. Works on the saved profile only (no scene objects), so it can
    /// be unit tested. The manager calls it at the end of a run and when the player taps claim.
    /// </summary>
    public sealed class MetaProgress
    {
        private readonly PlayerProfile _p;
        private readonly GameplayTuning _t;
        private Func<DateTime> _now = () => DateTime.Now;

        public MetaProgress(PlayerProfile profile, GameplayTuning tuning) { _p = profile; _t = tuning; }
        /// <summary>For tests: replace the clock.</summary>
        public void SetClock(Func<DateTime> now) { _now = now; }

        private string Today => _now().ToString("yyyy-MM-dd");

        // ------------------------------------------------------------------ daily reward
        public bool DailyAvailable => _p.lastDailyClaim != Today;

        /// <summary>Streak day (1 to 7) the next claim will be, continuing a streak only if yesterday was claimed.</summary>
        public int NextDailyDay
        {
            get
            {
                if (_p.lastDailyClaim == _now().AddDays(-1).ToString("yyyy-MM-dd")) return (_p.dailyStreak % 7) + 1;   // streak continues
                if (_p.lastDailyClaim == Today) return Mathf.Max(1, _p.dailyStreak);                                      // already claimed today
                return 1;                                                                                                // streak broken or first claim
            }
        }

        public bool ClaimDaily(out int coins, out int gems)
        {
            coins = gems = 0;
            if (!DailyAvailable) return false;
            int day = NextDailyDay;
            _p.dailyStreak = day;
            _p.lastDailyClaim = Today;
            coins = _t.dailyCoins[Mathf.Clamp(day - 1, 0, _t.dailyCoins.Length - 1)];
            if (day == 7) gems = _t.dailyGemsOnLastDay;
            _p.coins += coins; _p.gems += gems;
            return true;
        }

        // ------------------------------------------------------------------ missions
        public void EnsureMissions()
        {
            if (_p.missionDate == Today && _p.missions.Count > 0) return;
            _p.missionDate = Today;
            _p.missions.Clear();
            var rng = new System.Random(_now().Year * 1000 + _now().DayOfYear);
            var kinds = new System.Collections.Generic.List<MissionKind>((MissionKind[])Enum.GetValues(typeof(MissionKind)));
            for (int i = 0; i < _t.missionsPerDay && kinds.Count > 0; i++)
            {
                var k = kinds[rng.Next(kinds.Count)];
                kinds.Remove(k);
                _p.missions.Add(Make(k, rng));
            }
        }

        private static MissionState Make(MissionKind k, System.Random rng)
        {
            int Pick(params int[] v) => v[rng.Next(v.Length)];
            switch (k)
            {
                case MissionKind.Distance: { int t = Pick(300, 500, 800); return new MissionState { kind = (int)k, target = t, rewardCoins = t / 5 }; }
                case MissionKind.Coins:    { int t = Pick(150, 300, 500); return new MissionState { kind = (int)k, target = t, rewardCoins = t / 3 }; }
                case MissionKind.Flips:    { int t = Pick(3, 5, 8); return new MissionState { kind = (int)k, target = t, rewardCoins = t * 12 }; }
                case MissionKind.AirTime:  { int t = Pick(10, 20, 30); return new MissionState { kind = (int)k, target = t, rewardCoins = t * 4 }; }
                case MissionKind.Boosts:   { int t = Pick(3, 5, 8); return new MissionState { kind = (int)k, target = t, rewardCoins = t * 10 }; }
                case MissionKind.Gems:     { int t = Pick(1, 2, 3); return new MissionState { kind = (int)k, target = t, rewardCoins = 40, rewardGems = 1 }; }
                default:                   { int t = Pick(2, 3, 4); return new MissionState { kind = (int)k, target = t, rewardCoins = t * 25 }; }
            }
        }

        public static string Describe(MissionState m)
        {
            switch ((MissionKind)m.kind)
            {
                case MissionKind.Distance: return "Reach " + m.target + " m in one run";
                case MissionKind.Coins: return "Collect " + m.target + " coins";
                case MissionKind.Flips: return "Land " + m.target + " flips";
                case MissionKind.AirTime: return "Spend " + m.target + " s in the air";
                case MissionKind.Boosts: return "Use boost " + m.target + " times";
                case MissionKind.Gems: return "Find " + m.target + (m.target == 1 ? " gem" : " gems");
                default: return "Open " + m.target + " distance chests";
            }
        }

        /// <summary>Adds a finished run to the missions and the vehicle's mastery.</summary>
        public void RecordRun(RunStats s, float distance, string vehicleId)
        {
            EnsureMissions();
            foreach (var m in _p.missions)
            {
                if (m.claimed) continue;
                switch ((MissionKind)m.kind)
                {
                    case MissionKind.Distance: m.progress = Mathf.Max(m.progress, Mathf.FloorToInt(distance)); break;
                    case MissionKind.Coins: m.progress += s.coins; break;
                    case MissionKind.Flips: m.progress += s.flips; break;
                    case MissionKind.AirTime: m.progress += Mathf.FloorToInt(s.airTime); break;
                    case MissionKind.Boosts: m.progress += s.boosts; break;
                    case MissionKind.Gems: m.progress += s.gems; break;
                    case MissionKind.Chests: m.progress += s.milestones; break;
                }
                m.progress = Mathf.Min(m.progress, m.target);
            }
            _p.gems += s.gems;
            AddXp(string.IsNullOrEmpty(vehicleId) ? "vehicle-start" : vehicleId, Mathf.FloorToInt(distance / 10f) + s.flips * 5);
        }

        public bool ClaimMission(int index, out int coins, out int gems)
        {
            coins = gems = 0;
            EnsureMissions();
            if (index < 0 || index >= _p.missions.Count) return false;
            var m = _p.missions[index];
            if (m.claimed || m.progress < m.target) return false;
            m.claimed = true;
            coins = m.rewardCoins; gems = m.rewardGems;
            _p.coins += coins; _p.gems += gems;
            return true;
        }

        public bool AnythingToClaim()
        {
            EnsureMissions();
            if (DailyAvailable) return true;
            foreach (var m in _p.missions) if (!m.claimed && m.progress >= m.target) return true;
            return false;
        }

        // ------------------------------------------------------------------ mastery
        public int Xp(string id) { foreach (var v in _p.vehicleXp) if (v.id == id) return v.xp; return 0; }
        private void AddXp(string id, int amount)
        {
            if (amount <= 0) return;
            foreach (var v in _p.vehicleXp) if (v.id == id) { v.xp += amount; return; }
            _p.vehicleXp.Add(new VehicleXp { id = id, xp = amount });
        }
        /// <summary>Mastery level: 1 + one level per 100 xp, each level a little harder (100, 200, ...).</summary>
        public static int LevelFromXp(int xp)
        {
            int level = 1, need = 100;
            while (xp >= need) { xp -= need; level++; need += 100; }
            return level;
        }
    }
}
