using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

[InitializeOnLoad]
public class DependencyChecker
{
    private static string packageCheckFilePath = "Assets/EAUploader-installer/package-check.json";
    private static int totalUpdatesNeeded = 0;
    private static int updatesCompleted = 0;
    private static bool eaUploaderWindowOpened = false;

    static DependencyChecker()
    {
        if (!HasCheckedDependencies())
        {
            CheckDependencies();
            SaveCheckedDependencies();
        }
        
        try
        {
            EditorApplication.ExecuteMenuItem("EAUploader/MainWindow");
        }
        catch (Exception ex)
        {
            Debug.LogError($"An error occurred: {ex.Message}");
        }
        
    }

    private static bool HasCheckedDependencies()
    {
        if (File.Exists(packageCheckFilePath))
        {
            string json = File.ReadAllText(packageCheckFilePath);
            JObject packageCheck = JObject.Parse(json);
            string eaUploaderVersion = GetEAUploaderVersion();
            string shaderEditorVersion = GetShaderEditorVersion();

            return (packageCheck["eaUploaderVersion"].ToString() == eaUploaderVersion &&
                    packageCheck["shaderEditorVersion"].ToString() == shaderEditorVersion);
        }

        return false;
    }

    private static void SaveCheckedDependencies()
    {
        JObject packageCheck = new JObject
        {
            ["eaUploaderVersion"] = GetEAUploaderVersion(),
            ["shaderEditorVersion"] = GetShaderEditorVersion()
        };

        Directory.CreateDirectory(Path.GetDirectoryName(packageCheckFilePath));
        File.WriteAllText(packageCheckFilePath, packageCheck.ToString());
    }

    private static void CheckDependencies()
    {
        try
        {
            totalUpdatesNeeded = 0;
            updatesCompleted = 0;

            string eaUploaderVersion = GetEAUploaderVersion();
        
            AddUpdateIfNeeded("tech.uslog.eauploader", eaUploaderVersion, "https://github.com/Project-EAUploader/EAUploader-for-VRChat.git");
            string shaderEditorVersion = GetShaderEditorVersion();
            AddUpdateIfNeeded("tech.uslog.shadereditor-for-eauploader", shaderEditorVersion, "https://github.com/Project-EAUploader/Shader-Editor-for-EAUploader.git");
            // Checking if any updates were needed and completed
            if (updatesCompleted < totalUpdatesNeeded)
            {
                Debug.Log("Not all updates were completed.");
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions that might occur during the check
            Debug.LogError($"An error occurred while checking dependencies: {ex.Message}");
        }
        finally
        {
            FinalizeUpdates();
        }
    }

    private static void AddUpdateIfNeeded(string packageName, string version, string repoUrl)
    {
        if (!IsPackageInstalled(packageName, version))
        {
            totalUpdatesNeeded++;
            bool updateResult = EnsurePackageInstalled(packageName, version, repoUrl);
            if (updateResult)
            {
                updatesCompleted++;
                CheckForAllUpdatesCompleted();
            }
        }
    }

    private static void CheckForAllUpdatesCompleted()
    {
        if (updatesCompleted == totalUpdatesNeeded)
        {
            FinalizeUpdates();
        }
    }

    private static void FinalizeUpdates()
    {
        Debug.Log("FinalizeUpdates");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
        ClearConsole();
    }

    private static void OpenEAUploaderWindowOnce()
    {
        if (!eaUploaderWindowOpened)
        {
            // 既存のウィンドウを検索
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Where(window => window.GetType().Name == "EAUploader").ToList();

            Debug.Log($"EAUploader windows found: {windows.Count}");

            if (windows.Count == 0)
            {
                Debug.Log("Attempting to open EAUploader...");
                bool result = EditorApplication.ExecuteMenuItem("EAUploader/MainWindow");
                Debug.Log($"EAUploader opened: {result}");
            }
            else
            {
                Debug.Log("Focusing on existing EAUploader window.");
                windows[0].Focus();
            }

            eaUploaderWindowOpened = true;
        }
    }

    private static bool EnsurePackageInstalled(string packageName, string version, string repoUrl)
    {
        if (IsPackageInstalled(packageName, version))
        {
            return false;
        }

        string package = $"{packageName}@{repoUrl}#v{version}";
        return AddPackageSync(package);
    }

    private static bool IsPackageInstalled(string packageName, string version)
    {
        string manifestJsonPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
        if (!File.Exists(manifestJsonPath))
        {
            Debug.LogError("manifest.json not found.");
            return false;
        }

        string manifestJson = File.ReadAllText(manifestJsonPath);
        JObject manifest = JObject.Parse(manifestJson);

        if (manifest["dependencies"] != null)
        {
            JToken dependencies = manifest["dependencies"];

            if (dependencies[packageName] != null)
            {
                string installedVersion = dependencies[packageName].ToString();
                if (installedVersion.Contains($"@{version}"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool AddPackageSync(string package)
    {
        try
        {
            Debug.Log($"Installing package: {package}");
            AddRequest request = Client.Add(package);
            while (!request.IsCompleted)
            {
                // Busy wait
            }

            if (request.Status == StatusCode.Success)
            {
                Debug.Log("Package added successfully: " + request.Result.packageId);
                return true;
            }
            else
            {
                Debug.LogError($"Failed to add package: {request.Error.message}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Exception during package installation: {ex.Message}");
            return false;
        }
    }

    private static string GetEAUploaderVersion()
    {
        return GetPackageVersion("https://raw.githubusercontent.com/Project-EAUploader/EAUploader-for-VRChat/main/package.json");
    }

    private static string GetShaderEditorVersion()
    {
        return GetPackageVersion("https://raw.githubusercontent.com/Project-EAUploader/Shader-Editor-for-EAUploader/main/package.json");
    }

    private static string GetPackageVersion(string url)
    {
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        var operation = webRequest.SendWebRequest();
        while (!operation.isDone)
        {
            // Busy wait
        }

        if (webRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error: " + webRequest.error);
            return null;
        }
        else
        {
            JObject packageJson = JObject.Parse(webRequest.downloadHandler.text);
            return packageJson["version"].ToString();
        }
    }

    private static void ClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        OpenEAUploaderWindowOnce();
    }
}