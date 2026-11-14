using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>A group filter button in the row above the collection grid.</summary>
    public class CollectionTab : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        public string Group { get; private set; }

        public void Bind(string group, string display, bool active, Action<CollectionTab> onClick)
        {
            Group = group;
            label.SetText(display);
            SetActive(active);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }

        public void SetActive(bool active)
        {
            var theme = UIThemeManager.Current;
            background.sprite = theme.GetSprite(active ? SpriteSlot.TabSelected : SpriteSlot.TabNormal);
            background.color = Color.white;
            label.color = theme.GetColor(active ? ColorToken.TextOnAccent : ColorToken.TextPrimary);
        }
    }
}
