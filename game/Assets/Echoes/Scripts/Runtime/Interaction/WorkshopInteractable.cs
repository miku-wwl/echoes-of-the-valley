using UnityEngine;

namespace Echoes
{
    /// <summary>The alchemy workshop. Interacting opens the crafting panel; the dialogue is only a fallback if the panel is not wired.</summary>
    public class WorkshopInteractable : DialogueInteractable
    {
        [SerializeField] CraftingPanel craftingPanel;

        [SerializeField, TextArea] string[] fallbackLines =
        {
            "A cosy workshop crowded with cauldrons, jars and bundles of drying herbs.",
            "The alchemy table is not set up yet."
        };

        public CraftingPanel Panel => craftingPanel;

        public override void Interact(InteractionDetector interactor)
        {
            if (craftingPanel != null) craftingPanel.Open();
            else Dialogue.Open(Speaker, fallbackLines);
        }
    }
}
