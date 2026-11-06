using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Auto-running 3-lane character: lane switch (A/D, arrows, swipe), jump (Space/W/Up/swipe up),
/// slide (S/Down/swipe down), gravity with ground raycast, power-up timers, hit/shield logic.
/// Movement is kinematic so it is fully deterministic.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public static readonly float[] Lanes = { -2f, 0f, 2f };

    const float Gravity = 28f;
    const float JumpSpeed = 10.5f;
    const float LaneSpeed = 16f;
    const float SlideTime = 0.7f;

    // Power-up timers (seconds remaining) + shield flag
    public float MagnetT, SpeedT, MultT, JetT, InvT;
    public bool Shield;

    public bool MagnetActive { get { return MagnetT > 0f; } }
    public float Multiplier { get { return MultT > 0f ? 2f : 1f; } }
    public float CurrentSpeed { get; private set; }
    public bool Grounded { get; private set; }
    public float Z { get { return z; } }

    int lane = 1;
    float x, y, z, vy, coyote, slideT, hitCd, jumpBuffer;
    bool jetting;
    CapsuleCollider cap;
    Transform visual;
    Animator anim;
    Renderer[] rends;
    readonly HashSet<string> animParams = new HashSet<string>();

    public static PlayerController Create()
    {
        GameObject go = new GameObject("Player");
        go.transform.position = new Vector3(0f, 0f, 2f);
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        CapsuleCollider c = go.AddComponent<CapsuleCollider>();
        c.radius = 0.35f;
        c.height = 1.7f;
        c.center = new Vector3(0f, 0.9f, 0f);
        PlayerController pc = go.AddComponent<PlayerController>();
        pc.cap = c;
        pc.BuildVisual();
        return pc;
    }

    void Awake()
    {
        x = transform.position.x;
        y = transform.position.y;
        z = transform.position.z;
    }

    void BuildVisual()
    {
        // Drop your Mixamo/Asset-Store character at Resources/Prefabs/Player and it replaces the placeholder.
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Player");
        GameObject v = prefab != null ? Instantiate(prefab) : Factory.BuildPlayerVisual();
        v.name = "Visual";
        v.transform.SetParent(transform, false);
        v.transform.localPosition = Vector3.zero;
        v.transform.localRotation = Quaternion.identity;
        foreach (Collider c in v.GetComponentsInChildren<Collider>()) Destroy(c);
        visual = v.transform;
        rends = v.GetComponentsInChildren<Renderer>();
        anim = v.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            foreach (AnimatorControllerParameter p in anim.parameters) animParams.Add(p.name);
        }
    }

    // ------------------------------------------------------------------ main loop

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        float dt = Time.deltaTime;

        TickTimers(dt);
        CurrentSpeed = gm.BaseSpeed * (SpeedT > 0f ? 1.5f : 1f);

        if (InputAdapter.LeftPressed && lane > 0) lane--;
        if (InputAdapter.RightPressed && lane < 2) lane++;
        if (InputAdapter.JumpPressed) jumpBuffer = 0.15f;
        if (InputAdapter.SlidePressed)
        {
            if (Grounded) StartSlide();
            else vy = Mathf.Min(vy, -20f); // fast-fall
        }

        x = Mathf.MoveTowards(x, Lanes[lane], LaneSpeed * dt);
        z += CurrentSpeed * dt;
        Vertical(dt);

        if (slideT > 0f)
        {
            slideT -= dt;
            if (slideT <= 0f) EndSlide();
        }

        if (y < -8f)
        {
            gm.OnPlayerFell();
            return;
        }

        transform.position = new Vector3(x, y, z);
        AnimateVisual();
    }

    void Vertical(float dt)
    {
        if (JetT > 0f)
        {
            jetting = true;
            y = Mathf.Lerp(y, 4.5f, dt * 4f);
            vy = 0f;
            Grounded = false;
            return;
        }
        jetting = false;

        vy -= Gravity * dt;
        float newY = y + vy * dt;
        bool landed = false;
        if (vy <= 0f)
        {
            RaycastHit hit;
            Vector3 origin = new Vector3(x, y + 0.5f, z);
            float dist = 0.5f + (y - newY) + 0.05f;
            if (Physics.Raycast(origin, Vector3.down, out hit, dist, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponent<GroundTile>() != null)
            {
                newY = hit.point.y;
                vy = 0f;
                landed = true;
            }
        }
        y = newY;

        if (landed) { Grounded = true; coyote = 0.1f; }
        else { Grounded = false; coyote -= dt; }

        if (jumpBuffer > 0f)
        {
            jumpBuffer -= dt;
            if (Grounded || coyote > 0f) Jump();
        }
    }

    void Jump()
    {
        vy = JumpSpeed;
        Grounded = false;
        coyote = 0f;
        jumpBuffer = 0f;
        if (slideT > 0f) { slideT = 0f; EndSlide(); }
        Trigger("Jump");
        AudioManager.Play("jump");
    }

    void StartSlide()
    {
        slideT = SlideTime;
        cap.height = 0.9f;
        cap.center = new Vector3(0f, 0.45f, 0f);
        Trigger("Slide");
    }

    void EndSlide()
    {
        cap.height = 1.7f;
        cap.center = new Vector3(0f, 0.9f, 0f);
    }

    void TickTimers(float dt)
    {
        if (MagnetT > 0f) MagnetT -= dt;
        if (SpeedT > 0f) SpeedT -= dt;
        if (MultT > 0f) MultT -= dt;
        if (InvT > 0f) InvT -= dt;
        if (hitCd > 0f) hitCd -= dt;
        if (JetT > 0f)
        {
            JetT -= dt;
            // never end the jetpack above a gap
            if (JetT <= 0f && !GroundBelow()) JetT = 0.3f;
        }
    }

    bool GroundBelow()
    {
        RaycastHit hit;
        return Physics.Raycast(new Vector3(x, y, z), Vector3.down, out hit, 30f, ~0, QueryTriggerInteraction.Ignore)
               && hit.collider.GetComponent<GroundTile>() != null;
    }

    // ------------------------------------------------------------------ gameplay API

    public void Hit()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;
        if (InvT > 0f || hitCd > 0f) return;
        hitCd = 1.5f;
        AudioManager.Play("hit");
        if (Shield)
        {
            Shield = false;
            gm.Flash(new Color(0.3f, 0.6f, 1f, 0.45f));
            return;
        }
        Trigger("Hit");
        gm.LoseLife();
    }

    public void Activate(PickupKind k)
    {
        switch (k)
        {
            case PickupKind.Magnet: MagnetT = 8f; break;
            case PickupKind.Shield: Shield = true; break;
            case PickupKind.SpeedBoost: SpeedT = 6f; break;
            case PickupKind.ScoreX2: MultT = 8f; break;
            case PickupKind.Jetpack: JetT = 5f; break;
            case PickupKind.Invincible: InvT = 6f; break;
        }
        AudioManager.Play("powerup");
    }

    public void Respawn(float atZ)
    {
        lane = 1;
        x = Lanes[1];
        z = atZ;
        y = 1.5f;
        vy = 0f;
        JetT = 0f;
        slideT = 0f;
        EndSlide();
        hitCd = 2.5f;
        transform.position = new Vector3(x, y, z);
    }

    // ------------------------------------------------------------------ visuals

    void AnimateVisual()
    {
        if (anim != null)
        {
            SetFloat("Speed", CurrentSpeed);
            SetBool("Grounded", Grounded);
            SetBool("Sliding", slideT > 0f);
        }
        else if (visual != null)
        {
            float bob = Grounded && slideT <= 0f ? Mathf.Abs(Mathf.Sin(Time.time * 11f)) * 0.1f : 0f;
            visual.localPosition = new Vector3(0f, bob, 0f);
            visual.localScale = slideT > 0f ? new Vector3(1f, 0.55f, 1.2f) : Vector3.one;
            visual.localRotation = Quaternion.Euler(slideT > 0f ? 15f : 0f, (Lanes[lane] - x) * 12f, 0f);
        }

        bool blink = hitCd > 0f && Mathf.FloorToInt(Time.time * 12f) % 2 == 0;
        for (int i = 0; i < rends.Length; i++)
            if (rends[i] != null) rends[i].enabled = !blink;
    }

    void Trigger(string n) { if (anim != null && animParams.Contains(n)) anim.SetTrigger(n); }
    void SetFloat(string n, float v) { if (animParams.Contains(n)) anim.SetFloat(n, v); }
    void SetBool(string n, bool v) { if (animParams.Contains(n)) anim.SetBool(n, v); }
}
