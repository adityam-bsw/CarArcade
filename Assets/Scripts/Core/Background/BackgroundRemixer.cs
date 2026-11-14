using System;
using UnityEngine;

namespace RoadBuilderGame
{
    /// <summary>One concrete background: which sprite sits on each layer, mirrored or not, shifted by how much, tinted how.</summary>
    public sealed class BackgroundRecipe
    {
        public readonly Sprite[] sprites = new Sprite[BackgroundSkin.LayerCount];
        public readonly float[] scroll = new float[BackgroundSkin.LayerCount];
        public readonly bool[] flip = new bool[BackgroundSkin.LayerCount];
        public readonly float[] offset01 = new float[BackgroundSkin.LayerCount];
        public readonly Color[] tint = new Color[BackgroundSkin.LayerCount];
        public Color fill = new Color(0.1f, 0.1f, 0.12f, 1f);
        public BackgroundSkin baseSkin;
        public TimeOfDay time;
        public string label;
    }

    /// <summary>
    /// Builds new backgrounds out of existing scenes by rules, so a handful of scenes yields hundreds of looks:
    /// layers are mixed only between scenes of the same mood, each layer may be mirrored and shifted, and a time-of-day
    /// tint is applied. Same random source in gives the same recipe out.
    /// </summary>
    public static class BackgroundRemixer
    {
        private static BackgroundSkin[] _all;

        // Per-layer colour multipliers (sky, far, mid, near). Day is untouched artwork.
        private static readonly Color[][] Tints =
        {
            new[] { Color.white, Color.white, Color.white, Color.white },                                                                  // Day
            new[] { new Color(1f, 0.88f, 0.92f), new Color(1f, 0.9f, 0.92f), new Color(1f, 0.94f, 0.94f), new Color(1f, 0.97f, 0.97f) },  // Dawn
            new[] { new Color(1f, 0.74f, 0.58f), new Color(1f, 0.78f, 0.64f), new Color(0.98f, 0.82f, 0.72f), new Color(0.92f, 0.82f, 0.78f) } // Dusk
        };

        public static BackgroundSkin[] All
        {
            get
            {
                if (_all == null || _all.Length == 0) _all = Resources.LoadAll<BackgroundSkin>("Backgrounds");
                return _all;
            }
        }

        public static Mood MoodOf(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.Desert: return Mood.Warm;
                case EnvironmentTheme.CyberNight: return Mood.Dark;
                default: return Mood.Green;
            }
        }

        /// <summary>Make a recipe for a theme. Pass the previous recipe so the next region differs from it.</summary>
        public static BackgroundRecipe Make(EnvironmentTheme theme, System.Random rng, BackgroundRecipe previous)
        {
            var pool = Pool(MoodOf(theme));
            if (pool.Length == 0) return null;

            // Base scene gives the sky and the far layer (they set the light and the haze), and is never the same twice in a row.
            BackgroundSkin baseSkin;
            if (pool.Length > 1 && previous != null)
            {
                // Choose among every scene except the one just shown, so two regions in a row never share a base.
                int pick = rng.Next(pool.Length - 1);
                int skip = Array.IndexOf(pool, previous.baseSkin);
                if (skip >= 0 && pick >= skip) pick++;
                baseSkin = pool[pick];
            }
            else baseSkin = pool[rng.Next(pool.Length)];

            var r = new BackgroundRecipe { baseSkin = baseSkin };
            for (int l = 0; l < BackgroundSkin.LayerCount; l++)
            {
                // Mid and near layers are swapped in from other scenes of the same mood about half the time.
                BackgroundSkin source = baseSkin;
                if (l >= 2 && rng.NextDouble() < 0.5) source = pool[rng.Next(pool.Length)];

                r.sprites[l] = source.Layer(l);
                r.scroll[l] = source.Scroll(l);
                r.flip[l] = rng.NextDouble() < (l == 1 ? 0.4 : 0.5);
                r.offset01[l] = (float)rng.NextDouble();
            }

            r.time = PickTime(rng, MoodOf(theme));
            var tint = Tints[(int)r.time];
            for (int l = 0; l < BackgroundSkin.LayerCount; l++) r.tint[l] = tint[l];

            r.fill = SampleFill(r.sprites[3]);
            r.label = baseSkin.displayName + "/" + r.time;
            return r;
        }

        /// <summary>A recipe that shows one scene exactly as drawn: no mixing, mirroring or tint.</summary>
        public static BackgroundRecipe FromSkin(BackgroundSkin skin)
        {
            var r = new BackgroundRecipe { baseSkin = skin, time = TimeOfDay.Day, label = skin.displayName };
            for (int l = 0; l < BackgroundSkin.LayerCount; l++)
            {
                r.sprites[l] = skin.Layer(l);
                r.scroll[l] = skin.Scroll(l);
                r.tint[l] = Color.white;
            }
            r.fill = SampleFill(r.sprites[3]);
            return r;
        }

        private static BackgroundSkin[] Pool(Mood mood)
        {
            var list = new System.Collections.Generic.List<BackgroundSkin>();
            foreach (var s in All) if (s != null && s.mood == mood) list.Add(s);
            return list.ToArray();
        }

        private static TimeOfDay PickTime(System.Random rng, Mood mood)
        {
            double v = rng.NextDouble();
            if (mood == Mood.Dark) return v < 0.8 ? TimeOfDay.Day : TimeOfDay.Dusk;
            if (v < 0.5) return TimeOfDay.Day;
            if (v < 0.75) return TimeOfDay.Dusk;
            return TimeOfDay.Dawn;
        }

        // The bottom-left texel of the near layer is solid ground: use it to fill below the artwork when the camera dips.
        private static Color SampleFill(Sprite near)
        {
            if (near != null && near.texture != null && near.texture.isReadable)
            {
                var c = near.texture.GetPixel(0, 0);
                c.a = 1f;
                return c;
            }
            return new Color(0.1f, 0.1f, 0.12f, 1f);
        }
    }
}
