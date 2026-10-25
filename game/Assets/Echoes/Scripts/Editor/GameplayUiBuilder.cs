using UnityEngine;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    internal struct GameplayUi
    {
        public GameplayHud Hud;
        public InventoryPanel Inventory;
        public CraftingPanel Crafting;
    }

    /// <summary>Builds the Phase 2 UI on the existing HUD canvas: coins and hen status, toasts, the satchel and the alchemy panel.</summary>
    internal static class GameplayUiBuilder
    {
        static readonly Color ModalColor = new Color(0.08f, 0.11f, 0.18f, 0.95f);

        public static GameplayUi Build(Canvas canvas, HudController hud)
        {
            Transform root = canvas.transform;
            GameplayUi ui = new GameplayUi { Hud = canvas.gameObject.AddComponent<GameplayHud>() };

            // Coins and the hen (top-right)
            Image status = UiKit.Panel(root, "StatusPanel", UiKit.PanelColor);
            status.rectTransform.Place(UiKit.TopRight, UiKit.TopRight, new Vector2(-32f, -32f), new Vector2(430f, 112f));
            Text coins = UiKit.Label(status.transform, "Coins", "Coins  20", 30, UiKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            coins.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(24f, -14f), new Vector2(380f, 40f));
            Text hen = UiKit.Label(status.transform, "Hen", GameSession.ChickenName + ": hungry", 26, UiKit.Cream, TextAnchor.UpperLeft);
            hen.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(24f, -60f), new Vector2(380f, 40f));

            // Collection and crafting feedback (right edge); toast 0 is the newest and sits lowest
            Text[] toasts = new Text[4];
            for (int i = 0; i < toasts.Length; i++)
            {
                toasts[i] = UiKit.Label(root, "Toast" + i, string.Empty, 30, UiKit.Cream, TextAnchor.MiddleRight, FontStyle.Bold);
                toasts[i].rectTransform.Place(UiKit.MiddleRight, UiKit.MiddleRight, new Vector2(-32f, -70f + i * 46f), new Vector2(560f, 42f));
            }

            Wire.Set(ui.Hud, "hud", hud);
            Wire.Set(ui.Hud, "coinText", coins);
            Wire.Set(ui.Hud, "chickenText", hen);
            Wire.Set(ui.Hud, "toastLines", toasts);

            ui.Inventory = BuildInventory(canvas);
            ui.Crafting = BuildCrafting(canvas);
            return ui;
        }

        static InventoryPanel BuildInventory(Canvas canvas)
        {
            InventoryPanel controller = canvas.gameObject.AddComponent<InventoryPanel>();
            Image panel = UiKit.Panel(canvas.transform, "InventoryPanel", ModalColor);
            panel.rectTransform.Place(UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(780f, 640f));
            panel.raycastTarget = true;

            Text title = UiKit.Label(panel.transform, "Title", "Satchel", 52, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            title.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -26f), new Vector2(700f, 70f));
            Text summary = UiKit.Label(panel.transform, "Summary", "Slots 0/8     Coins 0", 30, UiKit.Cream, TextAnchor.UpperCenter);
            summary.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -100f), new Vector2(700f, 44f));

            int rows = Inventory.DefaultSlots;
            Text[] names = new Text[rows];
            Text[] counts = new Text[rows];
            Image[] icons = new Image[rows];
            for (int i = 0; i < rows; i++)
            {
                float y = -160f - i * 54f;
                icons[i] = UiKit.Panel(panel.transform, "Icon" + i, Color.white, false);
                icons[i].rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(60f, y - 4f), new Vector2(36f, 36f));
                names[i] = UiKit.Label(panel.transform, "Name" + i, string.Empty, 30, UiKit.Cream, TextAnchor.MiddleLeft);
                names[i].rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(116f, y), new Vector2(420f, 44f));
                counts[i] = UiKit.Label(panel.transform, "Count" + i, string.Empty, 28, UiKit.Gold, TextAnchor.MiddleRight);
                counts[i].rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(540f, y), new Vector2(190f, 44f));
            }

            Text hint = UiKit.Label(panel.transform, "Hint", "I / Esc  Close", 24, UiKit.Gold, TextAnchor.LowerCenter);
            hint.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 16f), new Vector2(400f, 34f));

            Wire.Set(controller, "panel", panel.gameObject);
            Wire.Set(controller, "summaryText", summary);
            Wire.Set(controller, "rowNames", names);
            Wire.Set(controller, "rowCounts", counts);
            Wire.Set(controller, "rowIcons", icons);
            panel.gameObject.SetActive(false);
            return controller;
        }

        static CraftingPanel BuildCrafting(Canvas canvas)
        {
            CraftingPanel controller = canvas.gameObject.AddComponent<CraftingPanel>();
            Image panel = UiKit.Panel(canvas.transform, "CraftingPanel", ModalColor);
            panel.rectTransform.Place(UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(1100f, 720f));
            panel.raycastTarget = true;

            Text title = UiKit.Label(panel.transform, "Title", "Alchemy Workshop", 54, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            title.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -22f), new Vector2(900f, 70f));
            Text subtitle = UiKit.Label(panel.transform, "Subtitle", "Mix what you gather. Press 1-3 or click Craft. Esc closes.", 26, UiKit.Cream, TextAnchor.UpperCenter);
            subtitle.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -88f), new Vector2(900f, 40f));

            int rows = RecipeBook.All.Count;
            Text[] names = new Text[rows];
            Text[] details = new Text[rows];
            Button[] buttons = new Button[rows];
            for (int i = 0; i < rows; i++)
            {
                float y = -138f - i * 158f;
                names[i] = UiKit.Label(panel.transform, "RecipeName" + i, string.Empty, 36, UiKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
                names[i].rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(40f, y), new Vector2(760f, 46f));
                details[i] = UiKit.Label(panel.transform, "RecipeDetail" + i, string.Empty, 26, UiKit.Cream, TextAnchor.UpperLeft);
                details[i].rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(40f, y - 52f), new Vector2(790f, 90f));
                buttons[i] = UiKit.MakeButton(panel.transform, "CraftButton" + i, "Craft", UiKit.TopLeft, new Vector2(950f, y - 60f), new Vector2(200f, 70f));
            }

            Text status = UiKit.Label(panel.transform, "Status", "Choose a recipe.", 30, UiKit.Cream, TextAnchor.MiddleLeft);
            status.rectTransform.Place(UiKit.BottomLeft, UiKit.BottomLeft, new Vector2(40f, 40f), new Vector2(800f, 48f));
            Button close = UiKit.MakeButton(panel.transform, "CloseButton", "Close", UiKit.BottomRight, new Vector2(-130f, 64f), new Vector2(200f, 64f));

            Wire.Set(controller, "panel", panel.gameObject);
            Wire.Set(controller, "nameTexts", names);
            Wire.Set(controller, "detailTexts", details);
            Wire.Set(controller, "craftButtons", buttons);
            Wire.Set(controller, "closeButton", close);
            Wire.Set(controller, "statusText", status);
            panel.gameObject.SetActive(false);
            return controller;
        }
    }
}
