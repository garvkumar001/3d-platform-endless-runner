using System.Collections;
using UnityEngine;

public enum PickupKind { Coin, Gem, Magnet, Shield, SpeedBoost, ScoreX2, Jetpack, Invincible }

/// <summary>Coins, gems and power-ups. Magnet pulls coins/gems; pickup plays a shrink animation.</summary>
public class Collectible : MonoBehaviour
{
    public PickupKind kind;
    bool taken;

    void Update()
    {
        if (taken) return;
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Player == null) return;
        PlayerController p = gm.Player;
        if (p.MagnetActive && (kind == PickupKind.Coin || kind == PickupKind.Gem))
        {
            Vector3 target = p.transform.position + Vector3.up;
            if (Vector3.Distance(transform.position, target) < 9f)
                transform.position = Vector3.MoveTowards(transform.position, target, (18f + p.CurrentSpeed) * Time.deltaTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (taken) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        if (GameManager.Instance == null) return;
        taken = true;
        GameManager.Instance.Collect(kind);
        StartCoroutine(Shrink());
    }

    IEnumerator Shrink()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, t / 0.2f);
            transform.position += Vector3.up * (3f * Time.deltaTime);
            yield return null;
        }
        Destroy(gameObject);
    }
}
