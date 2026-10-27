using System;
using UnityEngine;

namespace Echoes
{
    /// <summary>Only the isolated pilot scene owns this comparison switch. Original scenes contain no pilot component.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class Phase52Pilot : MonoBehaviour
    {
        public GameObject OriginalVisual;
        public GameObject SkinnedVisual;
        public GameObject Route;
        public GameObject Portrait;
        public Renderer[] ReplacedTreeRenderers;
        public Terrain Terrain;
        public TerrainData BaselineTerrain;
        public TerrainData PilotTerrain;

        public bool BaselineMode { get; private set; }

        void Awake()
        {
            BaselineMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-pilot-baseline") >= 0;
            Apply(BaselineMode);
            Debug.Log("[Echoes][P52] isolated pilot, baseline=" + BaselineMode + "; owner approval PENDING; world expansion disabled.");
        }

        public void Apply(bool baseline)
        {
            OriginalVisual.SetActive(baseline);
            SkinnedVisual.SetActive(!baseline);
            Route.SetActive(!baseline);
            if (Portrait != null) Portrait.SetActive(!baseline);
            foreach (Renderer renderer in ReplacedTreeRenderers) if (renderer != null) renderer.enabled = baseline;
            Terrain.terrainData = baseline ? BaselineTerrain : PilotTerrain;
            Terrain.GetComponent<TerrainCollider>().terrainData = Terrain.terrainData;
            if (!baseline)
            {
                RenderSettings.fogColor = new Color(.67f, .77f, .82f);
                RenderSettings.fogDensity = .0075f;
            }
        }
    }
}
