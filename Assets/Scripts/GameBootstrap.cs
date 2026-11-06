using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes the four scenes self-starting. Whatever the scene file contains, the correct controller
/// is attached by scene name (MainMenu, StageSelect, Information, Gameplay) when it loads.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;

        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        Setup(SceneManager.GetActiveScene());
    }

    static void OnLoaded(Scene scene, LoadSceneMode mode)
    {
        Setup(scene);
    }

    static void Setup(Scene scene)
    {
        switch (scene.name)
        {
            case "MainMenu": Ensure<MainMenuController>(); break;
            case "StageSelect": Ensure<StageSelectController>(); break;
            case "Information": Ensure<InfoController>(); break;
            case "Gameplay": Ensure<GameManager>(); break;
        }
    }

    static void Ensure<T>() where T : MonoBehaviour
    {
        if (Object.FindFirstObjectByType<T>() != null) return;
        new GameObject(typeof(T).Name).AddComponent<T>();
    }
}
