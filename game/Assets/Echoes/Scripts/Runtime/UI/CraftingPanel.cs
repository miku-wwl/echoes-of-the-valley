using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// The alchemy workshop screen. One row per recipe shows its ingredients against the real satchel contents; a row's
    /// Craft button (or the 1-3 keys) calls GameSession.TryCraft, so crafting always validates ingredients and room.
    /// </summary>
    public class CraftingPanel : MonoBehaviour
    {
        const string Have = "7fe39a";
        const string Missing = "ff8f8f";

        [SerializeField] GameObject panel;
        [SerializeField] Text[] nameTexts;
        [SerializeField] Text[] detailTexts;
        [SerializeField] Button[] craftButtons;
        [SerializeField] Button closeButton;
        [SerializeField] Text statusText;

        public bool IsOpen { get; private set; }
        public int RecipeRows => nameTexts != null ? nameTexts.Length : 0;
        public GameObject Panel => panel;
        public Button CloseButton => closeButton;
        public Button CraftButton(int index) => craftButtons[index];
        public string StatusMessage => statusText != null ? statusText.text : string.Empty;
        public string DetailText(int index) => detailTexts[index].text;
        public string NameText(int index) => nameTexts[index].text;

        void Awake()
        {
            if (panel != null) panel.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            for (int i = 0; i < craftButtons.Length; i++)
            {
                int index = i;
                craftButtons[i].onClick.AddListener(() => Craft(index));
            }
        }

        void Update()
        {
            if (!IsOpen) return;

            if (GameFlow.TryConsumePause())
            {
                Close();
                return;
            }
            for (int i = 0; i < RecipeRows; i++)
                if (GameInput.DigitPressed(i + 1)) Craft(i);
            Refresh();
        }

        void OnDestroy()
        {
            if (IsOpen) GameFlow.SetPanelOpen(false);
        }

        public void Open()
        {
            if (IsOpen || !GameFlow.GameplayInputAllowed) return;
            IsOpen = true;
            if (panel != null) panel.SetActive(true);
            GameFlow.SetPanelOpen(true);
            AudioDirector.PlayStatic(SoundCue.PanelOpen);
            if (statusText != null) statusText.text = "Choose a recipe.";
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (panel != null) panel.SetActive(false);
            GameFlow.SetPanelOpen(false);
            AudioDirector.PlayStatic(SoundCue.PanelClose);
        }

        /// <summary>Crafts recipe number <paramref name="index"/>. Failures change nothing and say why.</summary>
        public Outcome Craft(int index)
        {
            GameSession session = GameSession.Current;
            if (session == null || index < 0 || index >= RecipeBook.All.Count) return Outcome.UnknownTarget;

            Recipe recipe = RecipeBook.All[index];
            Outcome result = session.TryCraft(recipe.Id);
            if (result != Outcome.Ok) session.RaiseCue(SoundCue.Denied);
            if (statusText != null)
            {
                if (result == Outcome.Ok) statusText.text = "Crafted " + recipe.Name + ".";
                else if (result == Outcome.Insufficient) statusText.text = "You are missing ingredients for " + recipe.Name + ".";
                else if (result == Outcome.NoRoom) statusText.text = "Your satchel has no room for " + recipe.Name + ".";
                else statusText.text = OutcomeText.Describe(result);
            }
            Refresh();
            return result;
        }

        void Refresh()
        {
            GameSession session = GameSession.Current;
            if (session == null) return;

            for (int i = 0; i < RecipeRows && i < RecipeBook.All.Count; i++)
            {
                Recipe recipe = RecipeBook.All[i];
                nameTexts[i].text = "[" + (i + 1) + "]  " + recipe.Name;

                StringBuilder text = new StringBuilder();
                foreach (ItemAmount input in recipe.Inputs)
                {
                    int have = session.Inventory.Count(input.Id);
                    text.Append("<color=#").Append(have >= input.Count ? Have : Missing).Append('>')
                        .Append(ItemCatalog.NameOf(input.Id)).Append(' ').Append(have).Append('/').Append(input.Count)
                        .Append("</color>   ");
                }
                text.Append("\n").Append(recipe.Description).Append("  Makes ")
                    .Append(recipe.Output.Count > 1 ? recipe.Output.Count + " x " : string.Empty).Append(ItemCatalog.NameOf(recipe.Output.Id)).Append('.');
                detailTexts[i].text = text.ToString();

                craftButtons[i].interactable = session.Inventory.Check(recipe.Inputs, new[] { recipe.Output }) == Outcome.Ok;
            }
        }
    }
}
