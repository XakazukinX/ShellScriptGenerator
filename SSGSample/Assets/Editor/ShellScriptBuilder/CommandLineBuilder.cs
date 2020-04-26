#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[Serializable]
public class SceneBuildSetting
{
    public List<string> sceneNames = new List<string>();
}


public class CommandLineBuilder
{
    public static void TestFunction()
    {
        Debug.Log("Hello World!");
        string output = "";
        var args = System.Environment.GetCommandLineArgs();
        for(int i=0;i< args.Length;i++)
        {
            output += i + "->" + args[i] + "\n";
        }
        Debug.Log(output);
        Console.WriteLine("Test!");
    }
    
    public static void Build()
    {
        var output = "";
        var platform = BuildTarget.StandaloneWindows64;
        var sceneSetting = new SceneBuildSetting();
        bool isDevelopment = false;
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-outputPath":
                    output = args[i + 1];
                    break;
                case "-platform":
                    platform = (BuildTarget) Enum.Parse(typeof(BuildTarget), args[i + 1]);
                    break;
                case "-sceneSet":
                    sceneSetting = GetSceneBuildSetting(args[i + 1]);
                    break;
                case "-development":
                    isDevelopment = true;   //Developmentビルドにする
                    break;
                default:
                    break;
            }
        }

        var option = new BuildPlayerOptions();
        option.locationPathName = output;
        Debug.Log($"Output path = {output}");
        
        if(isDevelopment)
        {
            //optionsはビットフラグなので、|で追加していくことができる
            option.options = BuildOptions.Development | BuildOptions.AllowDebugging;
        }

        option.target = platform;

        //sceneSetting.jsonが引数に入ってなかったらProjectRootの親から持ってくる
        if (sceneSetting.sceneNames.Count == 0)
        {
            sceneSetting =
                GetSceneBuildSetting(
                    $"{ShellScriptGeneratorConstants.GetProjectParentPath()}/{ShellScriptGeneratorConstants.SceneBuildSettingJsonName}");
        }

        var scenes = GetCorrectScene(sceneSetting);
        //AddScenesToBuildSetting(scenes);
        option.scenes = scenes;
        Debug.Log("/////////////////////////");

        for (int i = 0; i < scenes.Length; i++)
        {
            Debug.Log(scenes[i]);
        }
        Debug.Log("/////////////////////////");
        var result = BuildPipeline.BuildPlayer(option);
        if(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("BUILD SUCCESS");
        }
        else
        {
            Debug.LogError($"BUILD FAILED {result.summary}");
        }
    }

    private static SceneBuildSetting GetSceneBuildSetting(string settingFilePath)
    {
        using (var sf = new FileStream(settingFilePath, FileMode.Open))
        {
            using (var sr = new StreamReader(sf))
            {
                var settingText = sr.ReadToEnd();
                Debug.Log(settingText);
                return JsonUtility.FromJson<SceneBuildSetting>(settingText);
            }
        }
    }
    
    private static void AddScenesToBuildSetting(string[] scenes)
    {
        //追加するシーンのリスト作成、追加
        List<EditorBuildSettingsScene> sceneList = new List<EditorBuildSettingsScene> ();
        for (int i = 0; i < scenes.Length; i++)
        {
            sceneList.Add(new EditorBuildSettingsScene(scenes[i], true));
            Debug.Log(scenes[i]);
        }
        
        EditorBuildSettings.scenes = sceneList.ToArray();
    }
    
    private static string[] GetCorrectScene(SceneBuildSetting buildSetting)
    {
        var allSceneNames = new List<string>();
        //guidが返ってくる
        var assets = AssetDatabase.FindAssets("t:Scene");
        
        for (int i = 0; i < buildSetting.sceneNames.Count; i++)
        {
            for (int k = 0; k < assets.Length; k++)
            {
                //guidからAssets以下のパスを取得する
                string path = AssetDatabase.GUIDToAssetPath(assets[k]);
                //シーン名のみを取得する
                string sceneName = Path.GetFileNameWithoutExtension(path);

                if (buildSetting.sceneNames[i] == sceneName)
                {
                    allSceneNames.Add(path);
                }
            }
        }
        
        return allSceneNames.ToArray();
    }
}
#endif