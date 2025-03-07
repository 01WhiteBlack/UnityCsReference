// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.Linq;
using UnityEditor.Utils;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml.XPath;
using UnityEditorInternal;
using System;
using System.Text.RegularExpressions;
using Mono.Cecil;
using NiceIO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Modules;
using UnityEditor.DeploymentTargets;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

namespace UnityEditor
{
    class MissingBuildPropertiesException : Exception {}

    // Holds data needed to verify a target against a set of requirements
    internal abstract class DeploymentTargetRequirements
    {
    }

    // Holds data needed for operating (launching etc) on a build
    internal abstract class BuildProperties : ScriptableObject
    {
        public static BuildProperties GetFromBuildReport(BuildReport report)
        {
            var allData = report.GetAppendices<BuildProperties>();
            if (allData.Length > 0)
                return allData[0];

            throw new MissingBuildPropertiesException();
        }

        public abstract DeploymentTargetRequirements GetTargetRequirements();
    }

    internal static class PostprocessBuildPlayer
    {
        internal const string StreamingAssets = "Assets/StreamingAssets";

        internal static void AddProjectBootConfigKey(string key)
        {
            AddProjectBootConfigKeyValue(key, null);
        }

        internal static void AddProjectBootConfigKeyValue(string key, string value)
        {
            projectBootConfigEntries[key] = value;
        }

        internal static bool RemoveProjectBootConfigKey(string key)
        {
            return projectBootConfigEntries.Remove(key);
        }

        internal static bool GetProjectBootConfigKeyValue(string key, out string value)
        {
            return projectBootConfigEntries.TryGetValue(key, out value);
        }

        internal static void ClearProjectBootConfigEntries()
        {
            projectBootConfigEntries.Clear();
        }

        private static Dictionary<string, string> projectBootConfigEntries = new Dictionary<string, string>();

        // Seems to be used only by PlatformDependent\AndroidPlayer\Editor\Managed\PostProcessAndroidPlayer.cs
        internal static bool InstallPluginsByExtension(string pluginSourceFolder, string extension, string debugExtension, string destPluginFolder, bool copyDirectories)
        {
            bool installedPlugins = false;

            if (!Directory.Exists(pluginSourceFolder))
                return installedPlugins;

            string[] contents = Directory.GetFileSystemEntries(pluginSourceFolder);
            foreach (string path in contents)
            {
                string fileName = Path.GetFileName(path);
                string fileExtension = Path.GetExtension(path);

                bool filenameMatch =    fileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase) ||
                    fileName.Equals(extension, StringComparison.OrdinalIgnoreCase);
                bool debugMatch =       !string.IsNullOrEmpty(debugExtension) &&
                    (fileExtension.Equals(debugExtension, StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals(debugExtension, StringComparison.OrdinalIgnoreCase));

                // Do we really need to check the file name here?
                if (filenameMatch || debugMatch)
                {
                    if (!Directory.Exists(destPluginFolder))
                        Directory.CreateDirectory(destPluginFolder);

                    string targetPath = Path.Combine(destPluginFolder, fileName);
                    if (copyDirectories)
                        FileUtil.CopyDirectoryRecursive(path, targetPath);
                    else if (!Directory.Exists(path))
                        FileUtil.UnityFileCopy(path, targetPath);

                    installedPlugins = true;
                }
            }
            return installedPlugins;
        }

        internal static void InstallStreamingAssets(string stagingAreaDataPath) =>
            InstallStreamingAssets(stagingAreaDataPath, null);

        internal static void InstallStreamingAssets(string stagingAreaDataPath, BuildReport report) =>
            InstallStreamingAssets(stagingAreaDataPath, "StreamingAssets", report);

        internal static void InstallStreamingAssets(string stagingAreaDataPath, string streamingAssetsFolderName, BuildReport report)
        {
            if (Directory.Exists(StreamingAssets))
            {
                var outputPath = Path.Combine(stagingAreaDataPath, streamingAssetsFolderName);
                FileUtil.CopyDirectoryRecursiveForPostprocess(StreamingAssets, outputPath, true);
                report?.RecordFilesAddedRecursive(outputPath, CommonRoles.streamingAsset);
            }

            foreach (var(dst, src) in BuildPlayerContext.ActiveInstance.StreamingAssets)
            {
                NPath targetPlayerPath = $"{stagingAreaDataPath}/{streamingAssetsFolderName}/{dst}";
                if (targetPlayerPath.Exists())
                {
                    var errorMessage =
                        "error: Callback provided streaming assets file conflicts with file already present in project." +
                        $" Project file 'StreamingAssets/{dst}'. Callback provided file '{src}'.";
                    Debug.LogError(errorMessage);
                    throw new BuildFailedException(errorMessage);
                }
                FileUtil.UnityFileCopy(src.ToString(), targetPlayerPath.EnsureParentDirectoryExists().ToString());
                report?.RecordFileAdded(targetPlayerPath.ToString(SlashMode.Native), CommonRoles.streamingAsset);
            }
        }

        internal static string GetStreamingAssetsBundleManifestPath()
        {
            string manifestPath = "";
            if (Directory.Exists(StreamingAssets))
            {
                var tmpPath = Path.Combine(StreamingAssets, "StreamingAssets.manifest");
                if (File.Exists(tmpPath))
                    manifestPath = tmpPath;
            }

            return manifestPath;
        }

        static public string PrepareForBuild(BuildOptions options, BuildTargetGroup targetGroup, BuildTarget target)
        {
            var postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor == null)
                return null;
            return postprocessor.PrepareForBuild(options, target);
        }

        [RequiredByNativeCode]
        static public bool SupportsScriptsOnlyBuild(BuildTargetGroup targetGroup, BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
            {
                return postprocessor.SupportsScriptsOnlyBuild();
            }

            return false;
        }

        [RequiredByNativeCode]
        static public bool UsesBeeBuild(BuildTargetGroup targetGroup, BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
            {
                return postprocessor.UsesBeeBuild();
            }

            return false;
        }

        static public string GetExtensionForBuildTarget(BuildTargetGroup targetGroup, BuildTarget target, int subtarget, BuildOptions options)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor == null)
                return string.Empty;
            return postprocessor.GetExtension(target, subtarget, options);
        }

        static public string GetExtensionForBuildTarget(BuildTargetGroup targetGroup, BuildTarget target, BuildOptions options) =>
            GetExtensionForBuildTarget(targetGroup, target, EditorUserBuildSettings.GetActiveSubtargetFor(target), options);

        static public bool SupportsInstallInBuildFolder(BuildTargetGroup targetGroup, BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
            {
                return postprocessor.SupportsInstallInBuildFolder();
            }

            return false;
        }

        static public bool SupportsLz4Compression(BuildTargetGroup targetGroup, BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
                return postprocessor.SupportsLz4Compression();
            return false;
        }

        static public Compression GetDefaultCompression(BuildTargetGroup targetGroup, BuildTarget target)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
                return postprocessor.GetDefaultCompression();
            return Compression.None;
        }

        private class NoTargetsFoundException : Exception
        {
            public NoTargetsFoundException() : base() {}
            public NoTargetsFoundException(string message) : base(message) {}
        }

        static public void Launch(BuildTargetGroup targetGroup, BuildTarget buildTarget, string path, string productName, BuildOptions options, BuildReport buildReport)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, buildTarget);
            if (postprocessor != null)
            {
                BuildLaunchPlayerArgs args;
                args.target = buildTarget;
                args.playerPackage = BuildPipeline.GetPlaybackEngineDirectory(buildTarget, options);
                args.installPath = path;
                args.productName = productName;
                args.options = options;
                args.report = buildReport;

                postprocessor.LaunchPlayer(args);
            }
            else
            {
                throw new UnityException(
                    $"Launching for target group {targetGroup}, build target {buildTarget} is not supported: There is no build post-processor available.");
            }
        }

        static public void LaunchOnTargets(BuildTargetGroup targetGroup, BuildTarget buildTarget, Build.Reporting.BuildReport buildReport, List<DeploymentTargetId> launchTargets)
        {
            try
            {
                // Early out so as not to show/update progressbars unnecessarily
                if (buildReport == null)
                    throw new System.NotSupportedException();

                ProgressHandler progressHandler = new ProgressHandler("Deploying Player",
                    delegate(string title, string message, float globalProgress)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(title, message, globalProgress))
                            throw new DeploymentOperationAbortedException();
                    }, 0.1f);     // BuildPlayer.cpp starts off at 0.1f for some reason

                var taskManager = new ProgressTaskManager(progressHandler);

                // Launch on all selected targets
                taskManager.AddTask(() =>
                {
                    int successfulLaunches = 0;
                    var exceptions = new List<DeploymentOperationFailedException>();
                    foreach (var target in launchTargets)
                    {
                        try
                        {
                            var manager = DeploymentTargetManager.CreateInstance(targetGroup, buildReport.summary.platform);
                            var buildProperties = BuildProperties.GetFromBuildReport(buildReport);
                            manager.LaunchBuildOnTarget(buildProperties, target, taskManager.SpawnProgressHandlerFromCurrentTask());
                            successfulLaunches++;
                        }
                        catch (DeploymentOperationFailedException e)
                        {
                            exceptions.Add(e);
                        }
                    }

                    foreach (var e in exceptions)
                        UnityEngine.Debug.LogException(e);

                    if (successfulLaunches == 0)
                    {
                        // TODO: Maybe more specifically no compatible targets?
                        throw new NoTargetsFoundException("Could not launch build");
                    }
                });

                taskManager.Run();
            }
            catch (DeploymentOperationFailedException e)
            {
                UnityEngine.Debug.LogException(e);
                EditorUtility.DisplayDialog(e.title, e.Message, "Ok");
            }
            catch (DeploymentOperationAbortedException)
            {
                System.Console.WriteLine("Deployment aborted");
            }
            catch (NoTargetsFoundException)
            {
                throw new UnityException(string.Format("Could not find any valid targets to launch on for {0}", buildTarget));
            }
        }

        [RequiredByNativeCode]
        static public void UpdateBootConfig(BuildTargetGroup targetGroup, BuildTarget target, BootConfigData config, BuildOptions options)
        {
            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
                postprocessor.UpdateBootConfig(target, config, options);

            foreach (var keyValue in projectBootConfigEntries)
            {
                if ((keyValue.Value == null) || keyValue.Value.All(char.IsWhiteSpace))
                    config.AddKey(keyValue.Key);
                else
                    config.Set(keyValue.Key, keyValue.Value);
            }
        }

        static public void Postprocess(BuildTargetGroup targetGroup, BuildTarget target, int subtarget, string installPath, string companyName, string productName,
            int width, int height, BuildOptions options,
            RuntimeClassRegistry usedClassRegistry, BuildReport report)
        {
            string stagingArea = "Temp/StagingArea";
            string stagingAreaData = "Temp/StagingArea/Data";
            string stagingAreaDataManaged = "Temp/StagingArea/Data/Managed";
            string playerPackage = BuildPipeline.GetPlaybackEngineDirectory(target, options);

            // Disallow providing an empty string as the installPath
            bool willInstallInBuildFolder = (options & BuildOptions.InstallInBuildFolder) != 0 && SupportsInstallInBuildFolder(targetGroup, target);
            if (installPath == String.Empty && !willInstallInBuildFolder)
                throw new Exception(installPath + " must not be an empty string");

            IBuildPostprocessor postprocessor = ModuleManager.GetBuildPostProcessor(targetGroup, target);
            if (postprocessor != null)
            {
                BuildPostProcessArgs args;
                args.target = target;
                args.subtarget = subtarget;
                args.stagingAreaData = stagingAreaData;
                args.stagingArea = stagingArea;
                args.stagingAreaDataManaged = stagingAreaDataManaged;
                args.playerPackage = playerPackage;
                args.installPath = installPath;
                args.companyName = companyName;
                args.productName = productName;
                args.productGUID = PlayerSettings.productGUID;
                args.options = options;
                args.usedClassRegistry = usedClassRegistry;
                args.report = report;

                BuildProperties props;
                try
                {
                    postprocessor.PostProcess(args, out props);
                }
                catch (BuildFailedException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    // Rethrow exceptions during build postprocessing as BuildFailedException, so we don't pretend the build was fine.
                    throw new BuildFailedException(e);
                }
                report.AddAppendix(props);

                return;
            }

            // If postprocessor is not provided, build target is not supported
            throw new UnityException(string.Format("Build target '{0}' not supported", target));
        }

        public static void PostProcessCompletedBuild(BuildPostProcessArgs args)
        {
            var postprocessor = ModuleManager.GetBuildPostProcessor(BuildPipeline.GetBuildTargetGroup(args.target), args.target);
            postprocessor.PostProcessCompletedBuild(args);
        }

        internal static string ExecuteSystemProcess(string command, string args, string workingdir)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = command,
                Arguments = args,
                WorkingDirectory = workingdir,
                CreateNoWindow = true
            };
            var p = new Program(psi);
            p.Start();
            while (!p.WaitForExit(100))
                ;

            string output = p.GetStandardOutputAsString();
            p.Dispose();
            return output;
        }

        public static string subDir32Bit
        {
            get
            {
                return "x86";
            }
        }

        public static string subDir64Bit
        {
            get
            {
                return "x86_64";
            }
        }
    }
}
