using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene 1 - Main Menu: title, stats, CLICK TO START, Start Game, audio sliders, Quit.</summary>
public class MainMenuController : MonoBehaviour
{
    Text clickTxt;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.Ensure();
        AudioManager.PlayMusic("Background_Grassland");
        MenuBackdrop.Build(WorldTheme.All[0], true);

        Canvas canvas = UIKit.MakeCanvas("MenuUI");
        Transform t = canvas.transform;
        Vector2 top = new Vector2(0.5f, 1f);
        Vector2 mid = new Vector2(0.5f, 0.5f);

        UIKit.Label(t, "PLATFORM RUNNER", 130, UIKit.Gold, top, top, new Vector2(0f, -40f), new Vector2(1700f, 160f));
        UIKit.Label(t, "3D ENDLESS RUNNER", 52, Color.white, top, top, new Vector2(0f, -195f), new Vector2(1200f, 70f));

        Vector2 tl = new Vector2(0f, 1f);
        UIKit.Panel(t, "Stats", new Color(0f, 0f, 0f, 0.45f), tl, tl, new Vector2(30f, -30f), new Vector2(300f, 190f));
        UIKit.Dot(t, UIKit.Gold, tl, new Vector2(70f, -75f), 34f);
        UIKit.Label(t, GameSession.Coins.ToString(), 38, Color.white, tl, new Vector2(0f, 0.5f), new Vector2(105f, -75f), new Vector2(200f, 44f), TextAnchor.MiddleLeft);
        UIKit.Dot(t, new Color(0.95f, 0.2f, 0.45f), tl, new Vector2(70f, -128f), 34f);
        UIKit.Label(t, GameSession.Gems.ToString(), 38, Color.white, tl, new Vector2(0f, 0.5f), new Vector2(105f, -128f), new Vector2(200f, 44f), TextAnchor.MiddleLeft);
        UIKit.Label(t, "BEST " + GameSession.BestScore.ToString("D6"), 30, UIKit.Gold, tl, new Vector2(0f, 0.5f), new Vector2(55f, -176f), new Vector2(240f, 40f), TextAnchor.MiddleLeft);

        clickTxt = UIKit.Label(t, "CLICK TO START", 64, Color.white, mid, mid, new Vector2(0f, -300f), new Vector2(900f, 80f));
        UIKit.MakeButton(t, "START GAME", UIKit.Green, mid, new Vector2(0f, -410f), new Vector2(560f, 110f), StartGame, 54);

#if !UNITY_WEBGL
        UIKit.MakeButton(t, "QUIT", UIKit.Red, new Vector2(0f, 0f), new Vector2(150f, 70f), new Vector2(220f, 80f), Quit, 36);
#endif
        UIKit.AudioSliders(t, new Vector2(1f, 0f), new Vector2(-250f, 70f), 320f, 90f);
    }

    void Update()
    {
        if (clickTxt != null)
        {
            Color c = clickTxt.color;
            c.a = 0.55f + 0.45f * Mathf.Sin(Time.time * 3f);
            clickTxt.color = c;
        }
        if (InputAdapter.ConfirmPressed) StartGame();
    }

    public void StartGame()
    {
        AudioManager.Play("click");
        GameSession.Load(GameSession.StageSelectScene);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
