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
        [Header("Surface traits (gameplay): see Docs/GAMEPLAY_RULES.md, Track properties")]
        [Tooltip("Multiplies the car's target speed (rolling resistance: below 1 drags, above 1 is fast).")] public float speedFactor = 1f;
        [Tooltip("Multiplies the ink one metre of road costs (above 1 = expensive to build).")] public float inkFactor = 1f;
        [Tooltip("Multiplies the value of every coin. Risky tracks pay more.")] public float coinFactor = 1f;
        [Tooltip("Vehicle: wheels cut out of the picture (round sprites with centre and radius in picture pixels). Empty = no spinning wheels.")]
        public WheelArt[] wheels;

        [Header("Smooth restyle (vehicles with smooth_status ready only; filled by SmoothVehicleLinker)")]
        [Tooltip("Smooth card/preview picture (wheels baked in). Canvas = old preview x smoothScale.")] public Sprite smoothPreview;
        [Tooltip("Smooth rig body (wheel arches empty). Same canvas as smoothPreview.")] public Sprite smoothBody;
        [Tooltip("Smooth wheel cut-outs; cx, cy, r in smooth body pixels (= old x smoothScale).")] public WheelArt[] smoothWheels;
        public int smoothScale = 1;

        /// <summary>True when smooth art may be used for this vehicle (ready in the manifest and fully linked).</summary>
        [Tooltip("Vehicle: centre of mass x midway between the wheels (long wheelbase offset: bus, semi, camper, fire truck, tractor). Set by SmoothVehicleLinker.")] public bool centerMassOnWheels;
        public bool HasSmooth => smoothBody != null && smoothPreview != null;
        /// <summary>The card picture for an art style. Pixel, or a vehicle without ready smooth art, keeps the old preview.</summary>
        public Sprite PreviewFor(ArtStyle style) => style == ArtStyle.Smooth && HasSmooth ? smoothPreview : preview;

        /// <summary>
        /// Rig data for a vehicle in a world of the given art style. Smooth uses the smooth body and wheels only when ready and fully linked
        /// (same wheel count); widthUnits, mass, torque and speed are the same either way, and the smooth wheel layout is old x scale, so world geometry is identical.
        /// </summary>
        public VehicleRig MakeRig(ArtStyle style)
        {
            bool smooth = style == ArtStyle.Smooth && HasSmooth && smoothWheels != null && (wheels == null || smoothWheels.Length == wheels.Length);
            return new VehicleRig
            {
                id = id, body = smooth ? smoothBody : preview, width = widthUnits, mass = mass, torque = torque, speedMultiplier = speed,
                wheels = smooth ? smoothWheels : wheels, centerMassOnWheels = centerMassOnWheels
            };
        }

        /// <summary>Approved by the inspector (or present before the asset agents existed).</summary>
        public bool Reviewed => status != CatalogStatus.Forged;
        /// <summary>One line for the road picker: what this track does to the run.</summary>
        public string TraitsLine()
        {
            string grip = friction < 0.6f ? "very slippery" : friction < 0.95f ? "slippery" : friction > 1.4f ? "sticky" : friction > 1.2f ? "good grip" : "ok grip";
            string t = grip;
            if (speedFactor <= 0.92f) t += ", slow"; else if (speedFactor >= 1.08f) t += ", fast";
            if (inkFactor <= 0.85f) t += ", cheap ink"; else if (inkFactor >= 1.2f) t += ", costly ink";
            if (coinFactor >= 1.2f) t += ", coins x" + coinFactor.ToString("0.#");
            if (bounce >= 0.1f) t += ", bouncy";
            return t;
        }

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
