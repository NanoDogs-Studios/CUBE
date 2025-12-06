using UnityEngine;

[CreateAssetMenu(fileName = "GlitchDash", menuName = "ScriptableObjects/Killer Abilities/GlitchDash", order = 1)]
public class GlitchDash : Ability
{
    public int dashDistance = 5;
    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        Dash(player);
    }

    void Dash(BasePlayer player)
    {
        Transform[] bones = player.transform.Find("RIG").GetComponentsInChildren<Transform>();
        foreach (Transform bone in bones)
        {
            if(bone.GetComponent<Rigidbody>() == null) continue;

            Vector3 dashDirection = bone.transform.forward * dashDistance; // Dash forward by 5 units
            bone.GetComponent<Rigidbody>().AddForce(dashDirection, ForceMode.VelocityChange);
        }
    }
}
