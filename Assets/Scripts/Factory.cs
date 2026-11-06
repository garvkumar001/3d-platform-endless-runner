using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds every in-game object. Each object first looks for a real prefab in
/// Resources/Prefabs/&lt;Name&gt; (this is where you drop your imported art). If none exists
/// it builds a colourful primitive placeholder, so the game is fully playable with zero assets.
/// </summary>
public static class Factory
{
    static Material template;
    static readonly Dictionary<int, Material> mats = new Dictionary<int, Material>();
    static readonly Dictionary<string, GameObject[]> propCache = new Dictionary<string, GameObject[]>();

    // ---------------------------------------------------------------- helpers

    public static Color Shade(Color c, float f)
    {
        return new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f), 1f);
    }

    public static Material Mat(Color c)
    {
        Color32 k = c;
        int key = (k.r << 24) | (k.g << 16) | (k.b << 8) | k.a;
        Material m;
        if (mats.TryGetValue(key, out m) && m != null) return m;
        if (template == null)
        {
            // Borrow the render-pipeline default material from a primitive (works in URP and in builds).
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            template = g.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(g);
        }
        m = new Material(template);
        m.color = c;
        mats[key] = m;
        return m;
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Color color,
        Vector3 euler = default(Vector3), bool keepCollider = false)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        if (!keepCollider)
        {
            Collider c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        Transform t = g.transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(euler);
        t.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = Mat(color);
        return g;
    }

    /// <summary>Makes every collider a trigger; if the object has none, fits one around its renderers.</summary>
    public static void EnsureTrigger(GameObject go, bool sphere, float scale)
    {
        bool any = false;
        foreach (Collider c in go.GetComponentsInChildren<Collider>())
        {
            MeshCollider mc = c as MeshCollider;
            if (mc != null && !mc.convex)
            {
                Object.DestroyImmediate(c);
                continue;
            }
            c.isTrigger = true;
            any = true;
        }
        if (any) return;

        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        Vector3 ls = go.transform.lossyScale;
        Vector3 center = go.transform.InverseTransformPoint(b.center);

        if (sphere)
        {
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.center = center;
            float maxScale = Mathf.Max(Mathf.Abs(ls.x), Mathf.Abs(ls.y), Mathf.Abs(ls.z));
            sc.radius = Mathf.Max(0.65f, b.extents.magnitude * 0.75f) / maxScale;
        }
        else
        {
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.center = center;
            bc.size = new Vector3(b.size.x / Mathf.Abs(ls.x), b.size.y / Mathf.Abs(ls.y), b.size.z / Mathf.Abs(ls.z)) * scale;
        }
    }

    static GameObject Spawn(string name, Transform parent, Vector3 pos, System.Func<GameObject> build)
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/" + name);
        GameObject go = prefab != null ? Object.Instantiate(prefab) : build();
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go;
    }

    // ---------------------------------------------------------------- public API

    public static GameObject CreatePickup(PickupKind kind, Transform parent, Vector3 pos)
    {
        string name = kind == PickupKind.Coin ? "Coin" : kind == PickupKind.Gem ? "Gem" : "PowerUp_" + PowerName(kind);
        GameObject go = Spawn(name, parent, pos, () => BuildPickup(kind));
        EnsureTrigger(go, true, 1f);
        Collectible c;
        if (!go.TryGetComponent(out c)) c = go.AddComponent<Collectible>();
        c.kind = kind;
        if (go.GetComponent<Spinner>() == null)
        {
            Spinner s = go.AddComponent<Spinner>();
            s.axis = Vector3.up;
            s.speed = kind == PickupKind.Coin || kind == PickupKind.Gem ? 160f : 90f;
        }
        return go;
    }

    public static GameObject CreateHazard(HazardKind kind, WorldTheme th, Transform parent, Vector3 pos)
    {
        string name;
        switch (kind)
        {
            case HazardKind.Spike: name = "SpikeTrap"; break;
            case HazardKind.Saw: name = "MovingSaw"; break;
            case HazardKind.Flyer: name = "FlyingEnemy"; break;
            case HazardKind.Robot: name = "EnemyBot"; break;
            default: name = kind.ToString(); break;
        }
        GameObject go = Spawn(name, parent, pos, () => BuildHazard(kind, th));
        EnsureTrigger(go, kind == HazardKind.Boulder, 0.9f);
        Hazard h;
        if (!go.TryGetComponent(out h)) h = go.AddComponent<Hazard>();
        h.kind = kind;
        h.moveSpeed = kind == HazardKind.Boulder ? 6f : kind == HazardKind.Robot ? 3.5f : 0f;
        return go;
    }

    public static GameObject CreateProp(string kind, WorldTheme th, Transform parent, Vector3 pos, float yaw)
    {
        GameObject[] custom = CustomProps(th);
        GameObject go = custom.Length > 0 ? Object.Instantiate(custom[Random.Range(0, custom.Length)]) : BuildProp(kind, th);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    static GameObject[] CustomProps(WorldTheme th)
    {
        GameObject[] a;
        if (!propCache.TryGetValue(th.Name, out a))
        {
            a = Resources.LoadAll<GameObject>("Worlds/" + th.Name + "/Props");
            propCache[th.Name] = a;
        }
        return a;
    }

    static string PowerName(PickupKind k)
    {
        switch (k)
        {
            case PickupKind.Magnet: return "Magnet";
            case PickupKind.Shield: return "Shield";
            case PickupKind.SpeedBoost: return "Speed";
            case PickupKind.ScoreX2: return "Multiplier";
            case PickupKind.Jetpack: return "Jetpack";
            default: return "Invincible";
        }
    }

    // ---------------------------------------------------------------- placeholders: player

    public static GameObject BuildPlayerVisual()
    {
        GameObject root = new GameObject("Visual");
        Transform t = root.transform;
        Color skin = new Color(0.94f, 0.75f, 0.58f);
        Color red = new Color(0.85f, 0.12f, 0.12f);
        Prim(PrimitiveType.Cube, t, new Vector3(0, 0.3f, 0), new Vector3(0.5f, 0.6f, 0.3f), new Color(0.2f, 0.32f, 0.8f));
        Prim(PrimitiveType.Cube, t, new Vector3(0, 0.9f, 0), new Vector3(0.65f, 0.65f, 0.35f), new Color(0.24f, 0.68f, 0.28f));
        Prim(PrimitiveType.Cube, t, new Vector3(-0.42f, 0.9f, 0), new Vector3(0.18f, 0.55f, 0.2f), skin);
        Prim(PrimitiveType.Cube, t, new Vector3(0.42f, 0.9f, 0), new Vector3(0.18f, 0.55f, 0.2f), skin);
        Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.45f, 0), Vector3.one * 0.5f, skin);
        Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.6f, 0), new Vector3(0.54f, 0.3f, 0.54f), red);
        Prim(PrimitiveType.Cube, t, new Vector3(0, 1.52f, 0.25f), new Vector3(0.4f, 0.06f, 0.3f), red);
        return root;
    }

    // ---------------------------------------------------------------- placeholders: pickups

    static GameObject BuildPickup(PickupKind kind)
    {
        GameObject root = new GameObject("Pickup");
        Transform t = root.transform;
        switch (kind)
        {
            case PickupKind.Coin:
                Prim(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.7f, 0.06f, 0.7f), new Color(1f, 0.78f, 0.15f), new Vector3(90, 0, 0));
                Prim(PrimitiveType.Cylinder, t, Vector3.zero, new Vector3(0.5f, 0.08f, 0.5f), new Color(1f, 0.9f, 0.4f), new Vector3(90, 0, 0));
                break;
            case PickupKind.Gem:
                Prim(PrimitiveType.Cube, t, Vector3.zero, Vector3.one * 0.5f, new Color(0.92f, 0.1f, 0.3f), new Vector3(45, 0, 45));
                break;
            default:
                Color c = PowerColor(kind);
                Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 0.9f, c);
                Color w = Color.white;
                Vector3 front = new Vector3(0, 0, -0.4f);
                switch (kind)
                {
                    case PickupKind.Magnet:
                        Prim(PrimitiveType.Cube, t, front + new Vector3(0, 0.15f, 0), new Vector3(0.5f, 0.14f, 0.1f), w);
                        Prim(PrimitiveType.Cube, t, front + new Vector3(-0.18f, -0.05f, 0), new Vector3(0.14f, 0.35f, 0.1f), w);
                        Prim(PrimitiveType.Cube, t, front + new Vector3(0.18f, -0.05f, 0), new Vector3(0.14f, 0.35f, 0.1f), w);
                        break;
                    case PickupKind.Shield:
                        Prim(PrimitiveType.Cube, t, front, new Vector3(0.38f, 0.48f, 0.1f), w);
                        break;
                    case PickupKind.SpeedBoost:
                        Prim(PrimitiveType.Cube, t, front + new Vector3(0, 0.12f, 0), new Vector3(0.45f, 0.12f, 0.1f), w, new Vector3(0, 0, 30));
                        Prim(PrimitiveType.Cube, t, front + new Vector3(0, -0.12f, 0), new Vector3(0.45f, 0.12f, 0.1f), w, new Vector3(0, 0, -30));
                        break;
                    case PickupKind.ScoreX2:
                        Prim(PrimitiveType.Cube, t, front, new Vector3(0.55f, 0.12f, 0.1f), w, new Vector3(0, 0, 45));
                        Prim(PrimitiveType.Cube, t, front, new Vector3(0.55f, 0.12f, 0.1f), w, new Vector3(0, 0, -45));
                        break;
                    case PickupKind.Jetpack:
                        Prim(PrimitiveType.Capsule, t, front, new Vector3(0.28f, 0.28f, 0.2f), new Color(0.85f, 0.15f, 0.15f));
                        break;
                    default:
                        Prim(PrimitiveType.Cube, t, front, new Vector3(0.4f, 0.4f, 0.1f), w, new Vector3(0, 0, 45));
                        break;
                }
                break;
        }
        return root;
    }

    public static Color PowerColor(PickupKind k)
    {
        switch (k)
        {
            case PickupKind.Magnet: return new Color(0.9f, 0.2f, 0.2f);
            case PickupKind.Shield: return new Color(0.27f, 0.5f, 1f);
            case PickupKind.SpeedBoost: return new Color(1f, 0.58f, 0.08f);
            case PickupKind.ScoreX2: return new Color(1f, 0.84f, 0f);
            case PickupKind.Jetpack: return new Color(0.92f, 0.92f, 0.95f);
            default: return new Color(0.78f, 0.31f, 1f);
        }
    }

    // ---------------------------------------------------------------- placeholders: hazards

    static GameObject BuildHazard(HazardKind kind, WorldTheme th)
    {
        GameObject root = new GameObject("Hazard");
        Transform t = root.transform;
        switch (kind)
        {
            case HazardKind.Crate:
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), Vector3.one * 1.2f, th.Crate);
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.6f, 0), new Vector3(1.24f, 0.14f, 1.24f), Shade(th.Crate, 0.6f));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.12f, 0), new Vector3(1.24f, 0.14f, 1.24f), Shade(th.Crate, 0.6f));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 1.08f, 0), new Vector3(1.24f, 0.14f, 1.24f), Shade(th.Crate, 0.6f));
                break;
            case HazardKind.Spike:
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.06f, 0), new Vector3(1.6f, 0.12f, 1.2f), new Color(0.25f, 0.25f, 0.28f));
                for (int ix = -1; ix <= 1; ix++)
                    for (int iz = -1; iz <= 1; iz++)
                        Prim(PrimitiveType.Cube, t, new Vector3(ix * 0.5f, 0.4f, iz * 0.35f), new Vector3(0.16f, 0.55f, 0.16f),
                            new Color(0.8f, 0.82f, 0.86f), new Vector3(0, 45, 0));
                break;
            case HazardKind.Boulder:
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.65f, 0), Vector3.one * 1.3f, new Color(0.47f, 0.4f, 0.36f));
                Prim(PrimitiveType.Sphere, t, new Vector3(0.4f, 0.95f, -0.3f), Vector3.one * 0.5f, new Color(0.36f, 0.3f, 0.27f));
                Prim(PrimitiveType.Sphere, t, new Vector3(-0.4f, 0.5f, -0.4f), Vector3.one * 0.4f, new Color(0.55f, 0.48f, 0.43f));
                break;
            case HazardKind.Saw:
            {
                GameObject disk = Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.55f, 0), new Vector3(1.1f, 0.05f, 1.1f),
                    new Color(0.78f, 0.82f, 0.86f), new Vector3(90, 0, 0));
                Spinner sp = disk.AddComponent<Spinner>();
                sp.axis = Vector3.up;
                sp.speed = 540f;
                for (int i = 0; i < 8; i++)
                {
                    // teeth sit on the rim of the disk (disk-local XZ plane)
                    Prim(PrimitiveType.Cube, disk.transform,
                        new Vector3(Mathf.Sin(i * 0.785f) * 0.5f, 0f, Mathf.Cos(i * 0.785f) * 0.5f),
                        new Vector3(0.12f, 1f, 0.12f), new Color(0.6f, 0.62f, 0.66f), new Vector3(0, i * 45f, 0));
                }
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.55f, -0.05f), Vector3.one * 0.25f, new Color(0.85f, 0.15f, 0.15f));
                break;
            }
            case HazardKind.Flyer:
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.4f, 0), Vector3.one * 0.7f, new Color(0.38f, 0.15f, 0.5f));
                Prim(PrimitiveType.Cube, t, new Vector3(-0.55f, 1.5f, 0), new Vector3(0.7f, 0.05f, 0.35f), new Color(0.2f, 0.1f, 0.28f), new Vector3(0, 0, 20));
                Prim(PrimitiveType.Cube, t, new Vector3(0.55f, 1.5f, 0), new Vector3(0.7f, 0.05f, 0.35f), new Color(0.2f, 0.1f, 0.28f), new Vector3(0, 0, -20));
                Prim(PrimitiveType.Sphere, t, new Vector3(-0.15f, 1.5f, -0.3f), Vector3.one * 0.14f, Color.white);
                Prim(PrimitiveType.Sphere, t, new Vector3(0.15f, 1.5f, -0.3f), Vector3.one * 0.14f, Color.white);
                break;
            case HazardKind.Robot:
                Prim(PrimitiveType.Cube, t, new Vector3(-0.2f, 0.25f, 0), new Vector3(0.22f, 0.5f, 0.3f), new Color(0.25f, 0.28f, 0.34f));
                Prim(PrimitiveType.Cube, t, new Vector3(0.2f, 0.25f, 0), new Vector3(0.22f, 0.5f, 0.3f), new Color(0.25f, 0.28f, 0.34f));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.75f, 0), new Vector3(0.8f, 0.55f, 0.5f), new Color(0.35f, 0.45f, 0.58f));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 1.2f, 0), new Vector3(0.5f, 0.35f, 0.4f), new Color(0.5f, 0.55f, 0.62f));
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 1.22f, -0.2f), Vector3.one * 0.14f, new Color(1f, 0.1f, 0.1f));
                break;
        }
        return root;
    }

    // ---------------------------------------------------------------- placeholders: environment props

    static void AddTree(Transform t, WorldTheme th, float s)
    {
        Color wood = new Color(0.4f, 0.27f, 0.15f);
        Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.2f * s, 0), new Vector3(0.45f * s, 1.2f * s, 0.45f * s), wood);
        Prim(PrimitiveType.Sphere, t, new Vector3(0, 3.2f * s, 0), Vector3.one * 2.6f * s, th.Leaf);
        Prim(PrimitiveType.Sphere, t, new Vector3(0.6f * s, 2.7f * s, 0.3f * s), Vector3.one * 1.6f * s, Shade(th.Leaf, 0.85f));
    }

    static GameObject BuildProp(string kind, WorldTheme th)
    {
        GameObject root = new GameObject("Prop_" + kind);
        Transform t = root.transform;
        float s = Random.Range(0.8f, 1.4f);
        Color grey = Color.Lerp(new Color(0.5f, 0.5f, 0.52f), th.Side, 0.25f);
        switch (kind)
        {
            case "tree":
                AddTree(t, th, s);
                break;
            case "vine":
                AddTree(t, th, s);
                Prim(PrimitiveType.Cylinder, t, new Vector3(0.9f * s, 2.6f * s, 0), new Vector3(0.1f, 1.4f * s, 0.1f), Shade(th.Leaf, 0.7f));
                Prim(PrimitiveType.Cylinder, t, new Vector3(-0.9f * s, 2.4f * s, 0.3f), new Vector3(0.1f, 1.6f * s, 0.1f), Shade(th.Leaf, 0.7f));
                break;
            case "pine":
                Prim(PrimitiveType.Cylinder, t, new Vector3(0, 0.8f * s, 0), new Vector3(0.4f * s, 0.8f * s, 0.4f * s), new Color(0.35f, 0.25f, 0.18f));
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 2.0f * s, 0), new Vector3(2.2f, 1.6f, 2.2f) * s, new Color(0.12f, 0.38f, 0.3f));
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 3.1f * s, 0), new Vector3(1.6f, 1.4f, 1.6f) * s, new Color(0.15f, 0.45f, 0.34f));
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 4.0f * s, 0), new Vector3(1.0f, 1.1f, 1.0f) * s, th.Leaf);
                break;
            case "rock":
                Prim(PrimitiveType.Sphere, t, new Vector3(0, 0.4f * s, 0), new Vector3(1.8f, 1.1f, 1.5f) * s, grey);
                Prim(PrimitiveType.Sphere, t, new Vector3(0.9f * s, 0.25f * s, 0.4f), new Vector3(0.9f, 0.6f, 0.8f) * s, Shade(grey, 0.85f));
                break;
            case "cactus":
                Prim(PrimitiveType.Cylinder, t, new Vector3(0, 1.5f * s, 0), new Vector3(0.5f * s, 1.5f * s, 0.5f * s), th.Leaf);
                Prim(PrimitiveType.Cylinder, t, new Vector3(0.45f * s, 1.4f * s, 0), new Vector3(0.28f * s, 0.35f * s, 0.28f * s), th.Leaf, new Vector3(0, 0, 90));
                Prim(PrimitiveType.Cylinder, t, new Vector3(0.8f * s, 1.9f * s, 0), new Vector3(0.28f * s, 0.6f * s, 0.28f * s), th.Leaf);
                break;
            case "gear":
            {
                GameObject holder = new GameObject("GearHolder");
                holder.transform.SetParent(t, false);
                holder.transform.localPosition = new Vector3(0, 2.2f * s, 0);
                holder.transform.localRotation = Quaternion.Euler(0, 0, 90);
                Spinner sp = holder.AddComponent<Spinner>();
                sp.axis = Vector3.up;
                sp.speed = Random.value < 0.5f ? 25f : -25f;
                Prim(PrimitiveType.Cylinder, holder.transform, Vector3.zero, new Vector3(3f * s, 0.4f, 3f * s), th.Crate);
                Prim(PrimitiveType.Cylinder, holder.transform, Vector3.zero, new Vector3(0.8f * s, 0.6f, 0.8f * s), th.Accent);
                for (int i = 0; i < 10; i++)
                {
                    float a = i * Mathf.PI * 2f / 10f;
                    Prim(PrimitiveType.Cube, holder.transform, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.55f * s,
                        new Vector3(0.45f, 0.4f, 0.45f) * s, th.Crate, new Vector3(0, -a * Mathf.Rad2Deg, 0));
                }
                break;
            }
            case "pipe":
                Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2.5f * s, 0), new Vector3(0.7f, 2.5f * s, 0.7f) , th.Accent);
                Prim(PrimitiveType.Cylinder, t, new Vector3(0, 5.1f * s, 0), new Vector3(0.95f, 0.15f, 0.95f), Shade(th.Accent, 0.6f));
                Prim(PrimitiveType.Cylinder, t, new Vector3(0, 2.0f * s, 0), new Vector3(0.9f, 0.12f, 0.9f), Shade(th.Accent, 0.6f));
                break;
            case "iceblock":
                Prim(PrimitiveType.Cube, t, new Vector3(0, 0.9f * s, 0), Vector3.one * 1.8f * s, th.Crate, new Vector3(0, 25, 0));
                Prim(PrimitiveType.Cube, t, new Vector3(1.3f * s, 0.5f * s, 0.4f), Vector3.one * 1.0f * s, Shade(th.Crate, 1.1f), new Vector3(0, 50, 0));
                break;
            case "ruin":
                Prim(PrimitiveType.Cube, t, new Vector3(0, 1.6f * s, 0), new Vector3(1.1f, 3.2f * s, 1.1f), new Color(0.62f, 0.58f, 0.5f));
                Prim(PrimitiveType.Cube, t, new Vector3(0, 3.3f * s, 0), new Vector3(1.5f, 0.4f, 1.5f), new Color(0.55f, 0.5f, 0.43f));
                Prim(PrimitiveType.Cube, t, new Vector3(1.6f * s, 0.9f, 0.3f), new Vector3(1.0f, 1.8f, 1.0f), new Color(0.58f, 0.54f, 0.47f), new Vector3(0, 20, 0));
                break;
        }
        return root;
    }
}
