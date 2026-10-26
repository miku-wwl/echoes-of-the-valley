using System;
using System.IO;
using UnityEditor;

namespace Echoes.EditorTools
{
    /// <summary>Asset locations used by the generators, plus recursive folder creation.</summary>
    internal static class AssetPaths
    {
        public const string Root = "Assets/Echoes";
        public const string Materials = Root + "/Art/Materials";
        public const string Terrain = Root + "/Art/Terrain";
        public const string Meshes = Root + "/Art/Meshes";
        public const string Textures = Root + "/Art/Textures";
        public const string Settings = Root + "/Art/Settings";
        public const string Audio = Root + "/Audio";
        public const string Prefabs = Root + "/Prefabs";
        public const string Scenes = Root + "/Scenes";
        public const string MainMenuScene = Scenes + "/MainMenu.unity";
        public const string ValleyScene = Scenes + "/Valley.unity";

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent)) throw new ArgumentException("Cannot create folder " + path);
            parent = parent.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
