using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Clover the hen. The GameSession owns the hungry -> producing -> egg-ready cycle; this component forwards E to the
    /// matching transaction and shows the state: a "hungry" bubble while she waits for feed, a spinning star while she lays,
    /// and an egg in the nest when it is ready.
    /// </summary>
    public class ChickenInteractable : Interactable
    {
        [SerializeField] Transform body;
        [SerializeField] GameObject hungryIcon;
        [SerializeField] GameObject producingIcon;
        [SerializeField] GameObject eggVisual;

        Vector3 bodyBasePosition;
        bool baseCaptured;

        public ChickenPhase ShownPhase { get; private set; }
        public bool HungryIconShown => hungryIcon != null && hungryIcon.activeSelf;
        public bool ProducingIconShown => producingIcon != null && producingIcon.activeSelf;
        public bool EggShown => eggVisual != null && eggVisual.activeSelf;

        public override string Prompt
        {
            get
            {
                GameSession session = GameSession.Current;
                if (session == null) return base.Prompt;
                switch (session.Chicken)
                {
                    case ChickenPhase.Producing:
                        return GameSession.ChickenName + " is laying an egg (" + Mathf.CeilToInt(session.ChickenRemaining) + " s)";
                    case ChickenPhase.EggReady:
                        return "Collect " + GameSession.ChickenName + "'s egg";
                    default:
                        return session.Inventory.Has(ItemIds.ChickenFeed, 1)
                            ? "Feed " + GameSession.ChickenName
                            : GameSession.ChickenName + " is hungry (needs Chicken Feed)";
                }
            }
        }

        void Awake()
        {
            if (body != null)
            {
                bodyBasePosition = body.localPosition;
                baseCaptured = true;
            }
        }

        void Update()
        {
            GameSession session = GameSession.Current;
            ChickenPhase phase = session != null ? session.Chicken : ChickenPhase.Hungry;
            ShownPhase = phase;

            if (hungryIcon != null) hungryIcon.SetActive(phase == ChickenPhase.Hungry);
            if (producingIcon != null) producingIcon.SetActive(phase == ChickenPhase.Producing);
            if (eggVisual != null) eggVisual.SetActive(phase == ChickenPhase.EggReady);

            if (producingIcon != null && producingIcon.activeSelf) producingIcon.transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            if (body == null || !baseCaptured) return;

            float t = Time.time;
            switch (phase)
            {
                case ChickenPhase.Hungry:   // pecking at the ground
                    body.localPosition = bodyBasePosition;
                    body.localRotation = Quaternion.Euler(Mathf.Abs(Mathf.Sin(t * 3f)) * 24f, 0f, 0f);
                    break;
                case ChickenPhase.Producing:   // settled on the nest, wiggling
                    body.localPosition = bodyBasePosition + Vector3.down * 0.08f;
                    body.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 9f) * 3f);
                    break;
                default:   // standing proudly, little hops
                    body.localPosition = bodyBasePosition + Vector3.up * Mathf.Max(0f, Mathf.Sin(t * 5f)) * 0.06f;
                    body.localRotation = Quaternion.identity;
                    break;
            }
        }

        public override void Interact(InteractionDetector interactor)
        {
            GameSession session = GameSession.Current;
            if (session == null) return;

            switch (session.Chicken)
            {
                case ChickenPhase.Hungry:
                    Outcome fed = session.TryFeedChicken();
                    if (fed == Outcome.Insufficient) session.Notify(GameSession.ChickenName + " is hungry. Mix Chicken Feed at the workshop.");
                    else if (fed != Outcome.Ok) session.Notify(OutcomeText.Describe(fed));
                    break;
                case ChickenPhase.Producing:
                    session.Notify(GameSession.ChickenName + " is busy laying an egg.");
                    break;
                default:
                    Outcome collected = session.TryCollectEgg();
                    if (collected == Outcome.NoRoom) session.Notify("Your satchel has no room for the egg.");
                    else if (collected != Outcome.Ok) session.Notify(OutcomeText.Describe(collected));
                    break;
            }
        }
    }
}
