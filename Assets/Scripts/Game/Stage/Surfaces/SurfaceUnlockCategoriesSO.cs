using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>Surface unlock categories (blueprint D4 surface unlocks): which surfaces belong to which tier and the distance that opens it. Pure data; the progression layer reads it.</summary>
    [CreateAssetMenu(menuName = "Road Builder/Stage/Surface Unlock Categories", fileName = "SurfaceUnlockCategories")]
    public class SurfaceUnlockCategoriesSO : ScriptableObject
    {
        [Serializable]
        public struct Category
        {
            public string id, displayName;
            [Tooltip("Cumulative lifetime distance that opens the category (the low end of the blueprint range; the surface list may be staggered by the progression layer).")] public int minDistanceM, maxDistanceM;
            [Tooltip("The extra gate of the blueprint table, as text (surface tutorial, 5 missions, boss reward, world mastery).")] public string extraGate;
            public string[] surfaceIds;
        }
        public Category[] categories = new Category[0];

        public int IndexOf(string surfaceId)
        {
            if (categories == null) return -1;
            for (int i = 0; i < categories.Length; i++)
                if (categories[i].surfaceIds != null) for (int j = 0; j < categories[i].surfaceIds.Length; j++) if (categories[i].surfaceIds[j] == surfaceId) return i;
            return -1;
        }

        public bool Validate(List<string> errors, ICollection<string> known = null)
        {
            bool ok = true;
            void Fail(string m) { ok = false; if (errors != null) errors.Add(name + ": " + m); }
            var seen = new HashSet<string>();
            int prev = -1;
            if (categories == null) return true;
            foreach (var c in categories)
            {
                if (string.IsNullOrEmpty(c.id)) Fail("category without id");
                if (c.minDistanceM < prev) Fail(c.id + ": categories must open in rising distance order");
                if (c.maxDistanceM < c.minDistanceM) Fail(c.id + ": max below min distance");
                prev = c.minDistanceM;
                if (c.surfaceIds == null) continue;
                foreach (var id in c.surfaceIds)
                {
                    if (!seen.Add(id)) Fail(id + " is in two categories");
                    if (known != null && !known.Contains(id)) Fail("unknown surface " + id);
                }
            }
            return ok;
        }
    }
}
