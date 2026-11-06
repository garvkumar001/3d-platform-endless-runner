using UnityEngine;

/// <summary>Builds a small 3D showcase (camera, light, ground, character, props) behind menu UI.</summary>
public static class MenuBackdrop
{
    public static void Build(WorldTheme th, bool showPlayer)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.Lerp(th.Sky, Color.white, 0.55f) * 0.9f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = th.Fog;
        RenderSettings.fogStartDistance = 25f;
        RenderSettings.fogEndDistance = 80f;

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = th.Fog;
        cam.fieldOfView = 55f;
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 2.2f, -6.5f);
        camGo.transform.LookAt(new Vector3(0f, 1.3f, 0f));

        GameObject sun = new GameObject("Sun");
        Light l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.15f;
        sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

        Transform root = new GameObject("Backdrop").transform;
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(0f, -0.5f, 20f), new Vector3(7f, 1f, 80f), th.Ground);
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(-9f, -2f, 20f), new Vector3(10f, 1f, 80f), th.Side);
        Factory.Prim(PrimitiveType.Cube, root, new Vector3(9f, -2f, 20f), new Vector3(10f, 1f, 80f), th.Side);

        for (int i = 0; i < 10; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 pos = new Vector3(side * Random.Range(5f, 12f), -1.5f, 2f + i * 5f);
            Factory.CreateProp(th.Props[i % th.Props.Length], th, root, pos, Random.Range(0f, 360f));
        }

        for (int i = 0; i < 8; i++)
        {
            GameObject coin = Factory.CreatePickup(PickupKind.Coin, root, new Vector3((i % 3 - 1) * 2f, 1f, 3f + i * 2f));
            Collectible c = coin.GetComponent<Collectible>();
            if (c != null) Object.Destroy(c);
        }

        if (showPlayer)
        {
            GameObject holder = new GameObject("PlayerShowcase");
            holder.transform.position = new Vector3(0f, 0f, 0f);
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Player");
            GameObject v = prefab != null ? Object.Instantiate(prefab) : Factory.BuildPlayerVisual();
            v.transform.SetParent(holder.transform, false);
            v.transform.localPosition = Vector3.zero;
            v.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            v.transform.localScale = Vector3.one * 1.5f;
            foreach (Collider c in v.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        }
    }
}
