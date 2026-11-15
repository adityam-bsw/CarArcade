using UnityEngine;

namespace RoadBuilderGame
{
    public struct HazardPay { public int Coins, Gems, Mastery; public float Frenzy; public bool First; public string Label, CosmeticId, TrophyId, CueId; }

    /// <summary>Payouts for clearing the Road-Eater and RO-B0 with the approved repeat half-pay rule (brief section 14). Counters live on <see cref="PlayerProfile"/> and are saved by SaveService; pure functions, no scene.</summary>
    public static class HazardRewards
    {
        public static HazardPay Eater(MonsterDefinitionSO d, PlayerProfile p, bool timeout)
        {
            if (timeout)
            {
                bool first = p.eaterEscapes + p.eaterTimeouts == 0;
                return new HazardPay { Coins = first ? d.timeoutCoinsFirst : d.timeoutCoinsRepeat, Frenzy = d.timeoutFrenzy, First = first, Label = "IT WANDERED OFF" };
            }
            bool f = p.eaterEscapes == 0;
            return new HazardPay { Coins = f ? d.escapeCoinsFirst : d.escapeCoinsRepeat, Frenzy = d.escapeFrenzy, First = f, Label = "ESCAPED" };
        }

        public static void RecordEater(PlayerProfile p, bool timeout) { if (timeout) p.eaterTimeouts++; else p.eaterEscapes++; }

        public static HazardPay Boss(BossDefinitionSO d, PlayerProfile p, bool timeout)
        {
            if (timeout)
            {
                bool first = p.bossDefeats + p.bossTimeouts == 0;
                return new HazardPay { Coins = first ? d.timeoutCoinsFirst : d.timeoutCoinsRepeat, First = first, Label = "RO-B0 GOT BORED" };
            }
            bool f = p.bossDefeats == 0;
            return new HazardPay { Coins = f ? d.defeatCoinsFirst : d.defeatCoinsRepeat, Gems = f ? d.defeatGemFirst : 0, Frenzy = 0.5f, First = f, Label = "OVERLOAD!" };
        }

        public static void RecordBoss(PlayerProfile p, bool timeout) { if (timeout) p.bossTimeouts++; else p.bossDefeats++; }

        /// <summary>Tier defeat: 500 coins + 5 gems first time (guaranteed) with the exclusive cosmetic and +250 coins; repeats pay coins and a mastery point. Gap list B2.</summary>
        public static HazardPay BossTier(BossTierSO t, PlayerProfile p)
        {
            bool first = !p.robotCosmetics.Contains(t.cosmeticId);
            return new HazardPay
            {
                Coins = first ? t.coinsFirst + t.firstBonusCoins : t.coinsRepeat, Gems = first ? t.gemsFirst : 0, Mastery = t.masteryPoints, Frenzy = 0.5f, First = first,
                Label = first ? "FIRST " + t.displayName.ToUpperInvariant() + " DEFEAT!" : "OVERLOAD!", CosmeticId = first ? t.cosmeticId : null, CueId = t.announcerCueId
            };
        }

        public static void RecordBossTier(PlayerProfile p, BossTierSO t)
        {
            p.bossDefeats++;
            if (!p.robotCosmetics.Contains(t.cosmeticId)) p.robotCosmetics.Add(t.cosmeticId);
            p.masteryPoints += Mathf.Max(0, t.masteryPoints);
        }

        /// <summary>Road-Eater defeat through a counterplay option: 250 to 500 coins (repeat pays half), a trophy per world, one mastery point.</summary>
        public static HazardPay EaterDefeat(EaterVariantSO v, PlayerProfile p)
        {
            bool first = !p.eaterTrophies.Contains(v.trophyId);
            return new HazardPay { Coins = first ? v.coinsFirst : v.coinsFirst / 2, Mastery = 1, Frenzy = 0.5f, First = first, Label = "DEFEATED " + v.displayName.ToUpperInvariant(), TrophyId = first ? v.trophyId : null, CueId = v.announcerCueId };
        }

        public static void RecordEaterDefeat(PlayerProfile p, EaterVariantSO v)
        {
            p.eaterEscapes++;
            if (!p.eaterTrophies.Contains(v.trophyId)) p.eaterTrophies.Add(v.trophyId);
            p.masteryPoints += 1;
        }
    }
}
