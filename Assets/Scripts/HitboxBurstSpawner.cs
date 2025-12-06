using System;
using System.Collections;
using UnityEngine;

public class HitboxBurstSpawner : MonoBehaviour
{
    /// <summary>
    /// Spawns multiple hitboxes over a short time.
    /// patternCallback receives t in [0,1] for each step and returns a modified HitboxCreateArguments for that step.
    /// </summary>
    public void SpawnBurst(HitboxCreateArguments baseArgs, int damage, int count = 3, float duration = 0.12f, float lifetimePerHit = 0.08f, Func<float, HitboxCreateArguments> patternCallback = null)
    {
        StartCoroutine(SpawnBurstCoroutine(baseArgs, damage, count, duration, lifetimePerHit, patternCallback));
    }

    private IEnumerator SpawnBurstCoroutine(HitboxCreateArguments baseArgs, int damage, int count, float duration, float lifetimePerHit, Func<float, HitboxCreateArguments> pattern)
    {
        if (count <= 0) yield break;

        // If no pattern supplied, use a default forward sweep along baseArgs.Rotation forward axis
        if (pattern == null)
        {
            Vector3 forward = baseArgs.Rotation * Vector3.forward;
            Vector3 start = baseArgs.Center - forward * (baseArgs.Size.z * 0.5f);
            Vector3 end = baseArgs.Center + forward * (baseArgs.Size.z * 0.5f + 0.5f); // extra reach
            pattern = (t) =>
            {
                HitboxCreateArguments args = new HitboxCreateArguments();
                args.Size = baseArgs.Size; // same size by default
                args.Rotation = baseArgs.Rotation;
                args.Center = Vector3.Lerp(start, end, t);
                return args;
            };
        }

        float interval = duration / Mathf.Max(1, count - 1);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : (float)i / (count - 1);
            HitboxCreateArguments args = pattern(t);

            // create the hitbox networked
            HitboxCreator.CreateHitbox(args, damage, lifetimePerHit);

            if (i < count - 1)
                yield return new WaitForSeconds(interval);
        }
    }
}
