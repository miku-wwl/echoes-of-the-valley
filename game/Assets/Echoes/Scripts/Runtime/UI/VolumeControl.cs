using UnityEngine;
using UnityEngine.UI;

namespace Echoes
{
    /// <summary>Two sliders (music and sound) bound to GameSettings. Dragging changes the volume at once; the file is written once the player stops.</summary>
    public class VolumeControl : MonoBehaviour
    {
        [SerializeField] Slider music;
        [SerializeField] Slider sound;

        float saveAt = -1f;
        bool wired;

        public Slider MusicSlider => music;
        public Slider SoundSlider => sound;

        void Awake() => Wire();

        void OnEnable()
        {
            Wire();
            Refresh();
        }

        void OnDisable() => Flush();

        void OnDestroy() => Flush();

        void Update()
        {
            if (saveAt > 0f && Time.unscaledTime >= saveAt) Flush();
        }

        /// <summary>Shows the stored volumes without treating that as a change.</summary>
        public void Refresh()
        {
            if (music != null) music.SetValueWithoutNotify(GameSettings.MusicVolume);
            if (sound != null) sound.SetValueWithoutNotify(GameSettings.SoundVolume);
        }

        void Wire()
        {
            if (wired) return;
            wired = true;
            if (music != null) music.onValueChanged.AddListener(value => Changed(() => GameSettings.MusicVolume = value));
            if (sound != null) sound.onValueChanged.AddListener(value => Changed(() => GameSettings.SoundVolume = value));
        }

        void Changed(System.Action apply)
        {
            apply();
            saveAt = Time.unscaledTime + 0.6f;
        }

        void Flush()
        {
            if (saveAt < 0f) return;
            saveAt = -1f;
            GameSettings.Save();
        }
    }
}
