using UnityEditor;
using UnityEngine;

namespace Echoes.EditorTools
{
    internal sealed class PrefabSet
    {
        public GameObject TreeRound;
        public GameObject TreeTall;
        public GameObject TreeBlossom;
        public GameObject Rock;
        public GameObject Hero;
        public GameObject Villager;
        public GameObject HerbNode;
        public GameObject FlowerNode;
        public GameObject OreNode;
        public GameObject Hen;
        public GameObject[] FlowerPatches;
    }

    /// <summary>Saves the reusable prefabs. Existing prefab files are overwritten in place, so GUIDs stay stable between runs.</summary>
    internal static class PrefabLibrary
    {
        public static PrefabSet Build(MaterialLibrary m)
        {
            AssetPaths.EnsureFolder(AssetPaths.Prefabs);

            PrefabSet set = new PrefabSet();
            set.TreeRound = Save(Pieces.Tree("Tree_Round", m, "LeafA", "LeafB", 1.7f));
            set.TreeTall = Save(Pieces.Tree("Tree_Tall", m, "LeafB", "LeafA", 2.4f));
            set.TreeBlossom = Save(Pieces.Tree("Tree_Blossom", m, "Blossom", "BlossomLight", 1.7f));
            set.Rock = Save(Pieces.Rock(m));
            set.Hero = Save(Pieces.Character("Hero", m, "TunicHero", "HairHero", "Scarf", hat: false, longHair: false, skirt: false, satchel: true));

            GameObject villager = Pieces.Character("Villager", m, "TunicMira", "HairMira", "Cream", hat: true, longHair: true, skirt: true, satchel: false);
            CapsuleCollider collider = villager.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.8f, 0f);
            collider.height = 1.6f;
            collider.radius = 0.34f;
            set.Villager = Save(villager);

            set.HerbNode = Save(GameplayPieces.HerbNode(m));
            set.FlowerNode = Save(GameplayPieces.FlowerNode(m));
            set.OreNode = Save(GameplayPieces.OreNode(m));
            set.Hen = Save(GameplayPieces.Hen(m));

            string[] petals = { "FlowerPink", "FlowerYellow", "FlowerWhite", "FlowerLilac" };
            set.FlowerPatches = new GameObject[petals.Length];
            for (int i = 0; i < petals.Length; i++)
                set.FlowerPatches[i] = Save(Pieces.FlowerPatch("Flowers_" + petals[i].Substring(6), m, petals[i]));

            AssetDatabase.SaveAssets();
            return set;
        }

        static GameObject Save(GameObject temporary)
        {
            string path = AssetPaths.Prefabs + "/" + temporary.name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, path);
            Object.DestroyImmediate(temporary);
            return prefab;
        }
    }
}
