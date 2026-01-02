using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "LagSpike", menuName = "ScriptableObjects/Survivor Abilities/LagSpike", order = 1)]
public class LagSpike : Ability
{
    public Mesh cubeMesh;

    public float force = 400f;

    GameObject playerObj;
    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        playerObj = player.gameObject;
        ThrowShard();
    }

    public void ThrowShard()
    {
        GameObject shard = PhotonNetwork.Instantiate("GlitchShard", Vector3.zero, Quaternion.identity);
        Rigidbody rb = shard.AddComponent<Rigidbody>();
        GameObject player = playerObj.transform.Find("CameraHead").Find("Cam").Find("C").gameObject;

        shard.transform.position = player.transform.position + player.transform.forward * 1.5f;
        rb.AddForce(player.transform.forward * force, ForceMode.Impulse);
        rb.useGravity = false;
        // TODO: make it slow the killer
        Destroy(shard, 5f); // Clean up the shard object after 5 second
    }
}
