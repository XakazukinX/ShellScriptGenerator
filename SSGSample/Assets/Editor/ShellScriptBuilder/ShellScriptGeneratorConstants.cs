#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

public class ShellScriptGeneratorConstants
{
    public const string SceneBuildSettingJsonName = "SceneBuildSetting.json";

    [MenuItem("BuildUtility/GenerateSceneBuildSetting")]
    public static void GenerateSceneBuildSettingJson()
    {
        var setting = new SceneBuildSetting();
        var scenes = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            var sceneName = Path.GetFileNameWithoutExtension(scenes[i].path);
            Debug.Log(sceneName);
            setting.sceneNames.Add(sceneName);
        }

        using (var fs = new FileStream($"{GetProjectParentPath()}/{SceneBuildSettingJsonName}", FileMode.Create))
        {
            using (var sw = new StreamWriter(fs))
            {
                var jsonText = JsonUtility.ToJson(setting);
                sw.Write(jsonText);
            }
        }
        
        AssetDatabase.Refresh();
    }

    public static string GetProjectParentPath()
    {
        var ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
        return Directory.GetParent(ProjectRoot).FullName;
    }

    public const string SettingFileName = "buildSetting.txt";
    
    public const string ProjectNameText = "PROJECT_NAME=";
    public const string ApplicationNameText = "APPLICATION_NAME=";
    public const string TargetPlatformText = "TARGET_PLATFORM=";
    public const string BuildMethodText = "BUILD_METHOD";
    public const string BuildCommandText = "BUILD_COMMAND";
    public const string COMMAND =
        "-batchmode -quit -logfile $2/log.txt -projectPath $2/$PROJECT_NAME -executeMethod $BUILD_METHOD -outputPath Build$APPLICATION_NAME -platform $TARGET_PLATFORM";
}
#endif