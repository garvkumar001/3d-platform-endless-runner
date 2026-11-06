using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Static game-wide state: scene indices (must match Build Settings order),
/// currently selected world, and persistent player wallet / best score.
/// </summary>
public static class GameSession
{
    // Build Settings order: 0 MainMenu, 1 StageSelect, 2 Information, 3 Gameplay
    public const int MainMenuScene = 0;
    public const int StageSelectScene = 1;
    public const int InfoScene = 2;
    public const int GameplayScene = 3;

    public const int ReviveCost = 10;

    public static int SelectedWorld;

    public static int Coins
    {
        get { return PlayerPrefs.GetInt("pr_coins", 0); }
        set { PlayerPrefs.SetInt("pr_coins", Mathf.Max(0, value)); }
    }

    public static int Gems
    {
        get { return PlayerPrefs.GetInt("pr_gems", 0); }
        set { PlayerPrefs.SetInt("pr_gems", Mathf.Max(0, value)); }
    }

    public static int BestScore
    {
        get { return PlayerPrefs.GetInt("pr_best", 0); }
        set { PlayerPrefs.SetInt("pr_best", Mathf.Max(0, value)); }
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static void Load(int sceneIndex)
    {
        Save();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }
}
