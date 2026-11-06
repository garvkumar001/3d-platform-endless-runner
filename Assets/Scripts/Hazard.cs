using UnityEngine;

public enum HazardKind { Crate, Spike, Boulder, Saw, Flyer, Robot }

/// <summary>Obstacle / enemy behaviour. Hitting one costs a life (unless shielded or invincible).</summary>
public class Hazard : MonoBehaviour
{
    public HazardKind kind;
    public float moveSpeed;

    Vector3 start;
    float phase;

    void Start()
    {
        start = transform.position;
        phase = Random.value * 6.28f;
    }

    bool Active()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Player == null) return false;
        return transform.position.z - gm.Player.transform.position.z < 38f;
    }

    void Update()
    {
        if (!Active()) return;
        float dt = Time.deltaTime;
        switch (kind)
        {
            case HazardKind.Saw:
                transform.position = new Vector3(Mathf.Sin(Time.time * 1.6f + phase) * 2.2f, start.y, start.z);
                break;
            case HazardKind.Flyer:
                transform.position = new Vector3(start.x + Mathf.Sin(Time.time * 2f + phase) * 0.35f,
                    start.y + Mathf.Sin(Time.time * 3.5f + phase) * 0.15f, start.z);
                break;
            case HazardKind.Boulder:
                transform.position += Vector3.back * (moveSpeed * dt);
                transform.Rotate(Vector3.right, -moveSpeed * dt * 90f, Space.World);
                break;
            case HazardKind.Robot:
                transform.position += Vector3.back * (moveSpeed * dt);
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p != null) p.Hit();
    }
}
