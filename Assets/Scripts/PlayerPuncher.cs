using Photon.Pun;
using System;
using UnityEngine;

public class PlayerPuncher : MonoBehaviour
{
    public BasePlayer player;
    private KillerType killerType;
    private RoundManager roundManager;

    public PoseHandler armHandler;
    private HitboxBurstSpawner hitboxSpawner;

    private void Start()
    {
        hitboxSpawner = GetComponent<HitboxBurstSpawner>();
        if (hitboxSpawner == null)
            hitboxSpawner = gameObject.AddComponent<HitboxBurstSpawner>();

        if (armHandler != null)
        {
            armHandler.onArmHandle += Punch;
        }
        roundManager = FindFirstObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnRoundStart += RoundStarted;
        }
    }

    private void RoundStarted()
    {
        killerType = player.GetEquippedKiller();
    }

    private void Punch()
    {
        if (killerType == null) return;

        Vector3 size = killerType.punchHitboxSize;

        Transform hips = player.transform.Find("CameraHead").Find("Cam").Find("C");
        if (hips == null) return;

        // base center is slightly in front of hips
        Vector3 center = hips.position + hips.forward * (size.z / 2f + 0.6f);
        Quaternion yRotation = Quaternion.Euler(0f, hips.eulerAngles.y, 0f);

        HitboxCreateArguments baseArgs = new HitboxCreateArguments
        {
            Size = size,
            Center = center,
            Rotation = yRotation
        };

        // spawn 4 hitboxes over 0.12s (tweak as needed)
        int hits = 4;
        float totalDuration = 0.12f;
        float lifetimePerHit = 0.08f;

        // Optional: custom pattern: small arc or expanding size
        Func<float, HitboxCreateArguments> arcPattern = (t) =>
        {
            // t in [0,1]. We'll lerp the center forward and slightly to the right then left so it feels 'swipey'
            Vector3 fw = yRotation * Vector3.forward;
            Vector3 right = yRotation * Vector3.right;

            float forwardStart = 0.2f;
            float forwardEnd = 1.1f;
            Vector3 arcCenter = hips.position + fw * Mathf.Lerp(forwardStart, forwardEnd, t);

            // add a tiny lateral curve (sinusoidal)
            float lateral = Mathf.Sin(t * Mathf.PI) * 0.2f; // 0 -> 0.2 -> 0
            arcCenter += right * lateral;

            // optionally scale size slightly over the burst
            Vector3 stepSize = size * (1f + 0.15f * t);

            return new HitboxCreateArguments
            {
                Size = stepSize,
                Center = arcCenter,
                Rotation = yRotation
            };
        };

        hitboxSpawner.SpawnBurst(baseArgs, killerType.PunchDamage, hits, totalDuration, lifetimePerHit, arcPattern);
    }
}
