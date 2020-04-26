#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


public class BuildShellScriptGenerator : EditorWindow
{
    private const float WINDOWSIZE_W = 500.0f;
    private const float WINDOWSIZE_H = 420.0f;

    [SerializeField] private BuildShellScriptSetting _shellScriptSetting = new BuildShellScriptSetting();

    private string ProjectRoot;
    private string ProjectName;
    private string AppName;
    private BuildTarget Platform = BuildTarget.NoTarget;
    private MonoScript BuildScript;
    private int SelectMethodIndex;
    


    [MenuItem("BuildUtility/ShellScriptGenerator")]
    public static void WindowOpen()
    {
        var window = GetWindow<BuildShellScriptGenerator>("Shell Script Generator");
        window.maxSize = window.minSize = new Vector2(WINDOWSIZE_W, WINDOWSIZE_H);
    }
    
    
    private void OnGUI()
    {
        var labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.wordWrap = true;

        //ApplicationDataPathの上の階層はProjectのRootDirectoryになっているはず
        //その上がgitの置かれるディレクトリ
        ProjectRoot = Directory.GetParent(Application.dataPath).FullName;
        ProjectName = Path.GetFileName(ProjectRoot);
        
        EditorGUILayout.LabelField($"ProjectRoot : {ProjectRoot}", labelStyle);
        EditorGUILayout.LabelField($"ProjectName : {ProjectName}", labelStyle);
        _shellScriptSetting.ProjectName = ProjectName;
        
        //アプリケーション名,プラットフォームを入力させる。
        //プラットフォームに応じて拡張子の添付などを行う
        AppName = EditorGUILayout.TextField("アプリケーションの名称", AppName);
        Platform = (BuildTarget) EditorGUILayout.EnumPopup("Platform", Platform);
        _shellScriptSetting.ApplicationName = GenerateAppName(AppName, Platform);
        _shellScriptSetting.TargetPlatform = Platform;
        
        //関数を登録させる
        BuildScript = (MonoScript) EditorGUILayout.ObjectField("ビルドスクリプト", BuildScript, typeof(MonoScript), true);
        if (BuildScript != null)
        {
            //登録したObjectからMonoScriptを取得
            var script = BuildScript;
            //リフレクションしてClassを取得
            var classInfo = script.GetClass();
            if (classInfo != null)
            {
                //public static でvoidのメソッドを取得
                var methods = classInfo.GetMethods()
                    .Where(x => x.ReturnType == typeof(void) && x.IsStatic && x.IsPublic);
                var methodNameList = new List<string>();
                foreach (var method in methods)
                {
                    methodNameList.Add(method.Name);
                }

                if (methodNameList.Count == 0)
                { 
                    methodNameList.Add("None");
                    EditorGUILayout.HelpBox("登録したスクリプトアクセス可能なメソッドを取得できませんでした。\n " +
                                            "public staticなメソッドが存在するか確認してください", MessageType.Error);
                }

                var selectOptions = methodNameList.ToArray();
                
                //選択中のインデックスが条件に合うメソッド数よりも多くなった場合はいったんインデックスを0に戻しとく
                //(例えば途中でビルド用のスクリプトを修正して関数を削った時とか)
                if (SelectMethodIndex >= selectOptions.Length)
                {
                    SelectMethodIndex = 0;
                    Debug.LogError($"ShellScriptGeneratorError! : 選択されたインデックスと関数の数に整合性が取れなくなりました。インデックスを0に戻します");
                }


                SelectMethodIndex = EditorGUILayout.Popup("呼び出す関数", SelectMethodIndex, selectOptions);

                var buildMethod = $"{classInfo.Name}.{selectOptions[SelectMethodIndex]}";
                //NameSpace内にある場合はそれも含めるかたちにする
                if (!string.IsNullOrEmpty(classInfo.Namespace))
                {
                    buildMethod = $"{classInfo.Namespace}.{buildMethod}";
                }

                _shellScriptSetting.BuildMethod = buildMethod;
            }
            else
            {
                EditorGUILayout.HelpBox("登録スクリプトからアクセス可能なクラスを検出できませんでした", MessageType.Error);
            }
            
        }

        //各種ボタン
        labelStyle.fontStyle = FontStyle.Bold;
        
        //buildSetting.txtを生成する
        GUILayout.Space(30);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            //Projectのルートディレクトリの上がgitの置かれるディレクトリ
            var path = $"{ShellScriptGeneratorConstants.GetProjectParentPath()}/{ShellScriptGeneratorConstants.SettingFileName}";
            
            EditorGUILayout.LabelField($"BuildShell と {ShellScriptGeneratorConstants.SettingFileName} を生成します");
            EditorGUILayout.LabelField($"生成先のパス ({ShellScriptGeneratorConstants.GetProjectParentPath()})", labelStyle);
            GUILayout.Space(10);
            if (GUILayout.Button("Generate BuildShell.sh and buildSetting.txt"))
            {
                GenerateSettingFile(path);
            }
        }
        EditorGUILayout.EndVertical();
        
        //SceneBuildSetting.jsonを生成する
        GUILayout.Space(30);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.LabelField($"{ShellScriptGeneratorConstants.SceneBuildSettingJsonName} を生成します");
            EditorGUILayout.LabelField($"生成先のパス ({ShellScriptGeneratorConstants.GetProjectParentPath()})", labelStyle);
            GUILayout.Space(10);
            if (GUILayout.Button("Generate SceneBuildSetting.json"))
            {
                ShellScriptGeneratorConstants.GenerateSceneBuildSettingJson();
            }
        }
        EditorGUILayout.EndVertical();
        
        
        //いったんJsonで出力してテストする
        GUILayout.Space(30);
        EditorGUILayout.BeginVertical(GUI.skin.box);
        {
            EditorGUILayout.LabelField($"SettingFileNameに使用されるデータをJson形式でConsole上に出力します");
            GUILayout.Space(10);
            if (GUILayout.Button("Debug!"))
            {
                Test();
            }
        }
        EditorGUILayout.EndVertical();

    }

    private string GenerateAppName(string _appName, BuildTarget _platForm)
    {
        //アプリケーション名に入力がなかったらとりあえずプロジェクト名を入れとく
        if (string.IsNullOrEmpty(_appName))
        {
            _appName = Application.productName;
            AppName = _appName;
        }

        var result = $"/{_appName}";
        
        switch (_platForm)
        {
            case BuildTarget.StandaloneWindows64:
                result = $"/{_appName}.exe";
                break;
            case BuildTarget.StandaloneOSX:
                result = $"/{_appName}.app";
                break;
            default:
                break;
        }

        return result;
    }

    private void GenerateSettingFile(string path)
    {
        Debug.Log(path);
        using (var fs = new FileStream($"{path}",FileMode.Create))
        {
            using (var sw = new StreamWriter(fs))
            {
                sw.WriteLine($"{ShellScriptGeneratorConstants.ProjectNameText}\"{_shellScriptSetting.ProjectName}\"");
                sw.WriteLine($"{ShellScriptGeneratorConstants.ApplicationNameText}\"{_shellScriptSetting.ApplicationName}\"");
                sw.WriteLine($"{ShellScriptGeneratorConstants.TargetPlatformText}\"{_shellScriptSetting.TargetPlatform.ToString()}\"");
                sw.WriteLine($"{ShellScriptGeneratorConstants.BuildMethodText}\"{_shellScriptSetting.BuildMethod}\"");
                sw.WriteLine($"{ShellScriptGeneratorConstants.BuildCommandText}\"{ShellScriptGeneratorConstants.COMMAND}\"");
            }
        }
        GenerateShellScriptFile();
        Debug.Log("Generate Done");
    }

    private void GenerateShellScriptFile()
    {
        using (var fs = new FileStream($"{ShellScriptGeneratorConstants.GetProjectParentPath()}/BuildShell.sh",
            FileMode.Create))
        {
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(
                    "#!/bin/sh\n" +
                    ". ./buildSetting.txt\n" +
                    "echo \"//// $PROJECT_NAME BuildStart ////\"\n" +
                    "echo \"\"\n" +
                    "mkdir -p ./$PROJECT_NAME/Build/\n" +
                    "echo \"//// Build Parameters ////\"\n" +
                    "echo \"\"\n" +
                    "echo UnityPath : $1\n" +
                    "echo WorkSpacePath : $2\n" +
                    "echo TargetPlatform : $TARGET_PLATFORM\n" +
                    "echo ApplicationName : $APPLICATION_NAME\n" +
                    "echo BuildCommand : $BUILD_COMMAND\n" +
                    "echo Run : $1 $BUILD_COMMAND\n" +
                    "echo \"\"\n" +
                    "echo \"/////////////////////////\"\n" +
                    "$1 $BUILD_COMMAND"
                );
            }
        }
    }
    

    private void Test()
    {
        var shellScriptSettingJson = JsonUtility.ToJson(_shellScriptSetting);
        Debug.Log(shellScriptSettingJson);
    }
}


#endif