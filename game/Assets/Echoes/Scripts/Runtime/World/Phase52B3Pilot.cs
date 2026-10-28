using System;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Presentation switch of the isolated Phase 5.2-B3 2.5D pilot scene (original scenes contain no B3 component). It runs after the B2
    /// pilot switch, which would otherwise re-enable the B2 skinned hero, and makes the illustrated sprites and the fixed camera the only
    /// player-facing presentation. "-echoes-b3-legacy-hero" shows the B2 3D hero under the same camera instead (for matched comparisons).
    /// </summary>
    [DefaultExecutionOrder(-400)]
    public sealed class Phase52B3Pilot : MonoBehaviour
    {
        public GameObject CharacterVisual;
        public GameObject[] LegacyVisuals = Array.Empty<GameObject>();
        public GameObject[] VillagerVisuals = Array.Empty<GameObject>();
        public Renderer[] HiddenVillagerRenderers = Array.Empty<Renderer>();
        public GameObject[] DialogueBusts = Array.Empty<GameObject>();
        public FixedFollowCamera25D FollowCamera;
        public ThirdPersonCamera LegacyCamera;

        public bool LegacyHeroMode { get; private set; }

        void Start()
        {
            LegacyHeroMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-b3-legacy-hero") >= 0;
            Apply(LegacyHeroMode);
            Debug.Log("[Echoes][P52 B3] 2.5D pilot, legacyHero=" + LegacyHeroMode + "; owner approval PENDING; world expansion disabled.");
        }

        public void Apply(bool legacyHero)
        {
            var pilot = GetComponent<Phase52Pilot>();
            if (CharacterVisual != null) CharacterVisual.SetActive(!legacyHero);
            if (pilot != null && pilot.SkinnedVisual != null) pilot.SkinnedVisual.SetActive(legacyHero);
            if (pilot != null && pilot.OriginalVisual != null) pilot.OriginalVisual.SetActive(false);
            foreach (GameObject v in VillagerVisuals) if (v != null) v.SetActive(!legacyHero);
            foreach (Renderer r in HiddenVillagerRenderers) if (r != null) r.enabled = legacyHero;
            if (FollowCamera != null) FollowCamera.enabled = true;
            if (FollowCamera != null) FollowCamera.Snap();
        }
    }
}
