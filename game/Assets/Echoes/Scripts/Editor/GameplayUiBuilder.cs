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
        static readonly Color ModalColor = UiKit.ModalColor;

        public static GameplayUi Build(Canvas canvas, HudController hud)
        {
            Transform root = canvas.transform;
            GameplayUi ui = new GameplayUi { Hud = canvas.gameObject.AddComponent<GameplayHud>() };

            // Coins and the hen (top-right), each with a small round marker
            Image status = UiKit.Panel(root, "StatusPanel", UiKit.PanelColor, true, true);
            status.rectTransform.Place(UiKit.TopRight, UiKit.TopRight, new Vector2(-32f, -32f), new Vector2(430f, 112f));
            Image coinMark = UiKit.Dot(status.transform, "CoinMark", UiKit.Gold);
            coinMark.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(24f, -20f), new Vector2(26f, 26f));
            Text coins = UiKit.Label(status.transform, "Coins", "Coins  20", 30, UiKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            coins.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(62f, -14f), new Vector2(350f, 40f));
            Image henMark = UiKit.Dot(status.transform, "HenMark", new Color(1f, 0.97f, 0.9f, 1f));
            henMark.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(24f, -66f), new Vector2(26f, 26f));
            Text hen = UiKit.Label(status.transform, "Hen", GameSession.ChickenName + ": hungry", 26, UiKit.Cream, TextAnchor.UpperLeft);
            hen.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(62f, -60f), new Vector2(350f, 40f));

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

        /// <summary>
        /// The closing screen: a warm veil over the live valley with the title, the line the game has been walking toward,
        /// the four words, short credits, the two choices and their keys. Added last so it draws above everything else.
        /// </summary>
        public static EndingPanel BuildEnding(Canvas canvas, ThirdPersonCamera orbitCamera)
        {
            EndingPanel controller = canvas.gameObject.AddComponent<EndingPanel>();
            Image veil = UiKit.Panel(canvas.transform, "EndingPanel", new Color(0.12f, 0.08f, 0.04f, 0.34f), false);
            veil.rectTransform.Fill();
            veil.raycastTarget = true;

            Image card = UiKit.Panel(veil.transform, "Card", new Color(0.11f, 0.09f, 0.19f, 0.88f), true, true);
            // 800 high and nudged down, so it clears the objective panel (ends 164 px from the top) and the controls hint
            card.rectTransform.Place(UiKit.Center, UiKit.Center, new Vector2(0f, -28f), new Vector2(1360f, 800f));

            Text title = UiKit.Label(card.transform, "Title", "Echoes of the Valley", 88, UiKit.Cream, TextAnchor.UpperCenter, FontStyle.Bold);
            title.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -26f), new Vector2(1280f, 112f));
            Text headline = UiKit.Label(card.transform, "Headline", "The valley has found its voice again.", 50, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            headline.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -142f), new Vector2(1280f, 68f));
            Text story = UiKit.Label(card.transform, "Story",
                "Silence became a stream. The stream woke the garden. The garden lifted the windmill's sails, and now the whole valley hums, with you inside the song.",
                34, UiKit.Cream, TextAnchor.UpperCenter);
            story.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -222f), new Vector2(1140f, 138f));
            Text stages = UiKit.Label(card.transform, "Stages", "Silence   ·   Awakening   ·   Resonance   ·   Rest", 42, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            stages.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -380f), new Vector2(1280f, 60f));
            Text credits = UiKit.Label(card.transform, "Credits", string.Join("\n", GameCredits.EndingLines),
                26, new Color(1f, 0.95f, 0.84f, 0.92f), TextAnchor.UpperCenter);
            credits.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -456f), new Vector2(1280f, 120f));

            Button continueButton = UiKit.MakeButton(card.transform, "ContinueButton", "Continue Exploring", UiKit.BottomCenter, new Vector2(-250f, 140f), new Vector2(440f, 88f));
            Button menuButton = UiKit.MakeButton(card.transform, "MainMenuButton", "Main Menu", UiKit.BottomCenter, new Vector2(250f, 140f), new Vector2(360f, 88f));
            Text controls = UiKit.Label(card.transform, "Controls", "Enter / E / Esc  Continue exploring        M  Main menu", 26, UiKit.Gold, TextAnchor.LowerCenter);
            controls.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 40f), new Vector2(1100f, 40f));

            Wire.Set(controller, "panel", veil.gameObject);
            Wire.Set(controller, "continueButton", continueButton);
            Wire.Set(controller, "mainMenuButton", menuButton);
            Wire.Set(controller, "orbitCamera", orbitCamera);
            veil.gameObject.SetActive(false);
            return controller;
        }

        static InventoryPanel BuildInventory(Canvas canvas)
        {
            InventoryPanel controller = canvas.gameObject.AddComponent<InventoryPanel>();
            Image panel = UiKit.Panel(canvas.transform, "InventoryPanel", ModalColor, true, true);
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
            Image panel = UiKit.Panel(canvas.transform, "CraftingPanel", ModalColor, true, true);
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
