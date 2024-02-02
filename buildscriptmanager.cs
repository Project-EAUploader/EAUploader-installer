#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using VRC.SDK3A.Editor;


public class CustomBuildProcessor
{
    private static bool initializationPerformed = false; 
    
    [InitializeOnLoadMethod]
    private static void InitializeOnLoad()
    {
        EditorApplication.update += WaitForIdle;
    }

    private static void WaitForIdle()
    {
        if (!EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // エディタがアイドル状態になったら、初期化処理を実行
            if (!initializationPerformed)
            {
                OnCustomBuildProcessor();
                initializationPerformed = true;

                // イベントを解除
                EditorApplication.update -= WaitForIdle;
            }
        }
    }

    private static void OnCustomBuildProcessor()
    {
        RegisterSDKCallback();
    }

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