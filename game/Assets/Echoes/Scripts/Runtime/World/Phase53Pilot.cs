using System;
using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>Scoped Phase53 presentation/pond safety. The preserved B5 scene and assets are never rewritten.</summary>
    [DefaultExecutionOrder(-370)]
    public sealed class Phase53Pilot : MonoBehaviour
    {
        public CharacterVisual2D5D Hero;
        public Renderer HeroCard;
        public SpriteSheet25D BeforeHeroSheet, HeroSheet;
        public Material BeforeHeroMaterial, HeroMaterial;
        public NpcVisual2D5D Mira;
        public Renderer MiraCard;
        public SpriteSheet25D BeforeMiraSheet, MiraSheet;
        public Material BeforeMiraMaterial, MiraMaterial;
        public GameObject NewPond;
        public Renderer[] Hidden = Array.Empty<Renderer>();
        public Collider[] OldColliders = Array.Empty<Collider>();
        public Collider[] DockColliders = Array.Empty<Collider>();
        public Terrain Terrain;
        public TerrainData BeforeTerrain, AfterTerrain;
        public StaticProp25D Workshop;
        public Material BeforeWorkshop, AfterWorkshop;
        public UnityEngine.UI.RawImage Minimap;
        public Texture BeforeMap, AfterMap;
        public bool BeforeMode { get; private set; }

        IEnumerator Start()
        {
            Apply(Array.IndexOf(Environment.GetCommandLineArgs(), "-echoes-p53-before") >= 0);
            yield return null; // Let gameplay bootstrap and spawn placement finish.
            if (!BeforeMode) RecoverInvalidSavedPosition();
        }
        public void Apply(bool before)
        {
            BeforeMode = before;
            if (Hero != null) Hero.UseSheet(before ? BeforeHeroSheet : HeroSheet);
            if (HeroCard != null) HeroCard.sharedMaterial = before ? BeforeHeroMaterial : HeroMaterial;
            if (Mira != null) Mira.UseSheet(before ? BeforeMiraSheet : MiraSheet);
            if (MiraCard != null) MiraCard.sharedMaterial = before ? BeforeMiraMaterial : MiraMaterial;
            foreach (Renderer r in Hidden) if (r != null) r.enabled = before;
            foreach (Collider c in OldColliders) if (c != null) c.enabled = before;
            foreach (Collider c in DockColliders) if (c != null) c.enabled = !before;
            if (NewPond != null) NewPond.SetActive(!before);
            if (Terrain != null)
            {
                Terrain.terrainData = before ? BeforeTerrain : AfterTerrain;
                Terrain.GetComponent<TerrainCollider>().terrainData = Terrain.terrainData;
            }
            if (Workshop != null)
            {
                Workshop.GetComponent<Renderer>().sharedMaterial = before ? BeforeWorkshop : AfterWorkshop;
                Workshop.Configure(Workshop.Frame, Workshop.BaseLeft, Workshop.BaseFront, Workshop.BaseRight, Workshop.BaseU, !before);
            }
            if (Minimap != null) Minimap.texture = before ? BeforeMap : AfterMap;
        }
        /// <summary>Defensive spawn recovery only: preserve progression and move an unsafe initial water position to the dock approach.</summary>
        public bool RecoverInvalidSavedPosition()
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null || !Phase53Pond.IsDeep(player.transform.position)) return false;
            Vector3 bank = new Vector3(-21.1f, 0f, -14f);
            bank.y = Terrain.SampleHeight(bank) + Terrain.transform.position.y + .08f;
            player.Teleport(bank);
            FindFirstObjectByType<FixedFollowCamera25D>()?.Snap();
            Debug.Log("[Echoes][P53] Unsafe initial water position moved to the existing dock approach; session/items unchanged.");
            return true;
        }
    }
}
