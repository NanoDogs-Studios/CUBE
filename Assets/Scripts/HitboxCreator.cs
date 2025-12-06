using Photon.Pun;
using UnityEngine;
using System;

public static class HitboxCreator
{
    /// <summary>
    /// Creates a networked hitbox (Photon instantiate), configures transform and damage, and sets lifetime.
    /// The Hitbox prefab must exist in Resources with the given prefabName.
    /// </summary>
    public static Hitbox CreateHitbox(HitboxCreateArguments args, int damage, float lifetime = 0.12f, string prefabName = "Hitbox")
    {
        GameObject hbObj = PhotonNetwork.Instantiate(prefabName, args.Center, args.Rotation);

        // transform corrections
        hbObj.transform.position = args.Center;
        hbObj.transform.rotation = args.Rotation;
        hbObj.transform.localScale = args.Size;

        // Hitbox script
        Hitbox hitbox = hbObj.GetComponent<Hitbox>();
        if (hitbox == null)
            hitbox = hbObj.AddComponent<Hitbox>();

        hitbox.Damage = damage;

        // lifetime helper
        NetworkedLifetime lifetimeComp = hbObj.GetComponent<NetworkedLifetime>();
        if (lifetimeComp == null)
            lifetimeComp = hbObj.AddComponent<NetworkedLifetime>();

        lifetimeComp.Lifetime = Mathf.Max(0.01f, lifetime);

        return hitbox;
    }
}
