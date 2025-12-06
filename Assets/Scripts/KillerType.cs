using UnityEngine;

[CreateAssetMenu(fileName = "Killer", menuName = "ScriptableObjects/Killer Type")]
public class KillerType : CubePlayerBase
{
    // the range at which the terror theme can be heard
    public int TerrorRadius = 60;
    public int PunchDamage = 20;

    public Vector3 punchHitboxSize = new Vector3(4f, 4f, 2f);
}
