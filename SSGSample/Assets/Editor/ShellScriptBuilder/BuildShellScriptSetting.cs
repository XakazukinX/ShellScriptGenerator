#if UNITY_EDITOR
using System;
using UnityEditor;

[Serializable]
public class BuildShellScriptSetting
{
    public string ProjectName = "";
    public string ApplicationName = "";
    public BuildTarget TargetPlatform = BuildTarget.NoTarget;
    public string BuildMethod = "";
}
#endif