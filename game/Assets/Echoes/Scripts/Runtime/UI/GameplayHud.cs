using System;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>
    /// Gameplay readouts around the existing HUD: coins, the hen's status, collection toasts, story banners, and the quest text
    /// that takes over the objective panel once the opening exploration objective is finished.
    /// </summary>
    public class GameplayHud : MonoBehaviour
    {
        [SerializeField] HudController hud;
        [SerializeField] Text coinText;
        [SerializeField] Text chickenText;
        [SerializeField] Text[] toastLines;
        [SerializeField] float toastSeconds = 3.5f;

        public const string QuestHeading = "Restore the Stream";

        string[] toastTexts;
        float[] toastAges;
        GameSession subscribed;

        public string CoinText => coinText != null ? coinText.text : string.Empty;
        public string ChickenText => chickenText != null ? chickenText.text : string.Empty;
        public string LatestToast => toastTexts != null && toastTexts.Length > 0 ? toastTexts[0] : null;

        void Awake()
        {
            int lines = toastLines != null ? toastLines.Length : 0;
            toastTexts = new string[lines];
            toastAges = new float[lines];
            for (int i = 0; i < lines; i++) toastAges[i] = float.MaxValue;
        }

        void Start()
        {
            GameSessionHost host = GameSessionHost.Instance;
            if (host != null && !string.IsNullOrEmpty(host.StartupNotice)) PushToast(host.StartupNotice);
        }

        void OnDestroy() => Unsubscribe();

        void Update()
        {
            GameSession session = GameSession.Current;
            if (session != subscribed)
            {
                Unsubscribe();
                subscribed = session;
                if (subscribed != null)
                {
                    subscribed.Message += PushToast;
                    subscribed.Milestone += OnMilestone;
                }
            }

            if (session != null)
            {
                if (coinText != null) coinText.text = "Coins  " + session.Coins;
                if (chickenText != null) chickenText.text = GameSession.ChickenName + ": " + ChickenStatus(session);
                if (session.ExplorationDone && hud != null) hud.SetObjective(QuestHeading, QuestBody(session));
            }
            FadeToasts();
        }

        public static string ChickenStatus(GameSession session)
        {
            switch (session.Chicken)
            {
                case ChickenPhase.Producing: return "laying an egg (" + Mathf.CeilToInt(session.ChickenRemaining) + " s)";
                case ChickenPhase.EggReady: return "egg ready!";
                default: return "hungry";
            }
        }

        /// <summary>The quest line shown in the objective panel, derived from real session state.</summary>
        public static string QuestBody(GameSession session)
        {
            switch (session.StreamQuest)
            {
                case StreamQuestStage.NotStarted:
                    return "Speak to Mira about the dry stream.";
                case StreamQuestStage.Active:
                    int herbs = session.Inventory.Count(ItemIds.Herb);
                    int flowers = session.Inventory.Count(ItemIds.Flower);
                    if (session.Inventory.Has(ItemIds.SpringTonic, 1)) return "Pour the Spring Tonic into the ruined spring.";
                    if (herbs >= 2 && flowers >= 1) return "Mix a Spring Tonic at the alchemy workshop.";
                    return "Gather Moonleaf Herb " + Math.Min(herbs, 2) + "/2 and Dawn Blossom " + Math.Min(flowers, 1) + "/1.";
                default:
                    return "Complete - the stream flows again. Look after Clover while the valley wakes.";
            }
        }

        void OnMilestone(string key)
        {
            if (hud == null) return;
            if (key == "quest_started")
                hud.ShowBanner("New Quest", QuestHeading, 6f);
            else if (key == "quest_completed")
                hud.ShowBanner("The Stream Flows Again", "Clear water and warm light spread through the valley.", 9f);
        }

        void Unsubscribe()
        {
            if (subscribed == null) return;
            subscribed.Message -= PushToast;
            subscribed.Milestone -= OnMilestone;
            subscribed = null;
        }

        void PushToast(string text)
        {
            if (toastTexts == null || toastTexts.Length == 0) return;
            for (int i = toastTexts.Length - 1; i > 0; i--)
            {
                toastTexts[i] = toastTexts[i - 1];
                toastAges[i] = toastAges[i - 1];
            }
            toastTexts[0] = text;
            toastAges[0] = 0f;
        }

        void FadeToasts()
        {
            if (toastLines == null) return;
            for (int i = 0; i < toastLines.Length; i++)
            {
                toastAges[i] += Time.unscaledDeltaTime;
                Text line = toastLines[i];
                if (line == null) continue;

                float remaining = toastSeconds - toastAges[i];
                line.text = remaining > 0f ? toastTexts[i] : string.Empty;
                Color color = line.color;
                color.a = Mathf.Clamp01(remaining / 0.6f);
                line.color = color;
            }
        }
    }
}
