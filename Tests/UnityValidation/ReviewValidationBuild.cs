using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class ReviewValidationBuild
{
    public static void Run()
    {
        try
        {
            PlayerSettings.companyName = "ReviewValidation";
            PlayerSettings.productName = "ChipChallengersReviewValidation";
            var output = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Validation", "ChipChallengers.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log("REVIEW_VALIDATION_BUILD: " + report.summary.result + ", errors=" + report.summary.totalErrors);
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }
}
