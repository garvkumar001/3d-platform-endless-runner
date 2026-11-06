using UnityEngine;

/// <summary>
/// Data for the five themed worlds. One generic track generator reads this,
/// replacing five copy-pasted segment scripts.
/// </summary>
public class WorldTheme
{
    public string Name;
    public Color Sky, Fog, Ground, Side, Accent, Crate, Leaf;
    public string[] Props;

    static Color C(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }

    public static readonly WorldTheme[] All = new WorldTheme[]
    {
        // Ground = dark track so lanes, coins and hazards stand out; Side = vivid scenery colour.
        new WorldTheme { Name = "Grassland", Sky = C(70, 165, 255), Fog = C(150, 215, 255),
            Ground = C(88, 62, 45), Side = C(45, 205, 70), Accent = C(255, 255, 255),
            Crate = C(240, 145, 35), Leaf = C(25, 190, 55),
            Props = new[] { "tree", "rock", "tree", "rock" } },

        new WorldTheme { Name = "Jungle", Sky = C(30, 195, 150), Fog = C(40, 160, 125),
            Ground = C(62, 42, 28), Side = C(0, 160, 65), Accent = C(255, 225, 90),
            Crate = C(215, 95, 40), Leaf = C(0, 175, 70),
            Props = new[] { "vine", "tree", "ruin", "rock" } },

        new WorldTheme { Name = "Desert", Sky = C(255, 150, 60), Fog = C(255, 200, 120),
            Ground = C(140, 85, 45), Side = C(255, 195, 70), Accent = C(255, 240, 180),
            Crate = C(205, 60, 45), Leaf = C(35, 175, 95),
            Props = new[] { "cactus", "rock", "ruin", "cactus" } },

        new WorldTheme { Name = "IceWorld", Sky = C(110, 195, 255), Fog = C(190, 230, 255),
            Ground = C(50, 90, 150), Side = C(225, 245, 255), Accent = C(120, 255, 255),
            Crate = C(80, 225, 255), Leaf = C(255, 255, 255),
            Props = new[] { "pine", "iceblock", "rock", "pine" } },

        new WorldTheme { Name = "Mechanical", Sky = C(55, 20, 100), Fog = C(65, 30, 110),
            Ground = C(34, 38, 54), Side = C(95, 45, 150), Accent = C(255, 175, 35),
            Crate = C(255, 110, 40), Leaf = C(255, 90, 200),
            Props = new[] { "gear", "pipe", "gear", "ruin" } },
    };

    public static string[] DisplayNames = { "GRASSLAND", "JUNGLE", "DESERT", "ICE WORLD", "MECHANICAL" };

    public static WorldTheme Current
    {
        get { return All[Mathf.Clamp(GameSession.SelectedWorld, 0, All.Length - 1)]; }
    }
}
