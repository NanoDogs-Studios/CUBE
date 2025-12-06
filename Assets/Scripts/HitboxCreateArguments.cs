using UnityEngine;

/// <summary>
/// Creation arguments for a hitbox
/// </summary>
public class HitboxCreateArguments
{
    public Vector3 Size = new Vector3(4f, 4f, 2f);
    public Vector3 Center = Vector3.zero;
    public Quaternion Rotation = Quaternion.identity;
}