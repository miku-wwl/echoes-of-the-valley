using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>The satchel screen (I). Read-only: it lists the real stacks, slot usage and coins from the GameSession.</summary>
    public class InventoryPanel : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] Text summaryText;
        [SerializeField] Text[] rowNames;
        [SerializeField] Text[] rowCounts;
        [SerializeField] Image[] rowIcons;

        public bool IsOpen { get; private set; }
        public int RowCount => rowNames != null ? rowNames.Length : 0;
        public string Summary => summaryText != null ? summaryText.text : string.Empty;
        public GameObject Panel => panel;

        public string RowName(int index) => rowNames[index].text;

        public string RowCountText(int index) => rowCounts[index].text;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        void Update()
        {
            if (IsOpen)
            {
                if (GameFlow.TryConsumePause() || GameInput.InventoryPressed) Close();
                else Refresh();
            }
            else if (GameInput.InventoryPressed && GameFlow.GameplayInputAllowed)
            {
                Open();
            }
        }

        void OnDestroy()
        {
            if (IsOpen) GameFlow.SetPanelOpen(false);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || !GameFlow.GameplayInputAllowed) return;
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            GameFlow.SetPanelOpen(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            GameFlow.SetPanelOpen(false);
        }

        void Refresh()
        {
            GameSession session = GameSession.Current;
            if (session == null) return;

            var stacks = session.Inventory.Snapshot();
            if (summaryText != null)
                summaryText.text = "Slots " + session.Inventory.UsedSlots + "/" + session.Inventory.SlotCapacity + "     Coins " + session.Coins;

            for (int i = 0; i < rowNames.Length; i++)
            {
                bool filled = i < stacks.Count;
                rowNames[i].text = filled ? ItemCatalog.NameOf(stacks[i].Id) : "- empty -";
                rowCounts[i].text = filled ? "x" + stacks[i].Count + " / " + session.Inventory.StackLimit : string.Empty;
                rowNames[i].color = filled ? new Color(1f, 0.95f, 0.84f) : new Color(1f, 0.95f, 0.84f, 0.35f);
                if (rowIcons != null && i < rowIcons.Length && rowIcons[i] != null)
                {
                    Color tint = filled ? ItemCatalog.Get(stacks[i].Id).Tint : new Color(1f, 1f, 1f, 0.12f);
                    rowIcons[i].color = tint;
                }
            }
        }
    }
}
