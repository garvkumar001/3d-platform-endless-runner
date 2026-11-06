using UnityEngine;

/// <summary>Third-person chase camera with lane follow, speed-boost FOV kick and hit shake.</summary>
public class CameraRig : MonoBehaviour
{
    public PlayerController Target;

    Camera cam;
    float shake;
    bool snapped;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    public void Shake(float seconds)
    {
        shake = seconds;
    }

    void LateUpdate()
    {
        if (Target == null) return;
        Vector3 p = Target.transform.position;
        float gy = Mathf.Clamp(p.y, 0f, 3.5f);
        Vector3 desired = new Vector3(p.x * 0.55f, 4.3f + gy * 0.45f, p.z - 7.5f);
        if (!snapped) { transform.position = desired; snapped = true; }
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-12f * Time.deltaTime));

        if (shake > 0f)
        {
            shake -= Time.deltaTime;
            transform.position += Random.insideUnitSphere * (0.25f * Mathf.Clamp01(shake * 3f));
        }

        transform.LookAt(new Vector3(p.x * 0.55f, 1.4f + gy * 0.3f, p.z + 6f));

        float fov = 62f + (Target.SpeedT > 0f ? 10f : 0f);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, Time.deltaTime * 4f);
    }
}
