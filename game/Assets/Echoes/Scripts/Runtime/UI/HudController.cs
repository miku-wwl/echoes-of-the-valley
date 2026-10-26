using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>In-game HUD: objective panel, interaction prompt and a fading banner. Controls hint is static text in the scene.</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] Text objectiveHeading;
        [SerializeField] Text objectiveBody;
        [SerializeField] GameObject promptRoot;
        [SerializeField] Text promptText;
        [SerializeField] CanvasGroup bannerGroup;
        [SerializeField] Text bannerTitle;
        [SerializeField] Text bannerSubtitle;

        float bannerAge;
        float bannerLength;

        public string ObjectiveHeading => objectiveHeading != null ? objectiveHeading.text : string.Empty;
        public string ObjectiveBody => objectiveBody != null ? objectiveBody.text : string.Empty;
        public bool IsPromptVisible => promptRoot != null && promptRoot.activeSelf;
        public string PromptText => promptText != null ? promptText.text : string.Empty;
        public bool IsBannerVisible => bannerGroup != null && bannerGroup.alpha > 0.01f;
        public string BannerTitle => bannerTitle != null ? bannerTitle.text : string.Empty;

        void Awake()
        {
            if (promptRoot != null) promptRoot.SetActive(false);
            if (bannerGroup != null) bannerGroup.alpha = 0f;
        }

        public void SetObjective(string heading, string body)
        {
            if (objectiveHeading != null) objectiveHeading.text = heading;
            if (objectiveBody != null) objectiveBody.text = body;
        }

        /// <summary>Shows "[E]  prompt", or hides the prompt when null or empty.</summary>
        public void SetPrompt(string prompt)
        {
            if (promptRoot == null) return;
            bool show = !string.IsNullOrEmpty(prompt);
            if (show && promptText != null) promptText.text = "[E]  " + prompt;
            if (promptRoot.activeSelf != show) promptRoot.SetActive(show);
        }

        public void ShowBanner(string title, string subtitle, float seconds)
        {
            if (bannerGroup == null) return;
            bannerTitle.text = title;
            bannerSubtitle.text = subtitle;
            bannerAge = 0f;
            bannerLength = Mathf.Max(1f, seconds);
            bannerGroup.alpha = 0.02f;
        }

        /// <summary>Clears a banner that is still showing (the ending screen needs the whole stage to itself).</summary>
        public void HideBanner()
        {
            bannerLength = 0f;
            bannerAge = 0f;
            if (bannerGroup != null) bannerGroup.alpha = 0f;
        }

        void Update()
        {
            if (bannerGroup == null || bannerLength <= 0f) return;

            bannerAge += Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01(bannerAge / 0.35f);
            float fadeOut = Mathf.Clamp01((bannerLength - bannerAge) / 0.8f);
            bannerGroup.alpha = Mathf.Min(fadeIn, fadeOut);

            if (bannerAge >= bannerLength)
            {
                bannerLength = 0f;
                bannerGroup.alpha = 0f;
            }
        }
    }
}
