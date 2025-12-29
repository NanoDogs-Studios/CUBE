using UnityEngine;

[CreateAssetMenu(fileName = "GlitchDash", menuName = "ScriptableObjects/Killer Abilities/GlitchDash", order = 1)]
public class GlitchDash : Ability
{
    public int dashDistance = 5;
    public AudioClip dashSound;
    public GameObject DashEffectsPrefab;


    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        Dash(player);
    }

    void Dash(BasePlayer player)
    {
        // Play dash sound
        if (dashSound != null)
        {
            AudioSource.PlayClipAtPoint(dashSound, player.transform.position);
        }

        Transform torso = player.transform.Find("RIG").Find("Torso");
        // Instantiate dash effects
        if (DashEffectsPrefab != null)
        {
            GameObject dashEffects = Instantiate(DashEffectsPrefab, new Vector3(torso.localPosition.x, 0.5f, torso.localPosition.z), Quaternion.Euler(new Vector3(-180, 0, 0)), torso);
            Object.Destroy(dashEffects, 1f); // Destroy effects after 2 seconds
        }

        Transform[] bones = player.transform.Find("RIG").GetComponentsInChildren<Transform>();
        foreach (Transform bone in bones)
        {
            if(bone.GetComponent<Rigidbody>() == null) continue;

            Vector3 dashDirection = bone.transform.forward * dashDistance; // Dash forward by 5 units
            bone.GetComponent<Rigidbody>().AddForce(dashDirection, ForceMode.VelocityChange);
        }
    }
}
