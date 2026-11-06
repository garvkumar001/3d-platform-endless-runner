using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Endless, themed level generator (replaces the five per-world segment scripts of the design deck).
/// Each 40 m segment: ground with optional gaps, lane lines, side terrain, props, coin trails,
/// gems, power-ups and hazards. Difficulty scales with distance. Segments behind the player are destroyed.
/// </summary>
public class TrackGenerator : MonoBehaviour
{
    public const float CellLen = 2f;
    public const int Cells = 20;
    public const float SegLen = CellLen * Cells;   // 40 m
    public const float TrackHalf = 3.5f;
    const float Ahead = 170f;

    Transform player;
    WorldTheme th;
    int next;
    int coinLane = 1, coinStreak;
    Material groundMat, sideMat;

    readonly List<GameObject> segs = new List<GameObject>();
    readonly List<float> segEnd = new List<float>();
    readonly List<Vector2> solid = new List<Vector2>();   // z-intervals that have floor

    public void Init(Transform p, WorldTheme theme)
    {
        player = p;
        th = theme;
        groundMat = Resources.Load<Material>("Worlds/" + th.Name + "/Ground");
        sideMat = Resources.Load<Material>("Worlds/" + th.Name + "/Side");
        while (next * SegLen < player.position.z + Ahead) Build(next++);
    }

    void Update()
    {
        if (player == null) return;
        float pz = player.position.z;
        while (next * SegLen < pz + Ahead) Build(next++);
        while (segs.Count > 0 && segEnd[0] < pz - 60f)
        {
            Destroy(segs[0]);
            segs.RemoveAt(0);
            segEnd.RemoveAt(0);
        }
        while (solid.Count > 1 && solid[0].y < pz - 60f) solid.RemoveAt(0);
    }

    /// <summary>Returns a z position with safe floor ahead of fromZ (used for respawn / revive).</summary>
    public float NextSolidZ(float fromZ)
    {
        for (int i = 0; i < solid.Count; i++)
        {
            float s = Mathf.Max(solid[i].x, fromZ + 2f);
            if (solid[i].y - s >= 10f) return s + 3f;
        }
        return fromZ + 10f;
    }

    // ------------------------------------------------------------------ building

    void AddSolid(float a, float b)
    {
        if (solid.Count > 0)
        {
            Vector2 l = solid[solid.Count - 1];
            if (Mathf.Abs(l.y - a) < 0.01f) { solid[solid.Count - 1] = new Vector2(l.x, b); return; }
        }
        solid.Add(new Vector2(a, b));
    }

    void AddGround(Transform root, float z1, float z2)
    {
        float len = z2 - z1, zc = (z1 + z2) * 0.5f;
        GameObject g = Factory.Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.5f, zc),
            new Vector3(TrackHalf * 2f, 1f, len), th.Ground, default(Vector3), true);
        g.AddComponent<GroundTile>();
        if (groundMat != null) g.GetComponent<Renderer>().sharedMaterial = groundMat;
        // lane dividers
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(-1f, 0.011f, zc), new Vector3(0.1f, 0.02f, len), th.Accent);
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(1f, 0.011f, zc), new Vector3(0.1f, 0.02f, len), th.Accent);
        // bright edge rails frame the playable lanes
        Color rail = Color.Lerp(th.Accent, th.Side, 0.35f);
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(-TrackHalf + 0.12f, 0.06f, zc), new Vector3(0.24f, 0.12f, len), rail);
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(TrackHalf - 0.12f, 0.06f, zc), new Vector3(0.24f, 0.12f, len), rail);
        AddSolid(z1, z2);
    }

    void AddSide(Transform root, float z1, float z2)
    {
        float len = z2 - z1, zc = (z1 + z2) * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject g = Factory.Prim(PrimitiveType.Cube, root, new Vector3(s * (TrackHalf + 5f), -2f, zc),
                new Vector3(10f, 1f, len), th.Side);
            if (sideMat != null) g.GetComponent<Renderer>().sharedMaterial = sideMat;
        }
    }

    void Build(int i)
    {
        GameObject rootGo = new GameObject("Segment_" + i);
        rootGo.transform.SetParent(transform, false);
        Transform root = rootGo.transform;
        float z0 = i * SegLen;
        float diff = GameManager.Instance != null ? GameManager.Instance.Difficulty : 0f;

        if (i == 0)
        {
            AddGround(root, -20f, 0f);
            AddSide(root, -20f, 0f);
        }
        AddSide(root, z0, z0 + SegLen);

        // ---- which cells have floor
        bool[] s = new bool[Cells];
        for (int c = 0; c < Cells; c++) s[c] = true;
        if (i >= 2)
        {
            float gapChance = Mathf.Lerp(0.05f, 0.16f, diff);
            int maxGap = diff > 0.5f ? 3 : 2;
            int since = 3;
            for (int c = 3; c < Cells - 3; c++)
            {
                since++;
                if (since >= 4 && Random.value < gapChance)
                {
                    int len = Mathf.Min(Random.Range(1, maxGap + 1), Cells - 3 - c);
                    for (int k = 0; k < len; k++) s[c + k] = false;
                    // coin arc over the gap
                    if (Random.value < 0.6f)
                    {
                        float gz = z0 + (c + len * 0.5f) * CellLen;
                        float lx = PlayerController.Lanes[Random.Range(0, 3)];
                        Factory.CreatePickup(PickupKind.Coin, root, new Vector3(lx, 1.5f, gz - len));
                        Factory.CreatePickup(PickupKind.Coin, root, new Vector3(lx, 2.2f, gz));
                        Factory.CreatePickup(PickupKind.Coin, root, new Vector3(lx, 1.5f, gz + len));
                    }
                    c += len;
                    since = 0;
                }
            }
        }

        // ---- ground runs
        int cc = 0;
        while (cc < Cells)
        {
            if (!s[cc]) { cc++; continue; }
            int st = cc;
            while (cc < Cells && s[cc]) cc++;
            AddGround(root, z0 + st * CellLen, z0 + cc * CellLen);
        }

        // ---- props along both sides
        int propCount = Random.Range(5, 9);
        for (int p = 0; p < propCount; p++)
        {
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 pos = new Vector3(side * Random.Range(TrackHalf + 1.5f, TrackHalf + 9f), -1.5f, z0 + Random.value * SegLen);
            string kind = th.Props[Random.Range(0, th.Props.Length)];
            Factory.CreateProp(kind, th, root, pos, Random.Range(0f, 360f));
        }

        // ---- rows of hazards / coins / gems / power-ups
        int cooldown = 0;
        float hazardChance = Mathf.Lerp(0.2f, 0.55f, diff);
        for (int r = 2; r < Cells - 2; r += 2)
        {
            float z = z0 + r * CellLen + 1f;
            bool ok = s[r] && s[r - 1] && s[r + 1];
            bool[] blocked = new bool[3];

            if (i >= 1 && ok && cooldown <= 0 && Random.value < hazardChance)
            {
                PlaceHazard(root, z, blocked, diff);
                cooldown = 1;
            }
            else cooldown--;

            if (!s[r]) continue;

            // coin trail
            if (coinStreak <= 0 && Random.value < 0.3f)
            {
                coinStreak = Random.Range(3, 8);
                coinLane = Random.Range(0, 3);
            }
            if (coinStreak > 0)
            {
                coinStreak--;
                if (blocked[coinLane])
                {
                    for (int l = 0; l < 3; l++) if (!blocked[l]) { coinLane = l; break; }
                }
                if (!blocked[coinLane])
                {
                    float cx = PlayerController.Lanes[coinLane];
                    Factory.CreatePickup(PickupKind.Coin, root, new Vector3(cx, 1f, z - 1f));
                    Factory.CreatePickup(PickupKind.Coin, root, new Vector3(cx, 1f, z + 1f));
                }
            }

            // gem / power-up on a free lane
            if (ok)
            {
                float roll = Random.value;
                int freeLane = -1;
                for (int l = 0; l < 3; l++) if (!blocked[l] && l != coinLane) { freeLane = l; break; }
                if (freeLane >= 0)
                {
                    Vector3 pos = new Vector3(PlayerController.Lanes[freeLane], 1f, z);
                    if (roll < 0.05f) Factory.CreatePickup(PickupKind.Gem, root, pos);
                    else if (roll < 0.075f && i >= 1)
                        Factory.CreatePickup((PickupKind)Random.Range((int)PickupKind.Magnet, (int)PickupKind.Invincible + 1), root, pos);
                }
            }
        }

        segs.Add(rootGo);
        segEnd.Add(z0 + SegLen);
    }

    void PlaceHazard(Transform root, float z, bool[] blocked, float diff)
    {
        int roll = Random.Range(0, diff < 0.15f ? 3 : 6);
        if (roll <= 2)
        {
            // static blockers: crate / spikes / boulder, never all three lanes
            int n = (diff > 0.4f && Random.value < 0.5f) ? 2 : 1;
            int first = Random.Range(0, 3);
            for (int k = 0; k < n; k++)
            {
                int l = (first + k * 2) % 3;
                blocked[l] = true;
                HazardKind kind = roll == 0 ? HazardKind.Crate : roll == 1 ? HazardKind.Spike : HazardKind.Boulder;
                Factory.CreateHazard(kind, th, root, new Vector3(PlayerController.Lanes[l], 0f, z));
            }
        }
        else if (roll == 3)
        {
            Factory.CreateHazard(HazardKind.Saw, th, root, new Vector3(0f, 0f, z));
            blocked[0] = blocked[1] = blocked[2] = true;
        }
        else if (roll == 4)
        {
            int l = Random.Range(0, 3);
            blocked[l] = true;
            Factory.CreateHazard(HazardKind.Flyer, th, root, new Vector3(PlayerController.Lanes[l], 0f, z));
        }
        else
        {
            int l = Random.Range(0, 3);
            blocked[l] = true;
            Factory.CreateHazard(HazardKind.Robot, th, root, new Vector3(PlayerController.Lanes[l], 0f, z));
        }
    }
}
