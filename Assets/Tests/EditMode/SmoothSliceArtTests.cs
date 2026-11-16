using NUnit.Framework;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>The smooth slice vehicles and pickups are a pure art swap: same sizes, colliders and physics numbers as the pixel ones.</summary>
    public class SmoothSliceArtTests
    {
        private static readonly VehicleType[] Types = { VehicleType.ClassicRed, VehicleType.SportCyan, VehicleType.MonsterTruck };

        [Test]
        public void Asset_ReferencesEverySliceSprite()
        {
            var a = SmoothSliceArt.Load();
            Assert.IsNotNull(a, "Resources/Smooth/SmoothSliceArt missing (menu Road Builder > Smooth > Build Countryside Skins)");
            foreach (var t in Types) { Assert.IsNotNull(a.Body(t), "body " + t); Assert.IsNotNull(a.Wheel(t), "wheel " + t); }
            foreach (var n in new[] { "Coin", "Heart", "InkCanister", "Pickup_Ring", "Pickup_Gem", "Pickup_Magnet", "Pickup_Shield", "Pickup_Double", "Pickup_SlowMo", "Pickup_Ghost" }) Assert.IsNotNull(a.Pickup(n), n);
            Assert.IsNotNull(a.starWhite, "neutral white star for ParticleManager tinting");
            Assert.IsNull(a.Pickup("Pickup_Unknown"), "a name without smooth art keeps the original");
            Assert.AreNotEqual(a.Wheel(VehicleType.ClassicRed), a.Wheel(VehicleType.MonsterTruck), "each vehicle has its own wheel");
        }

        [Test]
        public void SmoothCar_HasSameCollidersPhysicsAndSizesAsPixelCar()
        {
            foreach (var t in Types)
            {
                var px = RoadCar.Create(Vector2.zero, t, ArtStyle.Pixel);
                var sm = RoadCar.Create(Vector2.zero, t, ArtStyle.Smooth);
                try
                {
                    var pp = px.Body.GetComponent<PolygonCollider2D>().points;
                    var sp = sm.Body.GetComponent<PolygonCollider2D>().points;
                    Assert.AreEqual(pp.Length, sp.Length, t + " polygon");
                    for (int i = 0; i < pp.Length; i++) Assert.AreEqual(pp[i], sp[i], t + " point " + i);
                    Assert.AreEqual(px.Body.mass, sm.Body.mass, t + " mass");
                    Assert.AreEqual(px.maxTorquePerWheel, sm.maxTorquePerWheel, t + " torque");
                    Assert.AreEqual(px.Body.centerOfMass, sm.Body.centerOfMass, t + " centre of mass");

                    var pc = px.GetComponentsInChildren<CircleCollider2D>(); var sc = sm.GetComponentsInChildren<CircleCollider2D>();
                    Assert.AreEqual(pc.Length, sc.Length, t + " circle colliders");
                    for (int i = 0; i < pc.Length; i++)
                    {
                        Assert.AreEqual(pc[i].radius, sc[i].radius, 1e-5f, t + " circle " + i);
                        Assert.AreEqual(pc[i].transform.position, sc[i].transform.position, t + " circle pos " + i);
                    }
                    var ps = px.GetComponentsInChildren<SpriteRenderer>(); var ss = sm.GetComponentsInChildren<SpriteRenderer>();
                    Assert.AreEqual(ps.Length, ss.Length, t + " renderers");
                    for (int i = 0; i < ps.Length; i++)
                    {
                        Assert.AreNotEqual(ps[i].sprite, ss[i].sprite, t + " sprite " + ps[i].name + " should differ");
                        Assert.AreEqual(ps[i].bounds.size.x, ss[i].bounds.size.x, 1e-3f, t + " width " + ps[i].name);
                        Assert.AreEqual(ps[i].bounds.size.y, ss[i].bounds.size.y, 1e-3f, t + " height " + ps[i].name);
                    }
                }
                finally { Object.DestroyImmediate(px.gameObject); Object.DestroyImmediate(sm.gameObject); }
            }
        }

        [Test]
        public void SmoothCars_UseTheirOwnWheelAndPixelCarsKeepSharedWheel()
        {
            var shared = Resources.Load<Sprite>("RoadBuilder/Wheel");
            var a = SmoothSliceArt.Load();
            foreach (var t in Types)
            {
                var px = RoadCar.Create(Vector2.zero, t);
                var sm = RoadCar.Create(Vector2.zero, t, ArtStyle.Smooth);
                try
                {
                    Assert.AreEqual(shared, px.transform.Find("BackWheel/WheelSprite").GetComponent<SpriteRenderer>().sprite, t + " pixel wheel");
                    Assert.AreEqual(a.Wheel(t), sm.transform.Find("BackWheel/WheelSprite").GetComponent<SpriteRenderer>().sprite, t + " smooth wheel");
                    Assert.AreEqual(a.Body(t), sm.transform.Find("Body/BodySprite").GetComponent<SpriteRenderer>().sprite, t + " smooth body");
                }
                finally { Object.DestroyImmediate(px.gameObject); Object.DestroyImmediate(sm.gameObject); }
            }
        }

        [Test]
        public void SmoothPickups_FitTheSameWorldSizeAsPixelOnes()
        {
            var a = SmoothSliceArt.Load();
            var sizes = new (string n, Collectible.Type t)[] { ("Coin", Collectible.Type.Coin), ("Heart", Collectible.Type.Heart), ("InkCanister", Collectible.Type.Ink), ("Pickup_Ring", Collectible.Type.Ring) };
            foreach (var (n, t) in sizes)
            {
                var g1 = new GameObject(); var g2 = new GameObject();
                try
                {
                    g1.AddComponent<Collectible>().Init(t, Resources.Load<Sprite>("RoadBuilder/" + n));
                    g2.AddComponent<Collectible>().Init(t, a.Pickup(n));
                    var b1 = g1.GetComponent<SpriteRenderer>().bounds.size; var b2 = g2.GetComponent<SpriteRenderer>().bounds.size;
                    Assert.AreEqual(b1.x, b2.x, 1e-3f, n + " width"); Assert.AreEqual(b1.y, b2.y, 1e-3f, n + " height");
                    Assert.AreEqual(g1.GetComponent<CircleCollider2D>().radius, g2.GetComponent<CircleCollider2D>().radius, n + " collider");
                }
                finally { Object.DestroyImmediate(g1); Object.DestroyImmediate(g2); }
            }
        }
    }
}
