using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Draws the valley for the single authoritative ResonanceStage; nothing here is saved or decided separately.
    /// Stage 0: a murky trickle, a dry basin, a withered garden, a broken windmill. Stage 1: a wide clear stream, a full basin
    /// with a fountain, blooms along the banks and a warmer light. Stage 2 adds the blooming garden, a lively hen yard,
    /// drifting pollen and cheerful villagers. Stage 3 adds the mended windmill turning, lanterns and flowers through the
    /// village and the golden hour. Each stage keeps everything the earlier ones showed.
    /// A stage that changes while playing runs a short transition (a ring of light, blooms popping up, water rising,
    /// sails speeding up, light warming); a stage already reached when the scene loads is shown at once, so nothing
    /// replays after loading a save.
    /// </summary>
    public class ResonanceView : MonoBehaviour
    {
        [SerializeField] ValleyLighting lighting;
        [SerializeField] GameObject troubledStream;
        [SerializeField] GameObject clearStream;
        [SerializeField] Renderer pond;
        [SerializeField] Renderer springWater;
        [SerializeField] Material murkyWater;
        [SerializeField] Material clearWater;
        [SerializeField] Vector3 springWaterDry = new Vector3(0.9f, 0.006f, 0.9f);
        [SerializeField] Vector3 springWaterFull = new Vector3(2.5f, 0.02f, 2.5f);
        [SerializeField] GameObject bloomRoot;
        [SerializeField] ParticleSystem pulse;
        [SerializeField] ParticleSystem fountain;
        [SerializeField] float transitionSeconds = 3.5f;

        [Header("Stage 2: the garden")]
        [SerializeField] GameObject gardenDormant;
        [SerializeField] GameObject gardenBloomRoot;
        [SerializeField] GameObject yardLifeRoot;
        [SerializeField] ParticleSystem gardenMotes;
        [SerializeField] ParticleSystem gardenPulse;
        [SerializeField] NpcIdle[] villagers;

        [Header("Stage 3: the windmill")]
        [SerializeField] WindmillRotator windmill;
        [SerializeField] GameObject[] windmillDamaged;
        [SerializeField] GameObject[] windmillRepaired;
        [SerializeField] GameObject villageLifeRoot;
        [SerializeField] ParticleSystem valleyMotes;
        [SerializeField] ParticleSystem finalPulse;

        PopGroup streamBloom;
        PopGroup gardenBloom;
        PopGroup yardLife;
        PopGroup villageLife;
        Coroutine transition;
        bool initialised;

        public ResonanceStage ShownStage { get; private set; } = ResonanceStage.SilentValley;
        public bool IsTransitioning { get; private set; }
        public bool ClearStreamShown => clearStream != null && clearStream.activeSelf;
        public bool TroubledStreamShown => troubledStream != null && troubledStream.activeSelf;
        public bool BloomShown => bloomRoot != null && bloomRoot.activeSelf;
        public bool PondIsClear => pond != null && pond.sharedMaterial == clearWater;
        public bool GardenBloomShown => gardenBloomRoot != null && gardenBloomRoot.activeSelf;
        public bool GardenDormantShown => gardenDormant != null && gardenDormant.activeSelf;
        public bool YardLifeShown => yardLifeRoot != null && yardLifeRoot.activeSelf;
        public bool WindmillRepairedShown => AllActive(windmillRepaired);
        public bool WindmillDamagedShown => AnyActive(windmillDamaged);
        public bool VillageLifeShown => villageLifeRoot != null && villageLifeRoot.activeSelf;
        public bool GardenMotesPlaying => gardenMotes != null && gardenMotes.isPlaying;
        public bool ValleyMotesPlaying => valleyMotes != null && valleyMotes.isPlaying;
        public WindmillRotator Windmill => windmill;
        public NpcIdle[] Villagers => villagers;

        /// <summary>Where the garden stands (the smoke run points its camera there).</summary>
        public Vector3 GardenPosition => gardenBloomRoot != null ? gardenBloomRoot.transform.position : Vector3.zero;
        public float StreamBlend => lighting != null ? lighting.StreamBlend : 0f;
        public float GardenBlend => lighting != null ? lighting.GardenBlend : 0f;
        public float ValleyBlend => lighting != null ? lighting.ValleyBlend : 0f;
        public ValleyLighting Lighting => lighting;

        void Awake()
        {
            streamBloom = new PopGroup(bloomRoot);
            gardenBloom = new PopGroup(gardenBloomRoot);
            yardLife = new PopGroup(yardLifeRoot);
            villageLife = new PopGroup(villageLifeRoot);
        }

        void Update()
        {
            GameSession session = GameSession.Current;
            if (session == null) return;

            if (!initialised)
            {
                initialised = true;
                ApplyStage(session.Resonance, true);
                return;
            }
            if (session.Resonance == ShownStage) return;

            if (session.Resonance > ShownStage) PlayTransition(session.Resonance);
            else ApplyStage(session.Resonance, true);
        }

        /// <summary>Puts every piece of the world in the state for <paramref name="stage"/>; "settled" also sets the animated values.</summary>
        void ApplyStage(ResonanceStage stage, bool settled)
        {
            bool stream = stage >= ResonanceStage.AwakeningStream;
            bool garden = stage >= ResonanceStage.BloomingGarden;
            bool valley = stage >= ResonanceStage.RestoredValley;

            // Stage 1: the stream
            Material water = stream ? clearWater : murkyWater;
            if (pond != null && water != null) pond.sharedMaterial = water;
            if (springWater != null && water != null) springWater.sharedMaterial = water;
            if (troubledStream != null) troubledStream.SetActive(!stream);
            if (clearStream != null) clearStream.SetActive(stream);
            if (bloomRoot != null) bloomRoot.SetActive(stream);
            if (fountain != null)
            {
                if (stream) fountain.Play();
                else fountain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // Stage 2: the garden and the yard
            if (gardenDormant != null) gardenDormant.SetActive(!garden);
            if (gardenBloomRoot != null) gardenBloomRoot.SetActive(garden);
            if (yardLifeRoot != null) yardLifeRoot.SetActive(garden);
            Loop(gardenMotes, garden);
            if (villagers != null)
                foreach (NpcIdle villager in villagers)
                    if (villager != null) villager.SetLevel(villager.LevelFor(stage));

            // Stage 3: the windmill and the village
            SetActive(windmillDamaged, !valley);
            SetActive(windmillRepaired, valley);
            if (villageLifeRoot != null) villageLifeRoot.SetActive(valley);
            Loop(valleyMotes, valley);

            if (settled)
            {
                streamBloom.Set(stream ? 1f : 0f);
                SetSpringWater(stream ? 1f : 0f);
                gardenBloom.Set(garden ? 1f : 0f);
                yardLife.Set(garden ? 1f : 0f);
                villageLife.Set(valley ? 1f : 0f);
                if (windmill != null) windmill.SetSpinning(valley, true);
                if (lighting != null)
                {
                    lighting.SetStreamBlend(stream ? 1f : 0f);
                    lighting.SetGardenBlend(garden ? 1f : 0f);
                    lighting.SetValleyBlend(valley ? 1f : 0f);
                }
            }
            ShownStage = stage;
        }

        void PlayTransition(ResonanceStage stage)
        {
            if (transition != null) StopCoroutine(transition);
            ApplyStage(stage, true);   // everything the earlier stages showed stays; only this stage's pieces animate
            transition = StartCoroutine(Animate(stage));
        }

        IEnumerator Animate(ResonanceStage stage)
        {
            IsTransitioning = true;
            float seconds = stage == ResonanceStage.RestoredValley ? transitionSeconds * 1.3f : transitionSeconds;
            Step(stage, 0f);
            switch (stage)
            {
                case ResonanceStage.AwakeningStream:
                    if (pulse != null) pulse.Play();
                    break;
                case ResonanceStage.BloomingGarden:
                    if (gardenPulse != null) gardenPulse.Play();
                    break;
                case ResonanceStage.RestoredValley:
                    if (finalPulse != null) finalPulse.Play();
                    if (windmill != null)
                    {
                        windmill.SetSpinning(false, true);   // start from rest, then speed up
                        windmill.SetSpinning(true, false);
                    }
                    break;
            }

            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
            {
                Step(stage, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            Step(stage, 1f);
            IsTransitioning = false;
            transition = null;
        }

        /// <summary>Animated values of the stage being played, for progress 0..1.</summary>
        void Step(ResonanceStage stage, float k)
        {
            switch (stage)
            {
                case ResonanceStage.AwakeningStream:
                    if (lighting != null) lighting.SetStreamBlend(Mathf.SmoothStep(0f, 1f, k));
                    SetSpringWater(Mathf.SmoothStep(0f, 1f, k * 1.5f));
                    streamBloom.Set(k);
                    break;
                case ResonanceStage.BloomingGarden:
                    if (lighting != null) lighting.SetGardenBlend(Mathf.SmoothStep(0f, 1f, k));
                    gardenBloom.Set(k);
                    yardLife.Set(k);
                    break;
                case ResonanceStage.RestoredValley:
                    if (lighting != null) lighting.SetValleyBlend(Mathf.SmoothStep(0f, 1f, k));
                    villageLife.Set(k);
                    break;
            }
        }

        void SetSpringWater(float fill)
        {
            if (springWater != null) springWater.transform.localScale = Vector3.Lerp(springWaterDry, springWaterFull, Mathf.Clamp01(fill));
        }

        static void Loop(ParticleSystem system, bool on)
        {
            if (system == null) return;
            if (on)
            {
                if (!system.isPlaying) system.Play();
            }
            else
            {
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        static void SetActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (GameObject item in objects)
                if (item != null) item.SetActive(active);
        }

        static bool AllActive(GameObject[] objects)
        {
            if (objects == null || objects.Length == 0) return false;
            foreach (GameObject item in objects)
                if (item == null || !item.activeSelf) return false;
            return true;
        }

        static bool AnyActive(GameObject[] objects)
        {
            if (objects == null) return false;
            foreach (GameObject item in objects)
                if (item != null && item.activeSelf) return true;
            return false;
        }

        /// <summary>Children pop up one after another: child i starts a little later than child i-1 and overshoots slightly.</summary>
        sealed class PopGroup
        {
            readonly Transform[] children;
            readonly Vector3[] scales;

            public PopGroup(GameObject root)
            {
                int count = root != null ? root.transform.childCount : 0;
                children = new Transform[count];
                scales = new Vector3[count];
                for (int i = 0; i < count; i++)
                {
                    children[i] = root.transform.GetChild(i);
                    scales[i] = children[i].localScale;
                }
            }

            public void Set(float progress)
            {
                int count = children.Length;
                for (int i = 0; i < count; i++)
                {
                    float local = Mathf.Clamp01((progress - 0.5f * i / Mathf.Max(1, count)) / 0.5f);
                    float eased = 1f + 2.70158f * Mathf.Pow(local - 1f, 3f) + 1.70158f * Mathf.Pow(local - 1f, 2f);   // ease-out-back
                    children[i].localScale = scales[i] * Mathf.Max(0.001f, local <= 0f ? 0f : eased);
                }
            }
        }
    }
}
