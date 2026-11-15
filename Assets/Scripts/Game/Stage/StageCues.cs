using System.Collections.Generic;

namespace RoadBuilderGame
{
    /// <summary>
    /// Cue id registry for stage events (Docs/design/voice-lines.md 3.3) and the placeholder sounds that stand in for them until the voice licence
    /// is decided (brief open question 6). Ids are the contract; the placeholder mapping is the only thing to change when real audio exists.
    /// Banner text is a key table (no localisation system exists yet), 1 to 3 words.
    /// </summary>
    public static class StageCues
    {
        // S1 announcer ids (StageEventDefinition.announcerCueId)
        public const string S1Ice = "vo.rob0.s1.ice", S1Mud = "vo.rob0.s1.mud", S1Meteor = "vo.rob0.s1.meteor", S1Glass = "vo.rob0.s1.glass",
            S1Rings = "vo.rob0.s1.rings", S1Eater = "vo.rob0.s1.eater", S1Boss = "vo.rob0.s1.boss";
        // sound-only ids
        public const string SignIce = "sfx.ice.sign", CueIce = "sfx.ice.cue", SignMud = "sfx.mud.sign", CueMud = "sfx.mud.cue",
            SignMeteor = "sfx.meteor.sign", CueMeteor = "sfx.meteor.cue", SignGlass = "sfx.glass.sign", CueGlass = "sfx.glass.cue",
            SignRings = "sfx.rings.sign", CueRings = "sfx.rings.cue", SignEater = "sfx.eater.sign", CueEater = "sfx.eater.cue",
            SignBoss = "sfx.boss.sign", CueBoss = "sfx.boss.cue";
        public const string EventClear = "vo.rob0.event.clear";
        // hazards (B2): Road-Eater, projectiles, RO-B0, finish. Voice ids use the placeholder sounds below until the voice licence is decided.
        public const string EaterSurge = "vo.rob0.eater.surge", EaterDanger = "vo.rob0.eater.danger", EaterSnap = "vo.rob0.eater.snap", EaterNear = "vo.rob0.eater.nearmiss",
            EaterHit = "vo.rob0.eater.hit", EaterEscape = "vo.rob0.eater.escape", EaterRetreat = "vo.rob0.eater.retreat", EaterEaten = "vo.rob0.eater.eaten",
            ProjBolt = "vo.rob0.proj.bolt", ProjBomb = "vo.rob0.proj.bomb", ProjSaw = "vo.rob0.proj.saw",
            BossPhase = "vo.rob0.boss.phase", BossRing = "vo.rob0.boss.ring", BossDefeat = "vo.rob0.boss.defeat", BossRetreat = "vo.rob0.boss.retreat", BossDeath = "vo.rob0.boss.death",
            Finish = "vo.rob0.finish";
        public const string EaterChew = "sfx.eater.chew", EaterJumpChew = "sfx.eater.jumpChew", EaterEscapeTick = "sfx.eater.escapeTick", EaterPushback = "sfx.eater.pushback",
            FireBolt = "sfx.proj.fire.bolt", FireBomb = "sfx.proj.fire.bomb", FireSaw = "sfx.proj.fire.saw", PhaseFlare = "sfx.boss.phaseFlare";

        /// <summary>Every id this phase may fire. A test checks each definition's cue ids are in this set (and so in the registry doc).</summary>
        public static readonly HashSet<string> Registry = new HashSet<string>
        {
            S1Ice, S1Mud, S1Meteor, S1Glass, S1Rings, S1Eater, S1Boss,
            SignIce, CueIce, SignMud, CueMud, SignMeteor, CueMeteor, SignGlass, CueGlass, SignRings, CueRings, SignEater, CueEater, SignBoss, CueBoss,
            EventClear,
            EaterSurge, EaterDanger, EaterSnap, EaterNear, EaterHit, EaterEscape, EaterRetreat, EaterEaten, ProjBolt, ProjBomb, ProjSaw,
            BossPhase, BossRing, BossDefeat, BossRetreat, BossDeath, Finish,
            EaterChew, EaterJumpChew, EaterEscapeTick, EaterPushback, FireBolt, FireBomb, FireSaw, PhaseFlare
        };

        private static readonly Dictionary<string, string> Banner = new Dictionary<string, string>
        {
            { "banner.ice", "ICE AHEAD" }, { "banner.mud", "MUD AHEAD" }, { "banner.meteor", "METEORS!" }, { "banner.glass", "GLASS ROAD" },
            { "banner.rings", "RING RUN" }, { "banner.eater", "ROAD-EATER" }, { "banner.boss", "RO-B0" },
            { "banner.eater.pushback", "PHEW!" }, { "banner.eater.escape", "ESCAPED" }, { "banner.eater.retreat", "IT WANDERED OFF" },
        };

        /// <summary>Short tag shown on the S2 sign (a word is the non-colour shape channel).</summary>
        private static readonly Dictionary<string, string> Sign = new Dictionary<string, string>
        {
            { "banner.ice", "ICE" }, { "banner.mud", "MUD" }, { "banner.meteor", "ROCKS" }, { "banner.glass", "GLASS" },
            { "banner.rings", "RINGS" }, { "banner.eater", "EATER" }, { "banner.boss", "RO-B0" },
        };

        public static string BannerText(string bannerKey) { return bannerKey != null && Banner.TryGetValue(bannerKey, out var t) ? t : string.Empty; }
        public static string SignText(string bannerKey) { return bannerKey != null && Sign.TryGetValue(bannerKey, out var t) ? t : string.Empty; }

        /// <summary>Placeholder sound for a cue id: the existing SFX file name, and a voice key if a recorded line exists (only meteor today).</summary>
        public static void Placeholder(string cueId, out string sfxKey, out string voiceKey, out float volume)
        {
            voiceKey = null; volume = 0.8f; sfxKey = null;
            if (string.IsNullOrEmpty(cueId)) return;
            if (cueId == S1Meteor) { sfxKey = "meteor_warn"; voiceKey = "meteor"; return; }
            if (cueId.StartsWith("vo.rob0.s1.")) { sfxKey = cueId == S1Rings ? "ring" : "whoosh"; return; }
            if (cueId.EndsWith(".sign")) { sfxKey = "open"; return; }
            if (cueId.EndsWith(".cue")) { sfxKey = "nearmiss"; volume = 0.9f; return; }
            if (cueId == EventClear) { sfxKey = "claim"; volume = 0.7f; return; }
            // hazard cues (B2): tells use the alarm, results the claim or ring sound, fire cues a whoosh, eater loops the soft open sound
            if (cueId == EaterSurge || cueId == EaterDanger || cueId == EaterSnap || cueId == ProjBolt || cueId == ProjBomb || cueId == ProjSaw || cueId == BossPhase) { sfxKey = "meteor_warn"; volume = 0.85f; }
            else if (cueId == EaterEscape || cueId == EaterRetreat || cueId == BossDefeat || cueId == BossRetreat || cueId == Finish) { sfxKey = "claim"; volume = 0.8f; }
            else if (cueId == BossRing) { sfxKey = "ring"; volume = 0.8f; }
            else if (cueId == EaterNear) { sfxKey = "nearmiss"; volume = 0.8f; }
            else if (cueId == EaterHit || cueId == EaterEaten || cueId == BossDeath) { sfxKey = "explosion"; volume = 0.8f; }
            else if (cueId.StartsWith("sfx.proj.fire.") || cueId == PhaseFlare) { sfxKey = "whoosh"; volume = 0.7f; }
            else if (cueId.StartsWith("sfx.eater.")) { sfxKey = "open"; volume = 0.6f; }
        }
    }
}
