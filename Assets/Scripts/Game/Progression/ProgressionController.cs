using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>What a hub action wants the game to do besides changing the profile.</summary>
    public enum HubEffect { None, StartDaily }

    /// <summary>
    /// Builds the progression hub pages (mastery, journey, daily, weekly, album, shop, unlocks, workshop) from the profile and the data assets, and carries out
    /// their actions. Plain logic over <see cref="PlayerProfile"/>; no scene objects. The manager owns the wallet: it copies its coin balance into the profile
    /// before calling and back afterwards.
    /// </summary>
    public sealed class ProgressionController
    {
        public PlayerProfile P;
        public ProgressionTuningSO T;
        public VehicleGateSO Vehicles;
        public WorldGateSO Worlds;
        public SurfaceGateSO Surfaces;
        public VehicleProfileSO Profiles;
        public IPurchaseService Purchases = FakePurchaseService.CreateDefault();
        public Func<DateTime> Utc = () => DateTime.UtcNow;
        public EventCompatibilityMatrix Matrix;
        public string SelectedVehicle = "";

        public ProgressionController(PlayerProfile p)
        {
            P = p; T = ProgressionTuningSO.Load(); Vehicles = VehicleGateSO.Load(); Worlds = WorldGateSO.Load(); Surfaces = SurfaceGateSO.Load(); Profiles = VehicleProfileSO.Load();
        }

        private static string Name(string id)
        {
            var e = AssetCatalog.Load()?.Find(id);
            string n = e != null && !string.IsNullOrEmpty(e.displayName) ? e.displayName : (id ?? "").Replace("vehicle-", "").Replace("world-", "").Replace("surface-", "").Replace('-', ' ');
            return n.ToUpperInvariant();
        }

        // ------------------------------------------------------------------ what is waiting (the dot on the Home button)
        public int Waiting()
        {
            var now = Utc();
            int n = FirstHourTracker.ClaimableCount(P, T) + AchievementService.ClaimableCount(P, T) + AlbumService.ClaimableCount(P, T, now) + (WeeklyService.CanClaimTokens(P, T, now) ? 1 : 0);
            foreach (var d in WeeklyService.Challenges(T, now)) if (WeeklyService.Progress(P, now, d) >= d.target && !P.HasClaim(WeeklyService.ClaimId(now, d.id))) n++;
            return n;
        }

        // ------------------------------------------------------------------ pages
        public HubView Build(HubPage page)
        {
            switch (page)
            {
                case HubPage.Mastery: return Mastery();
                case HubPage.Journey: return Journey();
                case HubPage.Daily: return Daily();
                case HubPage.Weekly: return Weekly();
                case HubPage.Album: return Album();
                case HubPage.Shop: return Shop();
                case HubPage.Unlocks: return UnlocksPage();
                default: return Workshop();
            }
        }

        private HubView Mastery()
        {
            var v = new HubView { page = HubPage.Mastery, title = "MASTERY", subtitle = Name(SelectedVehicle) };
            var vp = Profiles.Find(SelectedVehicle);
            if (vp == null) { v.footer = "PICK A GARAGE VEHICLE TO SEE ITS MASTERY"; return v; }
            var prog = P.ProgressFor(SelectedVehicle);
            v.subtitle = Name(SelectedVehicle) + "   " + Mathf.FloorToInt(prog.metres) + " M DRIVEN";
            v.Add("STRENGTH AND WEAKNESS", VehicleEconomy.ProfileLine(Profiles, Vehicles, SelectedVehicle), "", "", false, true);
            for (int i = 0; i < T.mastery.Length; i++)
            {
                var m = T.mastery[i];
                bool done = (prog.masteryClaimedMask & (1 << i)) != 0 || prog.metres >= m.metres;
                string title = "LEVEL " + (i + 1) + "   " + m.metres + " M";
                string reward = m.reward == MasteryReward.Coins ? "+" + m.amount + " COINS" : m.reward == MasteryReward.Gem ? "+" + m.amount + " GEM" : m.reward == MasteryReward.Sticker ? "MASTER STICKER" : m.reward == MasteryReward.Colour ? "UNIQUE COLOUR" : "GOLD TRIM";
                var row = v.Add(title, reward + (done ? "   REACHED" : "   " + Mathf.CeilToInt(m.metres - prog.metres) + " M TO GO"), "", "", false, done);
                if (m.reward == MasteryReward.Coins || m.reward == MasteryReward.Gem) continue;
                string id = Cosmetics.MasteryId(m.reward, SelectedVehicle);
                if (Cosmetics.Owned(P, id)) { row.button = Cosmetics.IsEquipped(P, SelectedVehicle, id) ? "ON" : "EQUIP"; row.action = "equip:" + id; row.enabled = true; }
            }
            string goalName = GoalName(vp.goalId);
            int have = Mathf.Min(P.Counter(SelectedVehicle + ":" + vp.goalId), vp.goalTarget);
            string gid = Cosmetics.GoalStickerId(SelectedVehicle);
            var g = v.Add("GOAL: " + vp.goalTarget + " " + goalName, have + " / " + vp.goalTarget + "   REWARD: GOAL STICKER", "", "", false, have >= vp.goalTarget);
            if (Cosmetics.Owned(P, gid)) { g.button = Cosmetics.IsEquipped(P, SelectedVehicle, gid) ? "ON" : "EQUIP"; g.action = "equip:" + gid; g.enabled = true; }
            v.footer = "STICKERS, COLOURS AND TRIM SHOW ON THE CAR";
            return v;
        }

        public static string GoalName(string goal)
        {
            switch (goal) { case "fragile-breaks": return "FRAGILE ROADS BROKEN"; case "near-misses": return "NEAR MISSES"; case "low-gravity-stages": return "LOW-GRAVITY STAGES"; default: return "CLEAN LANDINGS"; }
        }

        private HubView Journey()
        {
            var v = new HubView { page = HubPage.Journey, title = "YOUR JOURNEY", subtitle = "A GUIDE, NOT A TO-DO LIST. DO IT IN ANY ORDER." };
            for (int i = 0; i < T.firstHour.Length; i++)
            {
                var s = T.firstHour[i];
                bool done = FirstHourTracker.IsDone(P, s.id), claimed = P.firstHourClaimed.Contains(s.id);
                string reward = s.coins > 0 || s.gems > 0 ? (s.coins > 0 ? "+" + s.coins + " COINS" : "") + (s.gems > 0 ? "  +" + s.gems + " GEM" : "") : "";
                var r = v.Add(s.note.ToUpperInvariant(), "ABOUT MINUTE " + s.minute + (reward.Length > 0 ? "   " + reward : ""), "", "", false, done && (claimed || reward.Length == 0));
                if (done && !claimed && reward.Length > 0) { r.button = "CLAIM"; r.action = "first:" + s.id; r.enabled = true; }
                else if (claimed) r.button = "TAKEN";
            }
            if (T.achievements != null)
                foreach (var a in T.achievements)
                {
                    bool done = AchievementService.Done(P, T, a), claimed = AchievementService.Claimed(P, a);
                    float cur = Mathf.Min(AchievementService.Value(P, T, a.metric), a.target);
                    var r = v.Add("GEMS: " + a.label, Mathf.FloorToInt(cur) + " / " + a.target + "   +" + a.gems + " GEM", "", "", false, claimed);
                    if (done && !claimed) { r.button = "CLAIM"; r.action = "ach:" + a.id; r.enabled = true; }
                    else if (claimed) r.button = "TAKEN";
                }
            v.footer = "GEMS COME FROM PLAYING";
            return v;
        }

        private HubView Daily()
        {
            var now = Utc();
            var d = DailyChallenge.Build(now, T, Matrix);
            var v = new HubView { page = HubPage.Daily, title = "DAILY CHALLENGE", subtitle = "SAME RUN FOR EVERYONE TODAY" };
            string ev = d.eventIds != null && d.eventIds.Length > 0 ? string.Join(", ", d.eventIds) : "RANDOM EVENTS";
            bool today = P.dailyDate == DailyChallenge.DateKey(now);
            bool claimable = DailyChallenge.CanClaim(P, now), claimed = today && P.dailyClaimed;
            var r = v.Add(Name(d.vehicleId) + " IN " + Name(d.worldId), "EVENTS: " + ev + "   REWARD +" + d.rewardCoins + " COINS, 1 MASTERY POINT, 1 WEEKLY TOKEN", claimed ? "DONE" : (claimable ? "CLAIM" : "PLAY"), claimable ? "daily:claim" : "daily:play", !claimed, claimed);
            v.Add("YOUR BEST TODAY", (today ? P.dailyBest : 0) + " M", "", "", false, false);
            var top = LeaderboardService.Top(P, DailyChallenge.DateKey(now), T.leaderboardSize);
            if (top.Count == 0) v.Add("LEADERBOARD (THIS DEVICE)", "NO RUNS YET TODAY", "", "", false);
            for (int i = 0; i < top.Count; i++) v.Add("#" + (i + 1) + "   " + top[i].score + " M", Name(top[i].vehicleId), "", "", false, i == 0);
            v.footer = "LOCAL LEADERBOARD: TOP " + T.leaderboardSize + " OF TODAY";
            return v;
        }

        private HubView Weekly()
        {
            var now = Utc();
            WeeklyService.Roll(P, now);
            var v = new HubView { page = HubPage.Weekly, title = "WEEKLY CHALLENGES", subtitle = "WEEK OF " + WeeklyService.Key(now) };
            bool canTok = WeeklyService.CanClaimTokens(P, T, now), tokDone = P.HasClaim("weeklytokens:" + WeeklyService.Key(now));
            v.Add("WEEKLY TOKENS " + Mathf.Min(P.weeklyTokens, T.weeklyTokensNeeded) + " / " + T.weeklyTokensNeeded, "REWARD +" + T.weeklyCoins + " COINS, +" + T.weeklyGems + " GEMS", tokDone ? "TAKEN" : (canTok ? "CLAIM" : "KEEP GOING"), "weekly:tokens", canTok, tokDone);
            foreach (var c in WeeklyService.Challenges(T, now))
            {
                int have = Mathf.Min(WeeklyService.Progress(P, now, c), c.target);
                bool done = have >= c.target, claimed = P.HasClaim(WeeklyService.ClaimId(now, c.id));
                var r = v.Add(c.label, have + " / " + c.target + "   +" + c.coins + " COINS, +1 TOKEN", claimed ? "TAKEN" : (done ? "CLAIM" : "UNDER WAY"), "weekly:" + c.id, done && !claimed, claimed);
            }
            v.footer = "THE DAILY CHALLENGE ALSO GIVES A TOKEN";
            return v;
        }

        private HubView Album()
        {
            var now = Utc();
            AlbumService.Start(P, now);
            int total = T.album != null ? T.album.Length : 0, reached = AlbumService.DayReached(P, now, Math.Max(1, total));
            var v = new HubView { page = HubPage.Album, title = "ROAD TRIP ALBUM", subtitle = "DAY " + reached + " OF " + total + "   ALL FREE, NOTHING EXPIRES" };
            for (int i = 0; i < total; i++)
            {
                var a = T.album[i];
                string what = !string.IsNullOrEmpty(a.cosmeticId) ? "OPTIONAL COSMETIC: " + Cosmetics.Resolve(T, a.cosmeticId).label : "+" + a.coins + " COINS" + (a.gems > 0 ? "  +" + a.gems + " GEM" : "");
                bool claimed = AlbumService.Claimed(P, a.day), open = a.day <= reached;
                v.Add("DAY " + a.day, what, claimed ? "TAKEN" : (open ? "CLAIM" : "DAY " + a.day), "album:" + a.day, open && !claimed, claimed);
            }
            v.footer = "TAKE THEM WHENEVER YOU LIKE";
            return v;
        }

        private HubView Shop()
        {
            var v = new HubView { page = HubPage.Shop, title = "GEM SHOP", subtitle = "COSMETICS ONLY   YOU HAVE " + P.gems + " GEMS", footer = "NO LOOT BOXES. NOTHING HERE MAKES YOU FASTER." };
            if (!T.shopEnabled) { v.footer = "THE SHOP IS CLOSED"; return v; }
            string veh = SelectedVehicle;
            foreach (var c in T.cosmetics)
            {
                bool owned = Cosmetics.Owned(P, c.id);
                if (c.gems <= 0 && !owned) continue;
                string kind = c.kind == CosmeticKind.Colour ? "PAINT" : c.kind == CosmeticKind.Sticker ? "STICKER" : "TRIM";
                if (owned) v.Add(c.label, kind + "   OWNED   FITS " + Name(veh), Cosmetics.IsEquipped(P, veh, c.id) ? "ON" : "EQUIP", "equip:" + c.id, !string.IsNullOrEmpty(veh), false);
                else v.Add(c.label, kind + "   " + c.gems + " GEMS", "BUY " + c.gems, "buy:" + c.id, P.gems >= c.gems);
            }
            if (Purchases != null && Purchases.IsAvailable && T.gemPacks != null)
                foreach (var g in T.gemPacks) v.Add("+" + g.gems + " GEMS", g.priceLabel + "   OPTIONAL, GROWN-UPS ONLY (TEST PURCHASE)", "GET", "pack:" + g.id, true);
            return v;
        }

        private HubView UnlocksPage()
        {
            var v = new HubView { page = HubPage.Unlocks, title = "ROAD UNLOCKS", subtitle = "BASIC, UNSTABLE, ADVANCED, SPECIAL, EXOTIC", footer = "DISTANCE IS YOUR LIFETIME TOTAL" };
            if (Surfaces.gates == null) return v;
            foreach (var g in Surfaces.gates)
            {
                var s = GateService.Surface(P, Surfaces, g.surfaceId);
                string cat = g.category.ToString().ToUpperInvariant();
                if (s.AlreadyUnlocked) { v.Add(Name(g.surfaceId), cat + "   OWNED", "OWNED", "", false, true); continue; }
                string block = GateText.Surface(P, Surfaces, g.surfaceId);
                bool ready = s.CanBuy;
                v.Add(Name(g.surfaceId), cat + "   " + (ready ? g.coins + " COINS" : block + (g.coins > 0 ? "   THEN " + g.coins + " COINS" : "")), ready ? (g.coins > 0 ? "UNLOCK " + g.coins : "UNLOCK") : "LOCKED", "surface:" + g.surfaceId, ready && P.coins >= g.coins);
            }
            return v;
        }

        private HubView Workshop()
        {
            var v = new HubView { page = HubPage.Workshop, title = "SCRAP WORKSHOP", subtitle = P.scrap + " SCRAP", footer = "SCRAP COMES FROM BOSSES, DUPLICATES AND DISMANTLING" };
            string veh = SelectedVehicle;
            var vp = Profiles.Find(veh);
            if (vp != null)
            {
                bool has = ScrapService.HasReinforce(P, veh), can = ScrapService.CanReinforce(Profiles, veh);
                int cost = ScrapService.ReinforceCost(T, Vehicles, veh);
                string weak = can ? VehicleProfileSO.WeaknessText(VehicleProfileSO.WeakAxis(vp)) : "NONE";
                v.Add("REINFORCE " + Name(veh), can ? "REDUCES: " + weak + "   " + cost + " SCRAP" : "THIS VEHICLE HAS NO WEAKNESS", has ? "OWNED" : "BUY " + cost, "reinforce", !has && can && P.scrap >= cost, has);
            }
            if (P.unlockedVehicleIds != null)
                foreach (var id in P.unlockedVehicleIds.ToArray())
                {
                    if (!ScrapService.CanDismantle(id) || id == veh) continue;
                    int gain = ScrapService.DismantleValue(T, Vehicles, id);
                    v.Add("DISMANTLE " + Name(id), "GIVES " + gain + " SCRAP. YOU CAN BUY IT AGAIN; UPGRADES AND MASTERY STAY.", "+" + gain, "dismantle:" + id, true);
                }
            return v;
        }

        // ------------------------------------------------------------------ actions
        /// <summary>Carries out a row action. Returns a short message ("" = nothing happened). Coins are read from and written to the profile.</summary>
        public string Act(string action, out HubEffect effect)
        {
            effect = HubEffect.None;
            if (string.IsNullOrEmpty(action)) return "";
            var now = Utc();
            int c = action.IndexOf(':');
            string head = c < 0 ? action : action.Substring(0, c), arg = c < 0 ? "" : action.Substring(c + 1);
            switch (head)
            {
                case "equip": return Cosmetics.Equip(P, T, SelectedVehicle, arg) ? "STYLE UPDATED" : "";
                case "buy":
                    foreach (var d in T.cosmetics)
                    {
                        if (d.id != arg || d.gems <= 0 || Cosmetics.Owned(P, arg) || P.gems < d.gems) continue;
                        P.gems -= d.gems; Cosmetics.Grant(P, T, arg, SelectedVehicle);
                        return d.label + " BOUGHT";
                    }
                    return "";
                case "pack":
                    foreach (var g in T.gemPacks)
                    {
                        if (g.id != arg || Purchases == null || !Purchases.IsAvailable) continue;
                        bool ok = false; Purchases.Purchase(g.id, r => ok = r);
                        if (!ok) return "";
                        P.gems += g.gems; P.gemsBought += g.gems;
                        return "+" + g.gems + " GEMS";
                    }
                    return "";
                case "first": return FirstHourTracker.IsDone(P, arg) && FirstHour.Claim(P, T, arg) ? "CLAIMED" : "";
                case "ach": return AchievementService.Claim(P, T, arg) ? "GEMS CLAIMED" : "";
                case "daily":
                    if (arg == "play") { effect = HubEffect.StartDaily; return ""; }
                    int coins;
                    return DailyChallenge.Claim(P, now, T, out coins) ? "+" + coins + " COINS" : "";
                case "weekly":
                    if (arg == "tokens") return WeeklyService.ClaimTokens(P, T, now) ? "WEEKLY REWARD" : "";
                    if (arg.Length == 0) return "";
                    return WeeklyService.Claim(P, T, now, arg) ? "CHALLENGE DONE" : "";
                case "album": { int day; return int.TryParse(arg, out day) && AlbumService.Claim(P, T, now, day) ? "ALBUM DAY " + day : ""; }
                case "surface": return GateService.TryUnlockSurface(P, Surfaces, arg) ? Name(arg) + " UNLOCKED" : "";
                case "reinforce": return ScrapService.BuyReinforce(P, T, Vehicles, Profiles, SelectedVehicle) ? "REINFORCED" : "";
                case "dismantle": { int g = ScrapService.Dismantle(P, T, Vehicles, arg); return g > 0 ? "+" + g + " SCRAP" : ""; }
            }
            return "";
        }
    }
}
