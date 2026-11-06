using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene 3 - Information: shows controls, a GET READY bar, then loads Gameplay (build index 3).</summary>
public class InfoController : MonoBehaviour
{
    const float Duration = 4.5f;
    float timer;
    Image bar;
    bool loading;

    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.Ensure();
        WorldTheme th = WorldTheme.Current;
        AudioManager.PlayMusic("Background_" + th.Name);
        MenuBackdrop.Build(th, false);

        Canvas canvas = UIKit.MakeCanvas("InfoUI");
        Transform t = canvas.transform;
        Vector2 top = new Vector2(0.5f, 1f);
        Vector2 mid = new Vector2(0.5f, 0.5f);
        Vector2 bot = new Vector2(0.5f, 0f);

        UIKit.Label(t, "HOW TO PLAY", 100, UIKit.Gold, top, top, new Vector2(0f, -40f), new Vector2(1400f, 120f));
        UIKit.Label(t, WorldTheme.DisplayNames[Mathf.Clamp(GameSession.SelectedWorld, 0, 4)], 48, Color.white, top, top, new Vector2(0f, -160f), new Vector2(1000f, 60f));

        KeyBox(t, "MOVE LEFT", "A  /  LEFT ARROW", new Vector2(-380f, 160f));
        KeyBox(t, "MOVE RIGHT", "D  /  RIGHT ARROW", new Vector2(380f, 160f));
        KeyBox(t, "JUMP", "SPACE  /  W  /  UP", new Vector2(-380f, -20f));
        KeyBox(t, "SLIDE", "S  /  DOWN ARROW", new Vector2(380f, -20f));

        UIKit.Panel(t, "Tips", new Color(0f, 0f, 0f, 0.55f), mid, mid, new Vector2(0f, -200f), new Vector2(1180f, 130f));
        UIKit.Label(t, "Collect coins & gems.  Grab power-ups.  Jump gaps, dodge or slide under hazards.", 32, Color.white, mid, mid, new Vector2(0f, -170f), new Vector2(1150f, 50f));
        UIKit.Label(t, "Touch: swipe left / right / up / down.", 32, UIKit.Gold, mid, mid, new Vector2(0f, -225f), new Vector2(1150f, 50f));

        UIKit.Label(t, "GET READY...", 56, Color.white, bot, bot, new Vector2(0f, 190f), new Vector2(800f, 70f));
        bar = UIKit.FillBar(t, bot, new Vector2(0f, 130f), new Vector2(700f, 40f), UIKit.Green);
        UIKit.Label(t, "Press SPACE to skip", 28, new Color(1f, 1f, 1f, 0.7f), bot, bot, new Vector2(0f, 60f), new Vector2(600f, 40f));
    }

    void KeyBox(Transform t, string title, string keys, Vector2 pos)
    {
        Vector2 mid = new Vector2(0.5f, 0.5f);
        UIKit.Panel(t, title, new Color(0.08f, 0.1f, 0.2f, 0.9f), mid, mid, pos, new Vector2(620f, 150f));
        UIKit.Label(t, title, 40, UIKit.Gold, mid, mid, pos + new Vector2(0f, 38f), new Vector2(600f, 50f));
        UIKit.Label(t, keys, 44, Color.white, mid, mid, pos + new Vector2(0f, -22f), new Vector2(600f, 60f));
    }

    void Update()
    {
        if (loading) return;
        timer += Time.deltaTime;
        if (bar != null) bar.fillAmount = Mathf.Clamp01(timer / Duration);
        if (timer >= Duration || InputAdapter.ConfirmPressed)
        {
            loading = true;
            GameSession.Load(GameSession.GameplayScene);
        }
    }
}
