using UnityEngine;

/// <summary>
/// Stage Select actions (named after the deck's StageControls.cs).
/// SelectWorld stores the chosen world, then PressPlay loads the Information scene,
/// which auto-loads the Gameplay scene (build index 3).
/// </summary>
public class StageControls : MonoBehaviour
{
    public void SelectWorld(int index)
    {
        GameSession.SelectedWorld = Mathf.Clamp(index, 0, WorldTheme.All.Length - 1);
        PressPlay();
    }

    public void PressPlay()
    {
        GameSession.Load(GameSession.InfoScene);
    }

    public void Back()
    {
        GameSession.Load(GameSession.MainMenuScene);
    }
}
