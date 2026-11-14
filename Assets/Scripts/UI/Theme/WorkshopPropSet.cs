using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoadBuilderGame.UI
{
    /// <summary>
    /// The stand-alone pixel props of the garage (tools, lamps, robot parts, cans...). Each is its own sprite so a screen can
    /// move, spin or hide it. Built by UIThemeFactory from Assets/Art/UI/Workshop/Props; read by id.
    /// </summary>
    public class WorkshopPropSet : ScriptableObject
    {
        [Serializable] public struct Entry { public string id; public Sprite sprite; }

        public List<Entry> props = new List<Entry>();

        public Sprite Get(string id)
        {
            foreach (var e in props) if (e.id == id) return e.sprite;
            return null;
        }
    }
}
