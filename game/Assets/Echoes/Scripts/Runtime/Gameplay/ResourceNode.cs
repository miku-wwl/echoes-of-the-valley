using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// A renewable gathering spot (herb bush, blossom patch, ore outcrop). The GameSession owns its state; this component
    /// registers it, shows ready/regrowing visuals and forwards E to GameSession.TryGather. Nodes always regrow, so no
    /// required ingredient can run out.
    /// </summary>
    public class ResourceNode : Interactable
    {
        [SerializeField] string nodeId;
        [SerializeField] string itemId = ItemIds.Herb;
        [SerializeField] int yieldAmount = 1;
        [SerializeField] float cooldownSeconds = 40f;
        [SerializeField] GameObject readyVisual;
        [SerializeField] GameObject depletedVisual;

        bool? shownReady;

        public string NodeId => nodeId;
        public string ItemId => itemId;
        public int YieldAmount => yieldAmount;
        public float CooldownSeconds => cooldownSeconds;
        public bool HasVisuals => readyVisual != null && depletedVisual != null;
        public bool ReadyVisualShown => readyVisual != null && readyVisual.activeSelf;

        public bool IsReady
        {
            get
            {
                GameSession session = GameSession.Current;
                return session != null && session.IsNodeReady(nodeId);
            }
        }

        public override string Prompt
        {
            get
            {
                string itemName = ItemCatalog.NameOf(itemId);
                GameSession session = GameSession.Current;
                if (session == null || session.IsNodeReady(nodeId)) return "Gather " + itemName;
                return itemName + " (regrowing, " + Mathf.CeilToInt(session.NodeRemaining(nodeId)) + " s)";
            }
        }

        void Start()
        {
            GameSession.Current?.RegisterNode(nodeId, itemId, yieldAmount, cooldownSeconds);
            RefreshVisuals(true);
        }

        void Update() => RefreshVisuals(false);

        public override void Interact(InteractionDetector interactor)
        {
            GameSession session = GameSession.Current;
            if (session == null) return;

            Outcome outcome = session.TryGather(nodeId);
            if (outcome == Outcome.Ok) return;   // the session already announced what was picked up

            string itemName = ItemCatalog.NameOf(itemId);
            if (outcome == Outcome.NotReady) session.Notify(itemName + " is still regrowing.");
            else if (outcome == Outcome.NoRoom) session.Notify("Your satchel cannot hold any more " + itemName + ".");
            else session.Notify(OutcomeText.Describe(outcome));
        }

        void RefreshVisuals(bool force)
        {
            bool ready = IsReady;
            if (!force && shownReady == ready) return;
            shownReady = ready;
            if (readyVisual != null) readyVisual.SetActive(ready);
            if (depletedVisual != null) depletedVisual.SetActive(!ready);
        }
    }
}
