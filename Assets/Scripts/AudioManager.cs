using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent audio manager. Looks for real clips in Resources/Audio using the
/// names from the design deck (CoinCollect, GemCollect, HitObstacle, Jump, PowerUp,
/// ButtonClick, GameOver, Background_Grassland ...). If a clip is missing it
/// synthesises a simple placeholder tone, so the game always has sound.
/// </summary>
public class AudioManager : MonoBehaviour
{
    static AudioManager inst;

    AudioSource music;
    AudioSource sfx;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static float Master
    {
        get { return PlayerPrefs.GetFloat("pr_master", 1f); }
        set { PlayerPrefs.SetFloat("pr_master", value); Apply(); }
    }

    public static float MusicVolume
    {
        get { return PlayerPrefs.GetFloat("pr_music", 0.7f); }
        set { PlayerPrefs.SetFloat("pr_music", value); Apply(); }
    }

    public static float SfxVolume
    {
        get { return PlayerPrefs.GetFloat("pr_sfx", 0.8f); }
        set { PlayerPrefs.SetFloat("pr_sfx", value); Apply(); }
    }

    static void Apply()
    {
        if (inst != null) inst.music.volume = Master * MusicVolume * 0.8f;
    }

    public static void Ensure()
    {
        if (inst != null) return;
        GameObject go = new GameObject("AudioManager");
        inst = go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true;
        music.playOnAwake = false;
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        Apply();
    }

    public static void Play(string key, float volume = 1f)
    {
        Ensure();
        AudioClip c = inst.Get(key);
        if (c != null) inst.sfx.PlayOneShot(c, volume * Master * SfxVolume);
    }

    public static void PlayMusic(string resourceName)
    {
        Ensure();
        AudioClip c = Resources.Load<AudioClip>("Audio/" + resourceName);
        if (c == null)
        {
            inst.music.Stop();
            return;
        }
        if (inst.music.clip == c && inst.music.isPlaying) return;
        inst.music.clip = c;
        Apply();
        inst.music.Play();
    }

    static string FileFor(string key)
    {
        switch (key)
        {
            case "coin": return "CoinCollect";
            case "gem": return "GemCollect";
            case "hit": return "HitObstacle";
            case "jump": return "Jump";
            case "powerup": return "PowerUp";
            case "click": return "ButtonClick";
            case "gameover": return "GameOver";
            default: return key;
        }
    }

    AudioClip Get(string key)
    {
        AudioClip c;
        if (cache.TryGetValue(key, out c)) return c;
        c = Resources.Load<AudioClip>("Audio/" + FileFor(key));
        if (c == null) c = Synth(key);
        cache[key] = c;
        return c;
    }

    AudioClip Synth(string key)
    {
        switch (key)
        {
            case "coin": return Tone(new[] { 988f, 1319f }, 0.08f, false);
            case "gem": return Tone(new[] { 1319f, 1568f, 2093f }, 0.07f, false);
            case "jump": return Tone(new[] { 330f, 440f, 550f }, 0.05f, false);
            case "hit": return Tone(new[] { 180f, 120f, 80f }, 0.09f, true);
            case "powerup": return Tone(new[] { 523f, 659f, 784f, 1047f }, 0.07f, false);
            case "click": return Tone(new[] { 800f }, 0.04f, false);
            case "gameover": return Tone(new[] { 392f, 330f, 262f, 196f }, 0.18f, true);
            default: return null;
        }
    }

    static AudioClip Tone(float[] freqs, float segment, bool square)
    {
        const int rate = 44100;
        int segSamples = Mathf.RoundToInt(segment * rate);
        float[] data = new float[segSamples * freqs.Length];
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            int s = Mathf.Min(i / segSamples, freqs.Length - 1);
            float local = (i % segSamples) / (float)segSamples;
            phase += 2f * Mathf.PI * freqs[s] / rate;
            float w = Mathf.Sin(phase);
            if (square) w = w >= 0f ? 0.6f : -0.6f;
            float env = Mathf.Clamp01(1f - local) * Mathf.Clamp01(local * 40f + 0.2f);
            data[i] = w * env * 0.35f;
        }
        AudioClip clip = AudioClip.Create("tone", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
