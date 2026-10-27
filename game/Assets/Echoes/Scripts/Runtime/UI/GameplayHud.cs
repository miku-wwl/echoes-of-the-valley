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
                if (session.ExplorationDone && hud != null)
                {
                    QuestLine line = QuestLog.Current(session);
                    hud.SetObjective(line.Heading, line.Body);
                }
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

        void OnMilestone(string key)
        {
            if (hud == null) return;
            switch (key)
            {
                case "quest_started":
                    hud.ShowBanner("New Quest", QuestLog.StreamHeading, 6f);
                    break;
                case "quest_completed":
                    hud.ShowBanner("The Stream Flows Again", "Clear water and warm light spread through the valley.", 9f);
                    break;
                case "garden_started":
                    hud.ShowBanner("New Quest", QuestLog.GardenHeading, 6f);
                    break;
                case "garden_completed":
                    hud.ShowBanner("The Garden Blooms", "Colour and warmth spread through the village.", 9f);
                    break;
                case "windmill_started":
                    hud.ShowBanner("New Quest", QuestLog.WindmillHeading, 6f);
                    break;
                case "windmill_completed":
                    hud.ShowBanner("The Windmill Turns", "The whole valley hums with life again.", 9f);
                    break;
                case "ending_seen":
                    hud.HideBanner();   // the ending screen opens right after this; an older banner must not show through it
                    break;
            }
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
            // The same message again while it is still showing refreshes it instead of stacking duplicate lines.
            if (toastTexts[0] == text && toastAges[0] < toastSeconds)
            {
                toastAges[0] = 0f;
                return;
            }
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
