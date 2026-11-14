using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    public enum CatalogKind { Vehicle, Surface, World }

    /// <summary>Review state copied from the asset manifest. Only Have and Ready are approved for release builds.</summary>
    public enum CatalogStatus { Have = 0, Ready = 1, Forged = 2 }

    /// <summary>One vehicle, surface or world that exists as art in the project. Data only: nothing here drives physics.</summary>
    [Serializable]
    public class CatalogEntry
    {
        public string id;
        public CatalogKind kind;
        public string displayName;
        public string group;
        public CatalogStatus status;
        [Tooltip("1 to 5 from the forge or inspector; 0 = not rated.")]
        public int quality;
        public string note;
        public string licence;
        public Sprite preview;

        [Header("Play data (filled by CatalogBuilder)")]
        [Tooltip("False = art exists but nothing can use it on the track yet; playNote says why.")]
        public bool playable;
        public string playNote;
        [Tooltip("Coins to unlock a vehicle. 0 = free.")]
        public int cost;
        [Tooltip("Vehicle: world width of the body picture, mass, torque per wheel, speed multiplier.")]
        public float widthUnits = 2.4f;
        public float mass = 1.45f;
        public float torque = 34f;
        public float speed = 1f;
        [Tooltip("Surface: road atlas strip (2 curb + 4 body + 20 skirt rows), or a legacy RoadStyle (-1 = use the strip).")]
        public Texture2D strip;
        public int roadStyle = -1;
        [Tooltip("World: four layers back to front (sky, far, mid, near), or a legacy EnvironmentTheme with remix (-1 = use the layers).")]
        public Sprite[] layers;
        public int themeIndex = -1;
        public float friction = 1.25f;
        public float bounce = 0f;

        /// <summary>Approved by the inspector (or present before the asset agents existed).</summary>
        public bool Reviewed => status != CatalogStatus.Forged;
        public bool Placeholder => quality > 0 && quality <= 2;
    }

    /// <summary>
    /// Everything in the asset manifest that has art, grouped for browsing. Built by Road Builder > Catalog > Build Catalog
    /// from Assets/Art/Catalog/catalog.json (written by tools/assets/make_previews.py). Per the asset rules, release builds
    /// show only reviewed entries; the Editor and development builds also show unreviewed ones so they can be judged.
    /// </summary>
    public class AssetCatalog : ScriptableObject
    {
        public const string ResourcePath = "Catalog/AssetCatalog";

        public List<CatalogEntry> entries = new List<CatalogEntry>();

        private static AssetCatalog _cached;

        public static bool ShowUnreviewed => Application.isEditor || Debug.isDebugBuild;

        public static AssetCatalog Load()
        {
            if (_cached == null) _cached = Resources.Load<AssetCatalog>(ResourcePath);
            return _cached;
        }

        public bool IsVisible(CatalogEntry e) => e != null && e.preview != null && (e.Reviewed || ShowUnreviewed);

        /// <summary>Entries of a kind, optionally of one group, that may be shown in this build.</summary>
        public List<CatalogEntry> Query(CatalogKind kind, string group = null)
        {
            var list = new List<CatalogEntry>();
            foreach (var e in entries)
            {
                if (e.kind != kind || !IsVisible(e)) continue;
                if (!string.IsNullOrEmpty(group) && e.group != group) continue;
                list.Add(e);
            }
            return list;
        }

        /// <summary>Group names in first-seen order, for the tab row.</summary>
        public List<string> Groups(CatalogKind kind)
        {
            var groups = new List<string>();
            foreach (var e in entries)
                if (e.kind == kind && IsVisible(e) && !groups.Contains(e.group)) groups.Add(e.group);
            return groups;
        }

        public CatalogEntry Find(string id)
        {
            foreach (var e in entries) if (e.id == id) return e;
            return null;
        }
    }
}
