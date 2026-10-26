using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// The valley's soundscape. Looping layers (music stems and ambience beds) all play at once and the director only moves
    /// their volumes: every layer has a volume per ResonanceStage, so the silent valley is a few sparse bells and wind, the
    /// restored stream adds water and a harp, the garden adds birds, chimes and a marimba, and the golden hour adds a warm pad
    /// and the melody. A stage reached live fades in over a few seconds; a loaded stage is heard at once. One-shot cues come
    /// from GameSession (so a sound means something really changed) and from the interface. The ending plays its own piece
    /// over a ducked bed. Every sound is original and was synthesised by code (see AudioLibrary).
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public string name;
            public AudioClip clip;
            public AudioSource source;
            public bool music = true;                           // music bus; otherwise the ambience bus
            public float[] stageVolume = new float[4];          // the volume at ResonanceStage 0, 1, 2 and 3
        }

        [Serializable]
        public sealed class CueSound
        {
            public SoundCue cue;
            public AudioClip[] clips = new AudioClip[0];
            public float volume = 1f;
        }

        [SerializeField] Layer[] layers = new Layer[0];
        [SerializeField] CueSound[] cueSounds = new CueSound[0];
        [SerializeField] AudioSource effectSource;
        [SerializeField] AudioSource uiSource;
        [SerializeField] AudioSource stepSource;
        [SerializeField] AudioSource endingSource;
        [SerializeField] AudioClip endingClip;
        [SerializeField] bool followStage = true;               // false: the title screen plays stage 0's volumes whatever happens
        [SerializeField] float fadeSeconds = 4f;

        readonly Dictionary<SoundCue, CueSound> sounds = new Dictionary<SoundCue, CueSound>();
        readonly Dictionary<SoundCue, int> counts = new Dictionary<SoundCue, int>();
        float[] volumes = new float[0];
        bool[] started = new bool[0];
        GameSession subscribed;
        bool snapped;
        float endingUntil;

        public static AudioDirector Instance { get; private set; }

        public ResonanceStage ShownStage { get; private set; }
        public SoundCue LastCue { get; private set; }
        public int TotalCues { get; private set; }
        public int LayerCount => layers.Length;
        public bool FollowsStage => followStage;
        public bool EndingMusicPlaying => Time.unscaledTime < endingUntil;
        public AudioClip EndingClip => endingClip;
        public int CueCount(SoundCue cue) => counts.TryGetValue(cue, out int n) ? n : 0;
        public bool HasSound(SoundCue cue) => sounds.TryGetValue(cue, out CueSound sound) && sound.clips != null && sound.clips.Length > 0 && sound.clips[0] != null;

        public string LayerName(int index) => layers[index].name;
        public AudioClip LayerClip(int index) => layers[index].clip;
        public AudioSource LayerSource(int index) => layers[index].source;

        /// <summary>The volume a layer is fading towards right now (stage volume x bus x settings x ducking).</summary>
        public float LayerTarget(string layerName)
        {
            int i = IndexOf(layerName);
            return i < 0 ? 0f : TargetFor(layers[i], ShownStage);
        }

        /// <summary>The volume a layer actually has right now.</summary>
        public float LayerVolume(string layerName)
        {
            int i = IndexOf(layerName);
            return i < 0 || i >= volumes.Length ? 0f : volumes[i];
        }

        /// <summary>True once the layer was started (the director's intent: a machine with no sound device still counts).</summary>
        public bool LayerStarted(string layerName)
        {
            int i = IndexOf(layerName);
            return i >= 0 && i < started.Length && started[i];
        }

        /// <summary>The layer's configured volume for a stage, before settings and ducking.</summary>
        public float StageVolume(string layerName, ResonanceStage stage)
        {
            int i = IndexOf(layerName);
            if (i < 0) return 0f;
            float[] table = layers[i].stageVolume;
            return table == null || table.Length == 0 ? 0f : table[Mathf.Clamp((int)stage, 0, table.Length - 1)];
        }

        /// <summary>Plays a cue from anywhere (interface code) if there is a director in the scene.</summary>
        public static void PlayStatic(SoundCue cue)
        {
            if (Instance != null) Instance.Play(cue);
        }

        void Awake()
        {
            Instance = this;
            foreach (CueSound sound in cueSounds) sounds[sound.cue] = sound;

            volumes = new float[layers.Length];
            started = new bool[layers.Length];
            foreach (Layer layer in layers)
            {
                if (layer.source == null) continue;
                layer.source.clip = layer.clip;
                layer.source.loop = true;
                layer.source.playOnAwake = false;
                layer.source.spatialBlend = 0f;
                layer.source.volume = 0f;
            }
        }

        void Start()
        {
            // Every layer is scheduled for the same instant, so the stems stay in step for as long as the game runs.
            double startAt = AudioSettings.dspTime + 0.2;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].source == null || layers[i].clip == null) continue;
                layers[i].source.PlayScheduled(startAt);
                started[i] = true;
            }
        }

        void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            BindSession();

            GameSession session = GameSession.Current;
            ShownStage = followStage && session != null ? session.Resonance : ResonanceStage.SilentValley;

            float dt = Time.unscaledDeltaTime;
            float blend = 1f - Mathf.Exp(-dt * 3f / Mathf.Max(0.2f, fadeSeconds));
            for (int i = 0; i < layers.Length; i++)
            {
                float target = TargetFor(layers[i], ShownStage);
                volumes[i] = !snapped ? target : Mathf.Abs(volumes[i] - target) < 0.002f ? target : Mathf.Lerp(volumes[i], target, blend);
                if (layers[i].source != null) layers[i].source.volume = volumes[i];
            }
            snapped = true;

            if (endingSource != null && EndingMusicPlaying) endingSource.volume = GameSettings.MusicVolume * 0.95f;
        }

        float TargetFor(Layer layer, ResonanceStage stage)
        {
            float[] table = layer.stageVolume;
            float stageVolume = table == null || table.Length == 0 ? 0f : table[Mathf.Clamp((int)stage, 0, table.Length - 1)];
            float bus = layer.music ? GameSettings.MusicVolume : GameSettings.SoundVolume;
            float duck = GameFlow.IsPaused ? 0.45f : 1f;
            if (EndingMusicPlaying) duck *= layer.music ? 0.15f : 0.6f;   // the ending piece has the stage to itself
            return stageVolume * bus * duck;
        }

        int IndexOf(string layerName)
        {
            for (int i = 0; i < layers.Length; i++)
                if (layers[i].name == layerName) return i;
            return -1;
        }

        void BindSession()
        {
            GameSession session = GameSession.Current;
            if (session == subscribed) return;
            Unsubscribe();
            subscribed = session;
            if (subscribed != null) subscribed.Cue += Play;
        }

        void Unsubscribe()
        {
            if (subscribed == null) return;
            subscribed.Cue -= Play;
            subscribed = null;
        }

        /// <summary>Counts the cue and plays one of its clips (the footsteps are varied in pitch).</summary>
        public void Play(SoundCue cue)
        {
            if (cue == SoundCue.None) return;
            counts[cue] = CueCount(cue) + 1;
            LastCue = cue;
            TotalCues++;

            if (cue == SoundCue.EndingBegins && endingSource != null && endingClip != null)
            {
                endingSource.clip = endingClip;
                endingSource.volume = GameSettings.MusicVolume * 0.95f;
                endingSource.Play();
                endingUntil = Time.unscaledTime + endingClip.length;
            }

            if (!sounds.TryGetValue(cue, out CueSound sound) || sound.clips == null || sound.clips.Length == 0) return;
            AudioClip clip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
            if (clip == null) return;

            float volume = sound.volume * GameSettings.SoundVolume;
            switch (cue)
            {
                case SoundCue.Footstep:
                    if (stepSource == null) return;
                    stepSource.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                    stepSource.PlayOneShot(clip, volume);
                    break;
                case SoundCue.UiClick:
                case SoundCue.UiHover:
                case SoundCue.DialogueTick:
                case SoundCue.PanelOpen:
                case SoundCue.PanelClose:
                    if (uiSource != null) uiSource.PlayOneShot(clip, volume);
                    break;
                default:
                    if (effectSource != null) effectSource.PlayOneShot(clip, volume);
                    break;
            }
        }
    }
}
