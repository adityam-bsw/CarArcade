using System.Collections.Generic;

namespace RoadBuilderGame
{
    /// <summary>One announcer row (Docs/design/voice-lines.md 3 to 5): priority P0..P4, same-key cooldown, hard length cap and the subtitle variants. No recordings exist: the text is the subtitle and a tell SFX carries the cue.</summary>
    public sealed class VoiceLine
    {
        public readonly string Id; public readonly int Priority; public readonly float Cooldown, MaxSeconds; public readonly string[] Text;
        public VoiceLine(string id, int pri, float cooldown, float max, params string[] text) { Id = id; Priority = pri; Cooldown = cooldown; MaxSeconds = max; Text = text; }
    }

    /// <summary>The announcer cue table, one source for the Announcer, the tests and the recording list. Hazard cues (P0) and story beats (P2) are the recording-critical ones.</summary>
    public static class VoiceLines
    {
        private static Dictionary<string, VoiceLine> _map;
        private static VoiceLine[] _all;

        public static IReadOnlyCollection<VoiceLine> All { get { Build(); return _all; } }
        public static VoiceLine Find(string id) { Build(); return id != null && _map.TryGetValue(id, out var l) ? l : null; }

        private static void Build()
        {
            if (_map != null) return;
            var l = new List<VoiceLine>
            {
                // P0 hazard tells (never cut, 1.0 s)
                new VoiceLine("vo.rob0.proj.bolt", 0, 1.5f, 1f, "Bolt!", "Zap!"), new VoiceLine("vo.rob0.proj.bomb", 0, 1.5f, 1f, "Bomb!", "Boom!"), new VoiceLine("vo.rob0.proj.saw", 0, 1.5f, 1f, "Saw!", "Spinner!"),
                new VoiceLine("vo.rob0.eater.surge", 0, 1.5f, 1f, "Lunge!", "Here it comes!"), new VoiceLine("vo.rob0.eater.danger", 0, 1.5f, 1.2f, "Faster!", "It's close!"), new VoiceLine("vo.rob0.eater.snap", 0, 1.5f, 1f, "Go go go!", "Hurry!"),
                new VoiceLine("vo.rob0.meteor.marker", 0, 1.5f, 1f, "Rock!", "Sky rock!"), new VoiceLine("vo.rob0.glass.crack", 0, 1.5f, 0.8f, "Cracking!", "Move!"),
                new VoiceLine("vo.rob0.s1.eater", 0, 3f, 1.2f, "Road-muncher!", "Something is munching!", "Keep moving!"),
                // P1 warnings
                new VoiceLine("vo.rob0.s1.ice", 1, 3f, 1.2f, "Slippery!", "Ice ahead!"), new VoiceLine("vo.rob0.s1.mud", 1, 3f, 1.2f, "Mud ahead!", "Gooey road!"), new VoiceLine("vo.rob0.s1.glass", 1, 3f, 1.2f, "Glass road!", "Keep moving!"),
                new VoiceLine("vo.rob0.s1.meteor", 1, 3f, 1.2f, "Rock shower!", "Meteors ahead!"),
                // P2 story beats
                new VoiceLine("vo.rob0.s1.boss", 2, 3f, 2.5f, "Inspection time! Hold still!", "RO-B0 is on the case!", "Illegal road detected. Dramatic sigh."),
                new VoiceLine("vo.rob0.s1.rings", 2, 3f, 1.2f, "Rings! Grab them!", "Bonus rings!"),
                new VoiceLine("vo.rob0.boss.phase", 2, 3f, 1.5f, "New rules!", "Stage two!"), new VoiceLine("vo.rob0.boss.defeat", 2, 3f, 2.5f, "Inspection failed! I mean passed!", "My circuits are dizzy. You win!", "Fine! The road is legal. Barely."),
                new VoiceLine("vo.rob0.boss.retreat", 2, 3f, 2f, "I'll be back! With forms!", "Recharging! Do not move!"), new VoiceLine("vo.rob0.boss.death", 2, 3f, 2f, "Inspection complete. Again?", "Ticket for you! Retry?"),
                new VoiceLine("vo.rob0.eater.hit", 2, 3f, 1.5f, "Chomp! Ticket!", "Nibbled!"), new VoiceLine("vo.rob0.eater.escape", 2, 3f, 2.5f, "You escaped! Rude.", "Munchy is full of rubble!", "Whew! Road saved."),
                new VoiceLine("vo.rob0.eater.retreat", 2, 3f, 2f, "It wandered off!", "Munchy got bored!"), new VoiceLine("vo.rob0.eater.eaten", 2, 3f, 2f, "Munched! Try again?", "Road eaten. Oops."),
                // P3 reactive
                new VoiceLine("vo.rob0.boss.ring", 3, 6f, 1f, "Grab the ring!", "Gold ring!"), new VoiceLine("vo.rob0.eater.nearmiss", 3, 6f, 2f, "Munchy missed you!", "That was a nibble!"),
                new VoiceLine("vo.rob0.nearmiss", 3, 6f, 1.5f, "Ooh, close!", "Whoa, that was tight!", "Almost a ticket!"), new VoiceLine("vo.rob0.dodge", 3, 6f, 1.5f, "Hmph. Lucky.", "You dodged my paperwork!", "Fine. Nice move."),
                new VoiceLine("vo.rob0.event.clear", 3, 6f, 1.5f, "All clear.", "Back to normal.", "Phew, done."), new VoiceLine("vo.rob0.finish", 3, 6f, 1.5f, "Finish line!", "Inspection passed!"),
                // world projectile tells (P0, 1.0 s) and Road-Eater world defeats (P2); one line per row of the B2 and A3 tables
            };
            foreach (var p in WorldProjectiles.Defaults()) l.Add(new VoiceLine("vo.rob0.proj." + p.worldId, 0, 1.5f, 1f, WorldTell(p)));
            foreach (var v in EaterVariantSO.Defaults()) l.Add(new VoiceLine(v.announcerCueId, 2, 3f, 2.5f, v.announcerText));
            _all = l.ToArray(); _map = new Dictionary<string, VoiceLine>();
            for (int i = 0; i < _all.Length; i++) _map[_all[i].Id] = _all[i];
        }

        private static string WorldTell(ProjectileDefinitionSO p)
        {
            switch (p.pattern)
            {
                case ProjectilePattern.Arc: return "Rock incoming!";
                case ProjectilePattern.Splitting: return "It splits!";
                case ProjectilePattern.Homing: return "It follows!";
                case ProjectilePattern.Bouncing: return "It bounces!";
                case ProjectilePattern.Zigzag: return "Zigzag!";
                default: return "Straight shot!";
            }
        }
    }
}
