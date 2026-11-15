namespace RoadBuilderGame
{
    public struct HazardPay { public int Coins, Gems; public float Frenzy; public bool First; public string Label; }

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
    }
}
