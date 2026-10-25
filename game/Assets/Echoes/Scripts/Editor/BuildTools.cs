using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Windows x64 Mono development build. Batch mode: Unity.exe -batchmode -quit -projectPath game
    /// -executeMethod Echoes.EditorTools.BuildTools.BuildWindows [-echoesBuildPath dir]. Default output is the
    /// git-ignored artifacts/build/win64 folder next to the game/ project.
    /// </summary>
    public static class BuildTools
    {
        public const string ExecutableName = "EchoesOfTheValley.exe";

        [MenuItem("Echoes/Build Windows (Development)")]
        public static void BuildWindows()
        {
            string outputDirectory = ReadArgument("-echoesBuildPath")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "build", "win64"));
            Directory.CreateDirectory(outputDirectory);

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No scenes in Build Settings; run ProjectSetup.GenerateAll first.");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log("[Echoes] BUILD " + summary.result + ": errors=" + summary.totalErrors + " warnings=" + summary.totalWarnings
                + " size=" + summary.totalSize + " bytes time=" + summary.totalTime + " id=" + summary.guid + " scenes=" + string.Join(",", scenes)
                + " output=" + summary.outputPath);

            if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static string ReadArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
