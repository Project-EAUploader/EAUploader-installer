#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using VRC.SDK3A.Editor;

[InitializeOnLoad]
public class CustomBuildProcessor
{
    static CustomBuildProcessor()
    {
        RegisterSDKCallback();
    }

    [InitializeOnLoadMethod]
    public static void RegisterSDKCallback()
    {
        VRCSdkControlPanel.OnSdkPanelEnable += AddBuildHook;
    }

    private static void AddBuildHook(object sender, EventArgs e)
    {
        if (VRCSdkControlPanel.TryGetBuilder<IVRCSdkAvatarBuilderApi>(out var builder))
        {
            builder.OnSdkBuildStart += OnVRChatBuildStart;
            builder.OnSdkBuildFinish += OnVRChatBuildFinish;
        }
    }

    private static void OnVRChatBuildStart(object sender, object target)
    {
        DefinePreprocessorDirective("EA_ONBUILD", true);
        Debug.Log("VRChat Build started. EA_ONBUILD flag set to true.");
    }

    private static void OnVRChatBuildFinish(object sender, object target)
    {
        DefinePreprocessorDirective("EA_ONBUILD", false);
        Debug.Log("VRChat Build finished. EA_ONBUILD flag set to false.");
    }

    private static void DefinePreprocessorDirective(string directive, bool enable)
    {
        var definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        var allDefines = definesString.Split(';').ToList(); // ここで 'ToList' メソッドを使用
        if (enable)
        {
            if (!allDefines.Contains(directive))
            {
                allDefines.Add(directive);
            }
        }
        else
        {
            if (allDefines.Contains(directive))
            {
                allDefines.Remove(directive);
            }
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.Join(";", allDefines.ToArray()));
    }
}
#endif