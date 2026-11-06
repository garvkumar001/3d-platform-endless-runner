using System.Text;
using UnityEngine;
using UnityEngine.UI;

public enum GameState { Playing, Paused, Dead }

/// <summary>
/// Gameplay scene controller. The scene only needs this one component: it builds the camera, light,
/// fog, player, endless track and the whole HUD / pause / game-over UI at runtime.
/// Handles lives, score, distance, difficulty, collecting, revive, restart.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public PlayerController Player { get; private set; }
    public float Distance { get; private set; }
    public float Score { get; private set; }
    public int Lives { get; private set; }
    public int RunCoins { get; private set; }
    public int RunGems { get; private set; }

    public float Difficulty { get { return Mathf.Clamp01(Distance / 1800f); } }
    public float BaseSpeed { get { return Mathf.Lerp(12f, 26f, Difficulty); } }

    WorldTheme theme;
    TrackGenerator track;
    CameraRig rig;
    bool revived;
    float startZ;
    float hudTimer;
    float flashAlpha;
    Color flashColor;

    // UI
    Text livesTxt, coinsTxt, gemsTxt, distTxt, scoreTxt, powerTxt;
    Image flashImg;
    GameObject pausePanel, overPanel;
    Text overScore, overBest, overRun;
    Button reviveBtn;
    Text reviveLabel;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        Application.targetFrameRate = 60;
        theme = WorldTheme.Current;
        AudioManager.Ensure();
        BuildWorld();
        BuildUI();
        Lives = 3;
        State = GameState.Playing;
    }

    void Start()
    {
        AudioManager.PlayMusic("Background_" + theme.Name);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------ world setup

    void BuildWorld()
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(theme.Sky, Color.white, 0.55f) * 0.9f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = theme.Fog;
        RenderSettings.fogStartDistance = 55f;
        RenderSettings.fogEndDistance = 160f;

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = theme.Fog;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 220f;
        cam.fieldOfView = 62f;
        camGo.AddComponent<AudioListener>();
        rig = camGo.AddComponent<CameraRig>();

        GameObject sun = new GameObject("Sun");
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        bool dark = theme.Name == "Mechanical";
        l.intensity = dark ? 0.9f : 1.15f;
        l.color = dark ? new Color(1f, 0.8f, 0.65f) : new Color(1f, 0.96f, 0.9f);
        l.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        Player = PlayerController.Create();
        startZ = Player.Z;
        track = gameObject.AddComponent<TrackGenerator>();
        track.Init(Player.transform, theme);
        rig.Target = Player;
    }

    // ------------------------------------------------------------------ update

    void Update()
    {
        if (InputAdapter.PausePressed)
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        if (State == GameState.Playing)
        {
            float newDist = Mathf.Max(Distance, Player.Z - startZ);
            Score += (newDist - Distance) * Player.Multiplier;
            Distance = newDist;
        }

        if (flashAlpha > 0f)
        {
            flashAlpha -= Time.unscaledDeltaTime * 2.5f;
            Color c = flashColor;
            c.a = Mathf.Max(0f, flashAlpha) * flashColor.a;
            flashImg.color = c;
        }

        hudTimer -= Time.unscaledDeltaTime;
        if (hudTimer <= 0f)
        {
            hudTimer = 0.08f;
            UpdateHud();
        }
    }

    void UpdateHud()
    {
        livesTxt.text = "x " + Lives;
        coinsTxt.text = GameSession.Coins.ToString();
        gemsTxt.text = GameSession.Gems.ToString();
        distTxt.text = Mathf.FloorToInt(Distance) + " m";
        scoreTxt.text = "SCORE  " + Mathf.FloorToInt(Score).ToString("D6");

        StringBuilder sb = new StringBuilder();
        PlayerController p = Player;
        if (p.Shield) sb.Append("SHIELD   ");
        if (p.MagnetT > 0f) sb.Append("MAGNET ").Append(Mathf.CeilToInt(p.MagnetT)).Append("s   ");
        if (p.SpeedT > 0f) sb.Append("SPEED ").Append(Mathf.CeilToInt(p.SpeedT)).Append("s   ");
        if (p.MultT > 0f) sb.Append("2X SCORE ").Append(Mathf.CeilToInt(p.MultT)).Append("s   ");
        if (p.JetT > 0f) sb.Append("JETPACK ").Append(Mathf.CeilToInt(p.JetT)).Append("s   ");
        if (p.InvT > 0f) sb.Append("INVINCIBLE ").Append(Mathf.CeilToInt(p.InvT)).Append("s");
        powerTxt.text = sb.ToString();
    }

    // ------------------------------------------------------------------ gameplay events

    public void Collect(PickupKind kind)
    {
        switch (kind)
        {
            case PickupKind.Coin:
                RunCoins++;
                GameSession.Coins++;
                Score += 10f * Player.Multiplier;
                AudioManager.Play("coin", 0.7f);
                break;
            case PickupKind.Gem:
                RunGems++;
                GameSession.Gems++;
                Score += 50f * Player.Multiplier;
                AudioManager.Play("gem");
                break;
            default:
                Player.Activate(kind);
                break;
        }
    }

    public void LoseLife()
    {
        Lives--;
        Flash(new Color(1f, 0.1f, 0.1f, 0.4f));
        rig.Shake(0.35f);
        if (Lives <= 0) GameOver();
    }

    public void OnPlayerFell()
    {
        if (State != GameState.Playing) return;
        Lives--;
        AudioManager.Play("hit");
        Flash(new Color(1f, 0.1f, 0.1f, 0.4f));
        if (Lives <= 0) GameOver();
        else Player.Respawn(track.NextSolidZ(Player.Z));
    }

    public void Flash(Color c)
    {
        flashColor = c;
        flashAlpha = 1f;
    }

    void GameOver()
    {
        State = GameState.Dead;
        int final = Mathf.FloorToInt(Score);
        if (final > GameSession.BestScore) GameSession.BestScore = final;
        GameSession.Save();
        AudioManager.Play("gameover");

        overScore.text = final.ToString("D6");
        overBest.text = "BEST  " + GameSession.BestScore.ToString("D6");
        overRun.text = "Coins +" + RunCoins + "    Gems +" + RunGems + "    " + Mathf.FloorToInt(Distance) + " m";
        reviveBtn.interactable = !revived && GameSession.Gems >= GameSession.ReviveCost;
        reviveLabel.text = revived ? "REVIVE USED" : "REVIVE  (" + GameSession.ReviveCost + " gems)";
        overPanel.SetActive(true);
    }

    // ------------------------------------------------------------------ buttons

    public void Revive()
    {
        if (revived || GameSession.Gems < GameSession.ReviveCost) return;
        GameSession.Gems -= GameSession.ReviveCost;
        revived = true;
        Lives = 1;
        overPanel.SetActive(false);
        Player.Respawn(track.NextSolidZ(Player.Z));
        Player.InvT = 3f;
        State = GameState.Playing;
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void Restart() { GameSession.Load(GameSession.GameplayScene); }
    public void Home() { GameSession.Load(GameSession.MainMenuScene); }

    // ------------------------------------------------------------------ UI

    Text HudRow(Transform t, int row, Color iconColor, string initial)
    {
        float y = -62f - row * 58f;
        Vector2 a = new Vector2(0f, 1f);
        UIKit.Dot(t, iconColor, a, new Vector2(62f, y), 38f);
        return UIKit.Label(t, initial, 40, Color.white, a, new Vector2(0f, 0.5f), new Vector2(96f, y), new Vector2(220f, 50f), TextAnchor.MiddleLeft);
    }

    void BuildUI()
    {
        Canvas canvas = UIKit.MakeCanvas("HUD", 10);
        Transform t = canvas.transform;
        flashImg = UIKit.FullImage(t, "Flash", new Color(1f, 0f, 0f, 0f));

        Vector2 tl = new Vector2(0f, 1f);
        UIKit.Panel(t, "HudBg", new Color(0f, 0f, 0f, 0.4f), tl, tl, new Vector2(24f, -24f), new Vector2(330f, 262f));
        livesTxt = HudRow(t, 0, new Color(1f, 0.25f, 0.3f), "x 3");
        coinsTxt = HudRow(t, 1, UIKit.Gold, "0");
        gemsTxt = HudRow(t, 2, new Color(0.95f, 0.2f, 0.45f), "0");
        distTxt = HudRow(t, 3, new Color(0.3f, 0.9f, 0.4f), "0 m");

        Vector2 tc = new Vector2(0.5f, 1f);
        scoreTxt = UIKit.Label(t, "SCORE  000000", 54, Color.white, tc, tc, new Vector2(0f, -28f), new Vector2(800f, 70f));
        powerTxt = UIKit.Label(t, "", 30, UIKit.Gold, tc, tc, new Vector2(0f, -100f), new Vector2(1200f, 40f));

        UIKit.MakeButton(t, "II", new Color(0.15f, 0.2f, 0.35f, 0.9f), new Vector2(1f, 1f), new Vector2(-70f, -70f), new Vector2(90f, 90f), Pause, 44);

        pausePanel = BuildPausePanel(t);
        overPanel = BuildOverPanel(t);
        pausePanel.SetActive(false);
        overPanel.SetActive(false);
    }

    GameObject BuildPausePanel(Transform parent)
    {
        RectTransform root = UIKit.Stretch(parent, "PausePanel");
        Image dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        Vector2 c = new Vector2(0.5f, 0.5f);
        UIKit.Panel(root, "Box", UIKit.Dark, c, c, Vector2.zero, new Vector2(760f, 820f));
        UIKit.Label(root, "PAUSED", 80, UIKit.Gold, c, c, new Vector2(0f, 330f), new Vector2(700f, 100f));
        UIKit.MakeButton(root, "RESUME", UIKit.Green, c, new Vector2(0f, 220f), new Vector2(460f, 90f), Resume);
        UIKit.MakeButton(root, "RESTART", UIKit.Blue, c, new Vector2(0f, 110f), new Vector2(460f, 90f), Restart);
        UIKit.MakeButton(root, "HOME", UIKit.Red, c, new Vector2(0f, 0f), new Vector2(460f, 90f), Home);
        UIKit.AudioSliders(root, c, new Vector2(0f, -150f), 460f, 90f);
        return root.gameObject;
    }

    GameObject BuildOverPanel(Transform parent)
    {
        RectTransform root = UIKit.Stretch(parent, "GameOverPanel");
        Image dim = root.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.7f);
        Vector2 c = new Vector2(0.5f, 0.5f);
        UIKit.Panel(root, "Box", UIKit.Dark, c, c, Vector2.zero, new Vector2(800f, 820f));
        UIKit.Label(root, "GAME OVER", 96, new Color(1f, 0.3f, 0.2f), c, c, new Vector2(0f, 320f), new Vector2(760f, 120f));
        overScore = UIKit.Label(root, "000000", 90, Color.white, c, c, new Vector2(0f, 190f), new Vector2(700f, 110f));
        overBest = UIKit.Label(root, "BEST  000000", 44, UIKit.Gold, c, c, new Vector2(0f, 110f), new Vector2(700f, 60f));
        overRun = UIKit.Label(root, "", 32, Color.white, c, c, new Vector2(0f, 50f), new Vector2(760f, 50f));
        reviveBtn = UIKit.MakeButton(root, "REVIVE", UIKit.Green, c, new Vector2(0f, -60f), new Vector2(560f, 100f), Revive, 40);
        reviveLabel = reviveBtn.GetComponentInChildren<Text>();
        UIKit.MakeButton(root, "HOME", UIKit.Blue, c, new Vector2(0f, -180f), new Vector2(560f, 100f), Home);
        UIKit.MakeButton(root, "RESTART", UIKit.Red, c, new Vector2(0f, -300f), new Vector2(560f, 100f), Restart);
        return root.gameObject;
    }
}
