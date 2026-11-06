#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatic first-open setup. When the project loads and the four scenes are missing, this creates them
/// (MainMenu, StageSelect, Information, Gameplay), registers them in Build Settings in the right order and
/// opens MainMenu. Everything is also available under the "Platform Runner" menu.
/// </summary>
[InitializeOnLoad]
public static class PlatformRunnerSetup
{
    static readonly string[] SceneNames = { "MainMenu", "StageSelect", "Information", "Gameplay" };

    static readonly string[] Folders =
    {
        "Assets/Scenes", "Assets/Prefabs", "Assets/Materials", "Assets/Animations", "Assets/UI", "Assets/Audio",
        "Assets/Models/Player", "Assets/Models/Environment", "Assets/Models/Collectibles", "Assets/Models/Obstacles",
        "Assets/Textures/Player", "Assets/Textures/Environment",
        "Assets/Resources/Prefabs", "Assets/Resources/Audio",
        "Assets/Resources/Worlds/Grassland/Props", "Assets/Resources/Worlds/Jungle/Props",
        "Assets/Resources/Worlds/Desert/Props", "Assets/Resources/Worlds/IceWorld/Props",
        "Assets/Resources/Worlds/Mechanical/Props",
    };

    static PlatformRunnerSetup()
    {
        EditorApplication.update += Poll;
    }

    static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.update -= Poll;

        if (!AllScenesExist())
        {
            if (SceneManager.GetActiveScene().isDirty) return; // never discard the user's unsaved work
            CreateFolders();
            CreateScenes();
            Debug.Log("Platform Runner 3D: scenes created and registered in Build Settings. Press Play!");
        }
        else
        {
            RegisterBuildSettings();
        }
    }

    static bool AllScenesExist()
    {
        foreach (string n in SceneNames)
            if (!File.Exists("Assets/Scenes/" + n + ".unity")) return false;
        return true;
    }

    [MenuItem("Platform Runner/Build Everything (Folders + Scenes)")]
    public static void BuildEverything()
    {
        CreateFolders();
        CreateScenes();
        EditorUtility.DisplayDialog("Platform Runner",
            "Done! Folders created, 4 scenes saved to Assets/Scenes and registered in Build Settings.\n\nPress Play in the MainMenu scene.", "OK");
    }

    [MenuItem("Platform Runner/1. Create Folders")]
    public static void CreateFolders()
    {
        foreach (string f in Folders)
            if (!Directory.Exists(f)) Directory.CreateDirectory(f);
        AssetDatabase.Refresh();
    }

    [MenuItem("Platform Runner/2. Create Scenes + Build Settings")]
    public static void CreateScenes()
    {
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

        MakeScene("MainMenu", go => go.AddComponent<MainMenuController>());
        MakeScene("StageSelect", go => go.AddComponent<StageSelectController>());
        MakeScene("Information", go => go.AddComponent<InfoController>());
        MakeScene("Gameplay", go => go.AddComponent<GameManager>());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        RegisterBuildSettings();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    static void RegisterBuildSettings()
    {
        EditorBuildSettingsScene[] list = new EditorBuildSettingsScene[SceneNames.Length];
        for (int i = 0; i < SceneNames.Length; i++)
            list[i] = new EditorBuildSettingsScene("Assets/Scenes/" + SceneNames[i] + ".unity", true);
        EditorBuildSettings.scenes = list;
    }

    [MenuItem("Platform Runner/Open Main Menu Scene")]
    public static void OpenMainMenu()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    static void MakeScene(string name, System.Action<GameObject> setup)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject go = new GameObject(name + "Controller");
        setup(go);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + name + ".unity");
    }
}
#endif
