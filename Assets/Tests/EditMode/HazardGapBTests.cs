using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>Gap list B: world projectiles, boss tiers and rewards, Road-Eater variants and counterplay registry, announcer rules. Pure data and model checks.</summary>
    public class HazardGapBTests
    {
        [Test]
        public void WorldProjectiles_FifteenRows_MatchBlueprint()
        {
            var all = WorldProjectiles.Defaults();
            Assert.AreEqual(15, all.Count);
            var worlds = new HashSet<string>(); var patterns = new HashSet<ProjectilePattern>();
            foreach (var p in all)
            {
                Assert.IsTrue(worlds.Add(p.worldId), p.id); patterns.Add(p.pattern);
                Assert.AreNotEqual(ProjectileSurfaceEffect.None, p.surfaceEffect, p.id);
                Assert.GreaterOrEqual(p.tellSeconds, 0.8f); Assert.LessOrEqual(p.homingTurnRate, 35f, "homing must stay evadable");
            }
            Assert.AreEqual(6, patterns.Count);
            Assert.AreEqual(ProjectilePattern.Arc, WorldProjectiles.ForWorld("world-rolling-hills").pattern);
            Assert.AreEqual(16f, WorldProjectiles.ForWorld("world-synthwave").speed);
            Assert.AreEqual(ProjectilePattern.Homing, WorldProjectiles.ForWorld("haunted").pattern);
            Assert.IsNull(WorldProjectiles.ForWorld(""), "Countryside keeps the archetypes");
        }

        [Test]
        public void SurfaceHook_DefaultsToNoOp_AndCanBeReplaced()
        {
            var old = ProjectileSurfaceHooks.Current;
            ProjectileSurfaceHooks.Current = null; Assert.AreSame(NoOpProjectileSurfaceHook.Instance, ProjectileSurfaceHooks.Current);
            ProjectileSurfaceHooks.Current.OnProjectileSurface(ProjectileSurfaceEffect.Crater, Vector2.zero, 1f, 1f, "x");
            ProjectileSurfaceHooks.Current = old;
        }

        [Test]
        public void BossTiers_PickByDistance()
        {
            var t = BossTierSO.Defaults();
            Assert.AreEqual(BossTierId.Scout, BossTierSO.ForDistance(t, 2000f).tier);
            Assert.AreEqual(BossTierId.Hunter, BossTierSO.ForDistance(t, 3000f).tier);
            Assert.AreEqual(BossTierId.Interceptor, BossTierSO.ForDistance(t, 5000f).tier);
            Assert.AreEqual(BossTierId.Commander, BossTierSO.ForDistance(t, 8000f).tier);
            Assert.AreEqual(BossTierId.Overlord, BossTierSO.ForDistance(t, 12000f).tier);
            Assert.IsTrue(t[2].trapAttack && !t[0].trapAttack);
        }

        [Test]
        public void Boss_TierCounterMeter_DisablesAtTenPoints_AndThreeHitsEndsRound()
        {
            var def = BossDefinitionSO.CreateDefault(); var m = new BossModel(def); m.Reset(3u); m.ApplyTier(BossTierSO.Defaults()[0]); m.Begin();
            Assert.AreEqual(10f, m.MeterMax);
            bool over = false; m.Raised += e => { if (e == BossEvent.Overpowered) over = true; };
            var ctx = new BossContext { CarSpeed = 8f, CounterAvailable = true, InkAvailable = true, RoadAheadForRoller = true };
            for (int i = 0; i < 400; i++) m.Tick(0.05f, ctx);
            m.Resolve(BossOutcome.HitShielded); Assert.AreEqual(0, m.Hits, "a shield blocks one without counting");
            m.Resolve(BossOutcome.Hit); m.Resolve(BossOutcome.Hit); Assert.IsFalse(over);
            m.Resolve(BossOutcome.Hit); Assert.IsTrue(over); Assert.AreEqual(BossState.Retreat, m.State);
        }

        [Test]
        public void Boss_PerfectCounters_DefeatWithinEncounterTime()
        {
            var def = BossDefinitionSO.CreateDefault(); var m = new BossModel(def); m.Reset(5u); m.ApplyTier(BossTierSO.Defaults()[0]); m.Begin();
            bool defeated = false; m.Raised += e => { if (e == BossEvent.Defeated) defeated = true; };
            for (int i = 0; i < 8 && !defeated; i++) m.Resolve(BossOutcome.Perfect);
            Assert.IsTrue(defeated, "10 points need only a handful of clean counters");
            Assert.LessOrEqual(BossTierSO.Defaults()[4].encounterSeconds, 45f);
        }

        [Test]
        public void BossTierRewards_FirstTimeThenRepeat_RecordsCosmetic()
        {
            var p = new PlayerProfile(); var t = BossTierSO.Defaults()[1];
            var a = HazardRewards.BossTier(t, p);
            Assert.AreEqual(500 + 250, a.Coins); Assert.AreEqual(5, a.Gems); Assert.AreEqual(t.cosmeticId, a.CosmeticId);
            HazardRewards.RecordBossTier(p, t);
            Assert.IsTrue(p.robotCosmetics.Contains(t.cosmeticId)); Assert.AreEqual(1, p.masteryPoints);
            var b = HazardRewards.BossTier(t, p);
            Assert.AreEqual(0, b.Gems); Assert.IsNull(b.CosmeticId); Assert.Greater(b.Coins, 0); Assert.Greater(b.Mastery, 0);
        }

        [Test]
        public void EaterVariants_TenWorlds_EverySignatureAndOptionHasAModule()
        {
            var all = EaterVariantSO.Defaults(); Assert.AreEqual(10, all.Count);
            var sigs = new HashSet<EaterSignature>(); var opts = new HashSet<EaterDefeatOption>();
            foreach (var v in all)
            {
                sigs.Add(v.signature); opts.Add(v.defeat);
                Assert.IsNotNull(EaterSignatures.Create(v.signature), v.id); Assert.IsNotNull(EaterCounterplays.Create(v.defeat), v.id);
                Assert.GreaterOrEqual(v.tellSeconds, 1.2f, v.id); Assert.That(v.coinsFirst, Is.InRange(250, 500));
            }
            Assert.AreEqual(10, sigs.Count); Assert.AreEqual(5, opts.Count);
            Assert.AreEqual("Dirt Maw", EaterVariantSO.ForWorld("world-rolling-hills").displayName);
            Assert.AreEqual("Shadow Hound", EaterVariantSO.ForWorld("world-haunted-halloween").displayName);
        }

        [Test]
        public void EaterDefeatReward_TrophyOnce_MasteryEachTime()
        {
            var p = new PlayerProfile(); var v = EaterVariantSO.Defaults()[3];
            var a = HazardRewards.EaterDefeat(v, p); Assert.AreEqual(v.trophyId, a.TrophyId); Assert.AreEqual(1, a.Mastery);
            HazardRewards.RecordEaterDefeat(p, v);
            var b = HazardRewards.EaterDefeat(v, p); Assert.IsNull(b.TrophyId); Assert.AreEqual(v.coinsFirst / 2, b.Coins); Assert.AreEqual(1, p.masteryPoints);
        }

        [Test]
        public void ThreePhaseCounter_DefeatsAfterThreePhases()
        {
            var v = EaterVariantSO.Defaults()[3]; var h = new EaterHost(); var c = new ThreePhaseCounter(); c.Begin(v, h);
            bool done = false; int phases = 1;
            for (float t = 0f; t < v.phaseSeconds * 3f + 1f && !done; t += 0.1f) { done = c.Tick(0.1f); phases = System.Math.Max(phases, c.PursuitPhase); }
            Assert.IsTrue(done); Assert.AreEqual(3, phases);
        }

        [Test]
        public void Announcer_PriorityCooldownAndTelegraphLock()
        {
            var a = new Announcer(); var subs = new List<string>(); var sfx = new List<string>(); bool threat = false;
            a.Subtitle = (t, s, u) => subs.Add(t); a.PlaySfx = id => sfx.Add(id); a.ThreatActive = () => threat;
            Assert.IsTrue(a.Say("vo.rob0.proj.bolt")); Assert.IsFalse(a.Say("vo.rob0.proj.bolt"), "same key cooldown");
            Assert.IsFalse(a.Say("vo.rob0.nearmiss"), "P3 never delays behind a playing P0");
            Assert.IsTrue(a.Say("vo.rob0.proj.bomb"), "a different P0 tell cuts in");
            a.Tick(3f); threat = true; Assert.IsFalse(a.Say("vo.rob0.dodge"), "telegraph lock drops P3");
            threat = false; a.Tick(3f); Assert.IsTrue(a.Say("vo.rob0.dodge"));
            Assert.IsFalse(a.Say("sfx.unknown.cue")); Assert.Contains("sfx.unknown.cue", sfx);
            Assert.GreaterOrEqual(subs.Count, 3);
        }

        [Test]
        public void VoiceLines_CoverEveryHazardCueAndRespectLengthCap()
        {
            foreach (var p in WorldProjectiles.Defaults()) Assert.IsNotNull(VoiceLines.Find("vo.rob0.proj." + p.worldId), p.id);
            foreach (var v in EaterVariantSO.Defaults()) Assert.IsNotNull(VoiceLines.Find(v.announcerCueId), v.id);
            foreach (var l in VoiceLines.All) { Assert.LessOrEqual(l.MaxSeconds, 2.5f, l.Id); Assert.Greater(l.Text.Length, 0, l.Id); }
            foreach (var id in new[] { StageCues.ProjBolt, StageCues.EaterSurge, StageCues.BossDefeat, StageCues.BossPhase }) Assert.IsNotNull(VoiceLines.Find(id), id);
        }
    }
}
