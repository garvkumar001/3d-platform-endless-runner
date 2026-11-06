using UnityEngine;

/// <summary>Scene 2 - Stage Select: five world cards, wallet at the top, Back button.</summary>
public class StageSelectController : MonoBehaviour
{
    void Start()
    {
        Time.timeScale = 1f;
        AudioManager.Ensure();
        StageControls controls = gameObject.AddComponent<StageControls>();

        Camera cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.gameObject.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.13f, 0.25f);
        cam.gameObject.AddComponent<AudioListener>();
        RenderSettings.fog = false;

        Canvas canvas = UIKit.MakeCanvas("StageUI");
        Transform t = canvas.transform;
        Vector2 top = new Vector2(0.5f, 1f);
        Vector2 mid = new Vector2(0.5f, 0.5f);

        UIKit.Label(t, "SELECT WORLD", 100, UIKit.Gold, top, top, new Vector2(0f, -40f), new Vector2(1400f, 120f));
        UIKit.Label(t, "CHOOSE YOUR ADVENTURE", 40, Color.white, top, top, new Vector2(0f, -160f), new Vector2(1000f, 50f));

        UIKit.MakeButton(t, "BACK", UIKit.Blue, new Vector2(0f, 1f), new Vector2(150f, -70f), new Vector2(220f, 80f), controls.Back, 38);

        Vector2 tr = new Vector2(1f, 1f);
        UIKit.Dot(t, UIKit.Gold, tr, new Vector2(-250f, -70f), 34f);
        UIKit.Label(t, GameSession.Coins.ToString(), 38, Color.white, tr, new Vector2(0f, 0.5f), new Vector2(-225f, -70f), new Vector2(150f, 44f), TextAnchor.MiddleLeft);
        UIKit.Dot(t, new Color(0.95f, 0.2f, 0.45f), tr, new Vector2(-100f, -70f), 34f);
        UIKit.Label(t, GameSession.Gems.ToString(), 38, Color.white, tr, new Vector2(0f, 0.5f), new Vector2(-75f, -70f), new Vector2(100f, 44f), TextAnchor.MiddleLeft);

        for (int i = 0; i < WorldTheme.All.Length; i++)
        {
            WorldTheme th = WorldTheme.All[i];
            int index = i;
            Vector2 pos = new Vector2((i - 2) * 355f, -60f);
            UIKit.Panel(t, "Card" + i, new Color(0.06f, 0.08f, 0.16f, 0.95f), mid, mid, pos, new Vector2(330f, 660f));
            Transform card = t.Find("Card" + i);

            UIKit.Panel(card, "Preview", th.Sky, mid, mid, new Vector2(0f, 120f), new Vector2(290f, 360f));
            UIKit.Panel(card, "Ground", th.Side, mid, mid, new Vector2(0f, -20f), new Vector2(290f, 90f));
            UIKit.Panel(card, "Path", th.Ground, mid, mid, new Vector2(0f, -20f), new Vector2(90f, 90f));
            UIKit.Dot(card, th.Leaf, mid, new Vector2(-80f, 40f), 110f);
            UIKit.Dot(card, Factory.Shade(th.Leaf, 0.8f), mid, new Vector2(85f, 20f), 80f);
            UIKit.Dot(card, th.Accent, mid, new Vector2(70f, 200f), 50f);

            UIKit.Label(card, (i + 1) + ". " + WorldTheme.DisplayNames[i], 34, Color.white, mid, mid, new Vector2(0f, -110f), new Vector2(310f, 50f));
            UIKit.MakeButton(card, "SELECT", UIKit.Green, mid, new Vector2(0f, -230f), new Vector2(240f, 80f), () => controls.SelectWorld(index), 38);
        }
    }
}
