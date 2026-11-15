using System;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Section D gap work: tiers and prices, profiles, mastery rewards, gates, earnings, scrap, daily/weekly/album/gems.</summary>
    public class ProgressionGapTests
    {
        private static readonly DateTime Day = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);   // a Wednesday

        private static PlayerProfile Fresh() { var p = new PlayerProfile(); p.Sanitize(); return p; }

        [Test] public void Gates_CoverEveryCatalogueVehicleWorldAndSurface()
        {
            var cat = AssetCatalog.Load();
            Assert.IsNotNull(cat);
            var vg = VehicleGateSO.CreateInstance<VehicleGateSO>(); var wg = ScriptableObject.CreateInstance<WorldGateSO>(); var sg = ScriptableObject.CreateInstance<SurfaceGateSO>();
            foreach (var e in cat.entries)
            {
                if (e.kind == CatalogKind.Vehicle) Assert.IsTrue(vg.TryGet(e.id, out _), "vehicle gate " + e.id);
                if (e.kind == CatalogKind.World) Assert.IsTrue(wg.TryGet(e.id, out _), "world gate " + e.id);
                if (e.kind == CatalogKind.Surface) Assert.IsTrue(sg.TryGet(e.id, out _), "surface gate " + e.id);
            }
        }

        [Test] public void VehiclePrices_SliceAndBlueprintAnchors()
        {
            var vg = ScriptableObject.CreateInstance<VehicleGateSO>();
            void Chk(string id, int m, int c) { Assert.IsTrue(vg.TryGet(id, out var g)); Assert.AreEqual(m, g.unlockMetres, id); Assert.AreEqual(c, g.coins, id); }
            Chk("vehicle-car", 0, 0); Chk("vehicle-sports-car", 0, 600); Chk("vehicle-monster-truck", 0, 2000); Chk("vehicle-bicycle", 500, 1500);
            Chk("vehicle-dune-buggy", 1500, 4000); Chk("vehicle-race-car", 3000, 8000); Chk("vehicle-tank", 8000, 28000); Chk("vehicle-moon-rover", 12000, 50000);
        }

        [Test] public void Profiles_NoVehicleIsStrictlyBetterThanAnother()
        {
            var ps = VehicleProfileSO.Defaults();
            Assert.AreEqual(32, ps.Length);
            foreach (var a in ps) foreach (var b in ps) if (a != b) Assert.IsFalse(VehicleProfileSO.StrictlyBetter(a, b), a.vehicleId + " beats " + b.vehicleId);
        }

        [Test] public void Profiles_DifferentDrawingStrategies()
        {
            var so = ScriptableObject.CreateInstance<VehicleProfileSO>();
            var bike = so.Find("vehicle-bicycle"); var tank = so.Find("vehicle-tank");
            Assert.Less(bike.ink, 1f); Assert.Less(bike.flip, 1f);
            Assert.Greater(tank.impact, 1.4f); Assert.Less(tank.speed, 1f); Assert.IsTrue(tank.crushesFragile);
            Assert.Less(so.Find("vehicle-moon-rover").accel, 0.7f);
        }

        [Test] public void Mastery_GrantsStickerColourTrim_DuplicateGivesScrap()
        {
            var p = Fresh(); var t = ScriptableObject.CreateInstance<ProgressionTuningSO>();
            MasteryService.RecordDistance(p, t, "vehicle-bicycle", 20000f);
            foreach (var id in new[] { "sticker:vehicle-bicycle", "colour:vehicle-bicycle", "trim:vehicle-bicycle" }) Assert.IsTrue(Cosmetics.Owned(p, id), id);
            Assert.IsTrue(Cosmetics.IsEquipped(p, "vehicle-bicycle", "colour:vehicle-bicycle"));
            Assert.AreEqual(0, p.scrap);
            Assert.IsFalse(Cosmetics.Grant(p, t, "sticker:vehicle-bicycle"));
            Assert.AreEqual(t.scrapPerDuplicate, p.scrap);
        }

        [Test] public void Goals_FragileAndLowGravityCountersCreditTheActiveVehicle()
        {
            var p = Fresh(); var t = ScriptableObject.CreateInstance<ProgressionTuningSO>();
            ProgressionEvents.ActiveVehicle = "vehicle-tank";
            ProgressionEvents.Record(p, ProgressionEvent.FragileBroken, 20);
            Assert.IsTrue(MasteryService.GoalDone(p, t, "vehicle-tank", "fragile-breaks"));
            Assert.IsTrue(Cosmetics.CheckGoal(p, t, "vehicle-tank"));
            Assert.IsTrue(Cosmetics.Owned(p, Cosmetics.GoalStickerId("vehicle-tank")));
            RunProgress.Record(p, t, null, new RunSummary { vehicleKey = "vehicle-moon-rover", worldId = "world-moon", distance = 5200f, utc = Day });
            Assert.IsTrue(MasteryService.GoalDone(p, t, "vehicle-moon-rover", "low-gravity-stages"));
            ProgressionEvents.ActiveVehicle = "";
        }

        [Test] public void BossDefeated_PaysScrapFirstThenRepeat()
        {
            var p = Fresh(); var t = ProgressionTuningSO.Load();
            ProgressionEvents.Record(p, ProgressionEvent.BossDefeated);
            Assert.AreEqual(t.scrapPerBossFirst, p.scrap);
            ProgressionEvents.Record(p, ProgressionEvent.BossDefeated);
            Assert.AreEqual(t.scrapPerBossFirst + t.scrapPerBossRepeat, p.scrap);
        }

        [Test] public void SurfaceGates_EnforceDistanceExtraAndCoins()
        {
            var p = Fresh(); var sg = ScriptableObject.CreateInstance<SurfaceGateSO>();
            Assert.IsFalse(GateService.TryUnlockSurface(p, sg, "surface-ice"));
            p.lifetimeDistance = 6000; p.coins = 5000;
            Assert.IsFalse(GateService.TryUnlockSurface(p, sg, "surface-ice"), "needs 5 missions");
            p.AddCounter("gate:Missions", 5);
            Assert.IsTrue(GateService.TryUnlockSurface(p, sg, "surface-ice"));
            Assert.AreEqual(5000 - 2200, p.coins);
            Assert.AreEqual("", GateText.Surface(p, sg, "surface-ice"));
            Assert.AreEqual(SurfaceCategory.Exotic, sgCat(sg, "surface-cloud"));
        }
        private static SurfaceCategory sgCat(SurfaceGateSO s, string id) { s.TryGet(id, out var g); return g.category; }

        [Test] public void WorldGates_LavaAndRhythmFallbackAndWater()
        {
            var p = Fresh(); var t = ProgressionTuningSO.Load();
            Assert.AreEqual(0, GateCounters.Value(p, ExtraGate.SurviveLava));
            RunProgress.Record(p, t, null, new RunSummary { vehicleKey = "vehicle-car", worldId = "world-rolling-hills", surfaceId = "surface-river", distance = 3200f, utc = Day });
            Assert.GreaterOrEqual(GateCounters.Value(p, ExtraGate.SurviveLava), t.fallbackForMissingEvents ? 1 : 0);
            Assert.GreaterOrEqual(GateCounters.Value(p, ExtraGate.WaterMastery), 1);
            Assert.AreEqual(1, GateCounters.MasteredWorlds(p, t));
        }

        [Test] public void Earnings_MedianProfileMeetsD5Bands_AndOldChestsDidNot()
        {
            var g = GameplayTuning.Load(); var t = ProgressionTuningSO.Load();
            foreach (var b in t.runBands)
            {
                float c = EarningsModel.Expected(g, EarningsModel.Profile.Median, b.metres);
                Assert.That(c, Is.InRange(b.minCoins, b.maxCoins), b.metres + " m");
            }
            Assert.Greater(EarningsModel.UncappedChests(g, 8000f), 5000f);   // the old quadratic chest formula alone
        }

        [Test] public void Scrap_DismantleAndReinforce()
        {
            var p = Fresh(); var t = ProgressionTuningSO.Load(); var vg = VehicleGateSO.Load(); var so = VehicleProfileSO.Load();
            p.unlockedVehicleIds.Add("vehicle-bicycle");
            int gain = ScrapService.Dismantle(p, t, vg, "vehicle-bicycle");
            Assert.Greater(gain, 0); Assert.AreEqual(gain, p.scrap);
            Assert.IsFalse(p.unlockedVehicleIds.Contains("vehicle-bicycle"));
            Assert.AreEqual(0, ScrapService.Dismantle(p, t, vg, "vehicle-car"));
            p.scrap = 500;
            float before = VehicleEconomy.Effective(p, t, so, "vehicle-tank").speed;
            Assert.IsTrue(ScrapService.BuyReinforce(p, t, vg, so, "vehicle-tank"));
            Assert.Greater(VehicleEconomy.Effective(p, t, so, "vehicle-tank").speed, before);   // the tank's weakest axis is speed
        }

        [Test] public void Leaderboard_KeepsTopTenPerDay()
        {
            var p = Fresh();
            for (int i = 1; i <= 15; i++) LeaderboardService.Submit(p, "2026-11-18", i * 10, "vehicle-car", 10);
            var top = LeaderboardService.Top(p, "2026-11-18", 10);
            Assert.AreEqual(10, top.Count); Assert.AreEqual(150, top[0].score); Assert.AreEqual(60, top[9].score);
            Assert.AreEqual(10, p.dailyBoard.Count);
        }

        [Test] public void DailyForcedEvents_HookCarriesTheIds()
        {
            ForcedEvents.Current = new FixedForcedEventList(new[] { "EVT-ICE", "EVT-MUD" });
            Assert.IsTrue(ForcedEvents.Active); Assert.AreEqual("EVT-MUD", ForcedEvents.Current.EventIds[1]);
            ForcedEvents.Current = null; Assert.IsFalse(ForcedEvents.Active);
        }

        [Test] public void Weekly_ChallengesProgressClaimAndTokens()
        {
            var p = Fresh(); var t = ScriptableObject.CreateInstance<ProgressionTuningSO>();
            var list = WeeklyService.Challenges(t, Day);
            Assert.AreEqual(5, list.Count);
            RunProgress.Record(p, t, null, new RunSummary { vehicleKey = "vehicle-car", distance = 7000f, cleanJumps = 50, nearMisses = 30, flips = 25, coins = 2000, utc = Day });
            WeeklyService.AddProgress(p, Day, WeeklyKind.BossDefeats, 1);
            int claimed = 0;
            foreach (var d in list) if (WeeklyService.Claim(p, t, Day, d.id)) claimed++;
            Assert.GreaterOrEqual(claimed, 4);
            Assert.IsFalse(WeeklyService.Claim(p, t, Day, list[0].id), "once");
            p.weeklyTokens = t.weeklyTokensNeeded;
            Assert.IsTrue(WeeklyService.ClaimTokens(p, t, Day)); Assert.IsFalse(WeeklyService.ClaimTokens(p, t, Day));
            WeeklyService.Roll(p, Day.AddDays(7)); Assert.AreEqual(0, p.weeklyTokens);
        }

        [Test] public void Album_ThirtyDays_TwentyFreeTenCosmetic_AlwaysClaimable()
        {
            var t = ScriptableObject.CreateInstance<ProgressionTuningSO>(); var p = Fresh();
            Assert.AreEqual(30, t.album.Length);
            int cos = 0; foreach (var d in t.album) if (!string.IsNullOrEmpty(d.cosmeticId)) cos++;
            Assert.AreEqual(10, cos);
            Assert.IsTrue(AlbumService.Claim(p, t, Day, 1));
            Assert.IsFalse(AlbumService.Claim(p, t, Day, 5), "day 5 not reached yet");
            var later = Day.AddDays(200);
            for (int d = 1; d <= 30; d++) AlbumService.Claim(p, t, later, d);
            Assert.AreEqual(0, AlbumService.ClaimableCount(p, t, later));
            Assert.IsTrue(Cosmetics.Owned(p, "paint-gold"));
        }

        [Test] public void Achievements_PayGemsOnce()
        {
            var p = Fresh(); var t = ProgressionTuningSO.Load(); p.lifetimeDistance = 1200;
            Assert.IsTrue(AchievementService.Claim(p, t, "ach-dist-1k")); Assert.AreEqual(1, p.gems);
            Assert.IsFalse(AchievementService.Claim(p, t, "ach-dist-1k"));
        }

        [Test] public void FirstHour_TicksItselfAndClaims()
        {
            var p = Fresh(); var t = ProgressionTuningSO.Load();
            Assert.IsFalse(FirstHourTracker.IsDone(p, "first-500m"));
            p.bestDistance = 600;
            Assert.IsTrue(FirstHourTracker.IsDone(p, "first-500m"));
            Assert.IsTrue(FirstHour.Claim(p, t, "first-500m")); Assert.AreEqual(150 + 500, p.coins);
        }

        [Test] public void Shop_CosmeticsAndFakePurchase()
        {
            var p = Fresh(); var c = new ProgressionController(p) { SelectedVehicle = "vehicle-car", Purchases = new FakePurchaseService() };
            p.gems = 5;
            Assert.AreEqual("SUNSET PAINT BOUGHT", c.Act("buy:paint-sunset", out _));
            Assert.AreEqual(0, p.gems);
            Assert.AreEqual("+20 GEMS", c.Act("pack:gems-small", out _));
            c.Purchases = new NoOpPurchaseService();
            Assert.AreEqual("", c.Act("pack:gems-small", out _));
            foreach (HubPage page in Enum.GetValues(typeof(HubPage))) Assert.IsNotNull(c.Build(page));
        }

        [Test] public void SaveMigration_V2_KeepsFreeVehiclesAndSurface()
        {
            var p = new PlayerProfile { version = 2, selectedSurfaceId = "surface-gravel" };
            p.Sanitize();
            Assert.AreEqual(PlayerProfile.CurrentVersion, p.version);
            Assert.Contains("vehicle-bicycle", p.unlockedVehicleIds); Assert.Contains("surface-gravel", p.unlockedSurfaceIds);
            Assert.IsNotNull(p.cosmeticsOwned); Assert.IsNotNull(p.claims); Assert.IsNotNull(p.dailyBoard);
        }
    }
}
