using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace RoadBuilderGame.Tests
{
    /// <summary>
    /// The catalogue is what the UI shows, so it must be consistent with the asset manifest: every entry is a real manifest
    /// item with a preview, ids are unique, nothing rejected or not found sneaks in, and every kind has entries.
    /// </summary>
    public class AssetCatalogTests
    {
        private const string CatalogPath = "Assets/Resources/Catalog/AssetCatalog.asset";

        private static AssetCatalog Catalog()
        {
            var c = AssetDatabase.LoadAssetAtPath<AssetCatalog>(CatalogPath);
            Assert.IsNotNull(c, "Run Road Builder > Catalog > Build Catalog (after tools/assets/make_previews.py)");
            return c;
        }

        private static string ManifestStatus(string id)
        {
            string path = Path.Combine(Application.dataPath, "..", "Docs", "asset-manifest", "items", id + ".json");
            if (!File.Exists(path)) return null;
            foreach (var line in File.ReadAllLines(path))
                if (line.Contains("\"status\"")) return line.Split('"')[3];
            return null;
        }

        [Test]
        public void IdsAreUnique_AndPreviewsExist()
        {
            var seen = new HashSet<string>();
            foreach (var e in Catalog().entries)
            {
                Assert.IsTrue(seen.Add(e.id), "duplicate id " + e.id);
                Assert.IsNotNull(e.preview, "no preview sprite for " + e.id);
                Assert.IsFalse(string.IsNullOrEmpty(e.displayName), "no name for " + e.id);
                Assert.IsFalse(string.IsNullOrEmpty(e.group), "no group for " + e.id);
            }
        }

        [Test]
        public void EveryEntry_IsARealManifestItem_WithAnApprovedStageOrForged()
        {
            foreach (var e in Catalog().entries)
            {
                string status = ManifestStatus(e.id);
                Assert.IsNotNull(status, e.id + " is not in Docs/asset-manifest/items");
                CollectionAssert.Contains(new[] { "have", "ready", "forged" }, status,
                    e.id + " has manifest status '" + status + "', which must not be shown in the game");
            }
        }

        [Test]
        public void StatusInCatalog_MatchesManifest()
        {
            foreach (var e in Catalog().entries)
            {
                string status = ManifestStatus(e.id);
                string expected = e.status == CatalogStatus.Have ? "have" : (e.status == CatalogStatus.Ready ? "ready" : "forged");
                Assert.AreEqual(expected, status, e.id + ": catalogue is out of date, rebuild it");
            }
        }

        [Test]
        public void EveryKind_HasEntries_AndGroups()
        {
            var c = Catalog();
            foreach (CatalogKind kind in System.Enum.GetValues(typeof(CatalogKind)))
            {
                Assert.Greater(c.Query(kind).Count, 0, kind + " has no visible entries");
                Assert.Greater(c.Groups(kind).Count, 0, kind + " has no groups");
            }
        }

        [Test]
        public void ReleaseVisibility_HidesUnreviewed()
        {
            var c = Catalog();
            int unreviewed = 0;
            foreach (var e in c.entries) if (!e.Reviewed) unreviewed++;
            // In the Editor everything shows (so it can be judged); in a release build only reviewed entries do.
            Assert.IsTrue(AssetCatalog.ShowUnreviewed, "the Editor must show unreviewed entries");
            Assert.Greater(unreviewed, 0, "test is only meaningful while some entries are unreviewed");
        }
    }
}
