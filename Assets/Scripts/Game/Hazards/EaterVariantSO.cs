using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum EaterSignature { BurrowEmerge, Sinkhole, LeechJump, MetalPull, BurningGround, SuctionCurrent, StretchSnap, CeilingRocks, ScanLineErase, ShadowTeleport }
    public enum EaterDefeatOption { BoostRingLure, CollapseTrap, HeavyPlatform, ThreePhases, FinalGateEscape }

    /// <summary>
    /// One Road-Eater world variant (blueprint A3): the signature attack, how it is defeated and the reward. Pure data; behaviour lives in the signature and
    /// counterplay modules looked up by enum, never in a switch. Assets in Resources/Stage/Hazards/Eaters, code rows as fallback (<see cref="Defaults"/>).
    /// </summary>
    [CreateAssetMenu(menuName = "Road Builder/Hazards/Eater Variant", fileName = "EATER-NEW")]
    public class EaterVariantSO : ScriptableObject
    {
        public string id = "EATER-HILLS", worldId = "hills", displayName = "Dirt Maw";
        public EaterSignature signature = EaterSignature.BurrowEmerge;
        public EaterDefeatOption defeat = EaterDefeatOption.BoostRingLure;
        public Color tint = new Color(0.45f, 0.32f, 0.2f);
        [Tooltip("Dragon art look: saturation and brightness scale applied after the hue shift to the tint's hue. 0 = follow the tint's own saturation and value (the old behaviour).")]
        public float satScale = 0f, valScale = 0f;
        [Header("Signature attack (telegraph is 1.2 s or more)")]
        public float firstAfterSeconds = 4f, everySeconds = 9f, tellSeconds = 1.2f, activeSeconds = 0.5f, radius = 1.9f, power = 14f, range = 28f;
        [Header("Counterplay")]
        [Tooltip("Boost ring: ring appears this often; taking it with the Eater within lureMaxGap defeats it.")] public float lureEverySeconds = 14f, lureMaxGap = 24f;
        [Tooltip("Collapse span: placed this far ahead, armed by passing boosted or airborne (or heavy), collapses when the Eater front reaches it.")] public float spanEverySeconds = 18f, spanAheadM = 18f, spanLength = 5f;
        public float heavyMass = 1.7f;
        [Tooltip("Three pursuit phases: seconds per phase; surviving all three defeats it.")] public float phaseSeconds = 14f;
        [Tooltip("Final gate: metres after the chase begins; 8 s of survival after crossing defeats it.")] public float gateAfterChaseM = 520f, gateEscapeSeconds = 8f;
        [Header("Reward and announcer")]
        public int coinsFirst = 300;
        public string trophyId = "trophy-eater-hills", announcerCueId = "vo.rob0.eater.defeat.hills", announcerText = "Dirt Maw is full!";

        public static string ShortId(string worldId) { return WorldProjectiles.ShortId(worldId); }

        private static List<EaterVariantSO> _defaults;
        public static IReadOnlyList<EaterVariantSO> Defaults()
        {
            if (_defaults != null) return _defaults;
            _defaults = new List<EaterVariantSO>
            {
                Row("hills", "Dirt Maw", EaterSignature.BurrowEmerge, EaterDefeatOption.BoostRingLure, new Color(0.5f, 0.34f, 0.2f), 300, "Dirt Maw is full!"),
                Row("desert", "Sand Devourer", EaterSignature.Sinkhole, EaterDefeatOption.CollapseTrap, new Color(0.85f, 0.7f, 0.4f), 300, "Sand Devourer sank!"),
                Row("moon", "Lunar Leech", EaterSignature.LeechJump, EaterDefeatOption.HeavyPlatform, new Color(0.55f, 0.5f, 0.75f), 350, "Leech floated away!"),
                Row("mars", "Iron Worm", EaterSignature.MetalPull, EaterDefeatOption.ThreePhases, new Color(0.75f, 0.35f, 0.25f), 350, "Iron Worm is rusted!"),
                Row("volcano", "Magma Maw", EaterSignature.BurningGround, EaterDefeatOption.FinalGateEscape, new Color(0.95f, 0.4f, 0.1f), 400, "Magma Maw cooled off!"),
                Row("underwater", "Abyss Eel", EaterSignature.SuctionCurrent, EaterDefeatOption.BoostRingLure, new Color(0.15f, 0.35f, 0.6f), 400, "Eel swam away!"),
                Row("candy", "Gummy Gobbler", EaterSignature.StretchSnap, EaterDefeatOption.CollapseTrap, new Color(1f, 0.45f, 0.7f), 450, "Gobbler got stuck!"),
                Row("cave", "Stone Jaw", EaterSignature.CeilingRocks, EaterDefeatOption.HeavyPlatform, new Color(0.5f, 0.5f, 0.55f), 450, "Stone Jaw crumbled!"),
                Row("synthwave", "Pixel Eraser", EaterSignature.ScanLineErase, EaterDefeatOption.ThreePhases, new Color(0.9f, 0.2f, 0.9f), 500, "Eraser got deleted!"),
                Row("haunted", "Shadow Hound", EaterSignature.ShadowTeleport, EaterDefeatOption.FinalGateEscape, new Color(0.25f, 0.2f, 0.4f), 500, "Hound ran home!"),
            };
            return _defaults;
        }

        private static EaterVariantSO Row(string world, string name, EaterSignature sig, EaterDefeatOption opt, Color tint, int coins, string line)
        {
            var v = CreateInstance<EaterVariantSO>();
            v.worldId = world; v.displayName = name; v.signature = sig; v.defeat = opt; v.tint = tint; v.coinsFirst = coins; v.announcerText = line;
            v.id = v.name = "EATER-" + world.ToUpperInvariant(); v.trophyId = "trophy-eater-" + world; v.announcerCueId = "vo.rob0.eater.defeat." + world;
            // per-signature numbers (hazards tuning data, one place)
            switch (sig)
            {
                case EaterSignature.BurrowEmerge: v.activeSeconds = 0.5f; v.radius = 2f; break;
                case EaterSignature.Sinkhole: v.activeSeconds = 4f; v.radius = 1.9f; v.power = 12f; break;
                case EaterSignature.LeechJump: v.activeSeconds = 0.7f; v.radius = 1.9f; break;
                case EaterSignature.MetalPull: v.tellSeconds = 1.3f; v.activeSeconds = 2.5f; v.power = 12f; v.range = 24f; break;
                case EaterSignature.BurningGround: v.activeSeconds = 3f; v.radius = 2.2f; break;
                case EaterSignature.SuctionCurrent: v.tellSeconds = 1.3f; v.activeSeconds = 3f; v.power = 10f; v.range = 30f; break;
                case EaterSignature.StretchSnap: v.activeSeconds = 0.35f; v.radius = 1.7f; break;
                case EaterSignature.CeilingRocks: v.activeSeconds = 0.4f; v.radius = 1.5f; break;
                case EaterSignature.ScanLineErase: v.tellSeconds = 1.3f; v.activeSeconds = 1.6f; break;
                case EaterSignature.ShadowTeleport: v.tellSeconds = 1.3f; v.activeSeconds = 0.45f; v.radius = 2f; break;
            }
            return v;
        }

        /// <summary>Visual-only dragon look for worlds that have no variant row (snow/arctic, night city, forest, cloud, toxic): hue, saturation scale and brightness scale applied by the
        /// same hue shader the variants use, so every world's dragon is distinct from the orange Volcano art. No gameplay: those worlds still have no signature attack.</summary>
        public static bool TryLook(string worldId, out Color tint, out float sat, out float val)
        {
            switch (ShortId(worldId))
            {
                case "snow": tint = new Color(0.45f, 0.78f, 1f); sat = 0.55f; val = 1.18f; return true;      // pale ice blue
                case "city": tint = new Color(0.62f, 0.2f, 1f); sat = 0.95f; val = 0.95f; return true;       // neon violet
                case "forest": tint = new Color(0.25f, 0.75f, 0.3f); sat = 0.8f; val = 0.95f; return true;
                case "cloud": tint = new Color(1f, 0.7f, 0.9f); sat = 0.45f; val = 1.2f; return true;        // pastel pink
                case "toxic": tint = new Color(0.72f, 0.92f, 0.1f); sat = 0.9f; val = 1f; return true;       // acid green
            }
            tint = Color.white; sat = val = 1f; return false;
        }

        public static EaterVariantSO ForWorld(string worldId)
        {
            string s = ShortId(worldId); if (s.Length == 0) return null;
            var all = Defaults();
            for (int i = 0; i < all.Count; i++) if (all[i].worldId == s) { var a = Resources.Load<EaterVariantSO>("Stage/Hazards/Eaters/" + all[i].id); return a != null ? a : all[i]; }
            return null;
        }
    }
}
