using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoadBuilderGame.UI
{
    /// <summary>One item in the collection grid: thumbnail, name and a review badge.</summary>
    public class CollectionCard : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image preview;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private Button button;

        public CatalogEntry Entry { get; private set; }

        public void Bind(CatalogEntry entry, bool selected, Action<CollectionCard> onClick)
        {
            Entry = entry;
            preview.sprite = entry.preview;
            preview.preserveAspect = true;
            nameText.SetText(entry.displayName);

            Refresh();
            SetSelected(selected);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(this));
        }

        /// <summary>Badge and dimming from the entry's state: not playable yet, locked (price), or review status.</summary>
        public void Refresh()
        {
            var entry = Entry;
            if (entry == null) return;
            var theme = UIThemeManager.Current;
            bool usable = entry.playable || entry.kind == CatalogKind.World;
            preview.color = usable ? Color.white : new Color(1f, 1f, 1f, 0.4f);

            if (!usable)
            {
                badgeText.SetText("COMING SOON");
                badgeText.color = theme.GetColor(ColorToken.Danger);
            }
            else if (entry.kind == CatalogKind.Vehicle && !GameServices.Model.IsVehicleUnlocked(entry))
            {
                badgeText.SetText("{0} COINS", entry.cost);
                badgeText.color = theme.GetColor(ColorToken.Accent);
            }
            else if (entry.Placeholder)
            {
                badgeText.SetText("PLACEHOLDER");
                badgeText.color = theme.GetColor(ColorToken.Danger);
            }
            else if (!entry.Reviewed)
            {
                badgeText.SetText("UNREVIEWED");
                badgeText.color = theme.GetColor(ColorToken.Accent);
            }
            else badgeText.SetText(string.Empty);
        }

        public void SetSelected(bool selected)
        {
            var theme = UIThemeManager.Current;
            background.color = theme.GetColor(selected ? ColorToken.Secondary : ColorToken.Surface);
        }
    }
}
