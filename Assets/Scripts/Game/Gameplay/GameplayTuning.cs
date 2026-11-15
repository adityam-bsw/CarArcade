using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>
    /// Every number that shapes how a run feels: boost, ink, coins, combo, power-ups, bonuses, daily rewards.
    /// Lives in Resources/Gameplay/GameplayTuning.asset so it can be tuned without code. Defaults here are the shipped values.
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Gameplay Tuning", fileName = "GameplayTuning")]
    public class GameplayTuning : ScriptableObject
    {
        [Header("Boost")]
        [Tooltip("Coins one boost costs when the free meter is not full.")] public int boostCoinCost = 5;
        public float boostDuration = 1.4f;
        [Tooltip("Extra seconds per boost upgrade level above 1.")] public float boostDurationPerLevel = 0.08f;
        [Tooltip("Extra target speed while boosting.")] public float boostSpeedBonus = 4.5f;
        public float boostCooldown = 0.8f;
        [Header("Free boost meter (0 to 1; a full meter makes the next boost free)")]
        public float meterPerAirSecond = 0.25f;
        public float meterPerFlip = 0.35f;
        public float meterPerComboCoin = 0.04f;

        [Header("Ink")]
        public float inkRegenPerSec = 1f;
        public float inkPickup = 45f;
        [Tooltip("Below this fraction of the tank an ink pickup is kept within reach of the end of the drawn road.")] public float inkRescueBelow = 0.45f;
        [Tooltip("Furthest (metres) past the end of the road a rescue ink is placed; it is placed at half of what the remaining ink can still draw.")] public float inkRescueMaxMetres = 14f;
        [Tooltip("Above this fraction no ink spawns.")] public float inkNoSpawnAbove = 0.8f;

        [Header("Spawning (percent chance per slot, rest are coins)")]
        public int heartChance = 7;
        public int powerUpChance = 10;
        public int gemChance = 3;
        public int inkChance = 14;

        [Header("Coins and combo")]
        public int coinValue = 10;
        public float comboWindow = 2.2f;
        public int comboPerStep = 5;
        public int maxMultiplier = 5;

        [Header("Power-ups (seconds)")]
        public float magnetTime = 10f;
        public float magnetRadius = 5f;
        public float doubleTime = 12f;
        public float slowMoTime = 6f;
        [Range(0.3f, 1f)] public float slowMoScale = 0.65f;
        public float ghostTime = 8f;

        [Header("Skill bonuses")]
        public float airMinSeconds = 0.8f;
        public int airCoinsPerSecond = 6;
        [Tooltip("Longest air time that still pays (a long fall off the road is not a trick).")] public float airMaxPaidSeconds = 4f;
        public int flipCoins = 25;
        public float flipDegrees = 300f;
        public float milestoneEvery = 250f;
        public int milestoneCoins = 50;
        [Range(0f, 1f)] public float milestoneGemChance = 0.25f;
        public int recordBonus = 100;

        [Header("Frenzy (fills from coins, flips, rings and near misses; then 'overdrive' for a few seconds)")]
        public float frenzyDuration = 7f;
        public float frenzyPerCoin = 0.012f;      // times the combo multiplier
        public float frenzyPerFlip = 0.12f;
        public float frenzyPerAirSecond = 0.04f;
        public float frenzyPerNearMiss = 0.15f;
        public float frenzyPerRing = 0.2f;
        public float frenzySpeedBonus = 3f;
        public int frenzyCoinMultiplier = 2;

        [Header("Boost rings (fly through for a free burst)")]
        public float ringBoostSeconds = 0.9f;
        public int ringCoins = 5;
        [Range(0f, 1f)] public float ringChancePerArc = 0.6f;

        [Header("Meteor storms (sky hazard: warning marker, then a strike)")]
        public float meteorStartDistance = 150f;
        public float meteorIntervalMin = 6f;
        public float meteorIntervalMax = 11f;
        public float meteorWarnSeconds = 1.1f;
        public float meteorBlastRadius = 1.7f;
        [Tooltip("Surviving a strike within this distance of the car counts as a near miss.")] public float nearMissRadius = 4.5f;
        public int nearMissCoins = 8;

        [Header("Daily reward (7 day streak)")]
        public int[] dailyCoins = { 50, 75, 100, 125, 150, 200, 300 };
        public int dailyGemsOnLastDay = 3;
        public int missionsPerDay = 3;

        private static GameplayTuning _cached;

        /// <summary>The tuning asset, or a default instance if the asset has not been created.</summary>
        public static GameplayTuning Load()
        {
            if (_cached != null) return _cached;
            _cached = Resources.Load<GameplayTuning>("Gameplay/GameplayTuning");
            if (_cached == null) _cached = CreateInstance<GameplayTuning>();
            return _cached;
        }
    }
}
