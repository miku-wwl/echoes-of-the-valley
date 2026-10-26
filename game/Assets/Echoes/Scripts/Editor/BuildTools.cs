using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Echoes.EditorTools
{
    /// <summary>
    /// Windows x64 Mono builds. Batch mode: Unity.exe -batchmode -quit -projectPath game
    /// -executeMethod Echoes.EditorTools.BuildTools.BuildWindows (development) or BuildWindowsRelease (a normal player: no
    /// Development Build watermark, no profiler or script debugging) [-echoesBuildPath dir]. Default output is the git-ignored
    /// artifacts/build/win64 (development) or artifacts/build/win64-release folder next to the game/ project.
    /// </summary>
    public static class BuildTools
    {
        public const string ExecutableName = "EchoesOfTheValley.exe";

        /// <summary>The version shown on the title screen (Application.version) in a release build.</summary>
        public const string ReleaseVersion = "1.0.0-rc1";

        [MenuItem("Echoes/Build Windows (Development)")]
        public static void BuildWindows() => Build(false);

        [MenuItem("Echoes/Build Windows (Release)")]
        public static void BuildWindowsRelease() => Build(true);

        static void Build(bool release)
        {
            string outputDirectory = ReadArgument("-echoesBuildPath")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "artifacts", "build", release ? "win64-release" : "win64"));
            Directory.CreateDirectory(outputDirectory);

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new InvalidOperationException("No scenes in Build Settings; run ProjectSetup.GenerateAll first.");

            string previousVersion = PlayerSettings.bundleVersion;
            if (release) PlayerSettings.bundleVersion = ReleaseVersion;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = release ? BuildOptions.None : BuildOptions.Development
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            PlayerSettings.bundleVersion = previousVersion;
            Debug.Log("[Echoes] BUILD " + summary.result + " (" + (release ? "release" : "development") + ", version " + (release ? ReleaseVersion : previousVersion)
                + "): errors=" + summary.totalErrors + " warnings=" + summary.totalWarnings
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
