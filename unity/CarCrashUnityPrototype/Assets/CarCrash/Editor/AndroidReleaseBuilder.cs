using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarCrashEditor
{
    // The signing file is provisioned outside Assets, ignored by git, and deleted by CI.
    public static class AndroidReleaseBuilder
    {
        [Serializable] private sealed class Signing
        {
            public string keystore,storePassword,keyAlias,keyPassword,versionName,packageName;
            public int versionCode;
        }
        public static void Build()
        {
            var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            var config=Path.Combine(project,".ci/android-signing.json");
            if(!File.Exists(config))throw new InvalidOperationException("Missing private Android signing configuration. Run ci/android_release.py prepare first.");
            var signing=JsonUtility.FromJson<Signing>(File.ReadAllText(config));
            if(signing==null||signing.packageName!="com.ibsam.crashcar"||signing.versionCode<2||signing.versionCode>2100000000||string.IsNullOrWhiteSpace(signing.versionName))
                throw new InvalidOperationException("Invalid Android release identity or version.");
            var keystore=Path.GetFullPath(Path.Combine(project,signing.keystore));
            if(!File.Exists(keystore)||string.IsNullOrEmpty(signing.storePassword)||string.IsNullOrEmpty(signing.keyAlias)||string.IsNullOrEmpty(signing.keyPassword))
                throw new InvalidOperationException("Incomplete Android upload signing configuration.");
            const string scene="Assets/CarCrash/Scenes/ImpactRunPrototype.unity";
            if(!File.Exists(Path.Combine(project,scene)))throw new InvalidOperationException("The verified game scene is missing. Regenerate it locally before releasing.");
            PlayerSettings.companyName="Corsair Labs";
            PlayerSettings.productName="Crash Car: Elysian";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,signing.packageName);
            PlayerSettings.bundleVersion=signing.versionName;
            PlayerSettings.Android.bundleVersionCode=signing.versionCode;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)26;
            PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.useCustomKeystore=true;
            PlayerSettings.Android.keystoreName=keystore;
            PlayerSettings.Android.keystorePass=signing.storePassword;
            PlayerSettings.Android.keyaliasName=signing.keyAlias;
            PlayerSettings.Android.keyaliasPass=signing.keyPassword;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;
            PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            EditorUserBuildSettings.buildAppBundle=true;
            EditorUserBuildSettings.development=false;
            // Use the committed scene: reflection capture needs a GPU and should not run in headless CI.
            var output=Path.GetFullPath(Path.Combine(project,"../../build/Android/CrashCar.aab"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            try
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene},locationPathName=output,target=BuildTarget.Android,options=BuildOptions.CompressWithLz4HC});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Android bundle build failed: "+report.summary.result);
                Debug.Log("ANDROID_RELEASE_PASS package=com.ibsam.crashcar versionCode="+signing.versionCode+" bytes="+report.summary.totalSize);
            }
            finally
            {
                PlayerSettings.Android.keystorePass="";PlayerSettings.Android.keyaliasPass="";
                PlayerSettings.Android.keystoreName="";PlayerSettings.Android.keyaliasName="";
                PlayerSettings.Android.useCustomKeystore=false;
            }
        }
    }
}
