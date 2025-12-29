using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "GlitchShard", menuName = "ScriptableObjects/Killer Abilities/GlitchShard", order = 1)]
public class GlitchShard : Ability
{
    public Mesh cubeMesh;

    public float force = 500f;
    public int damage = 15;
    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        PhotonView pv = player.GetComponent<PhotonView>();
        pv.RPC("ThrowShard", RpcTarget.All, player);
    }

    [PunRPC]
    public void ThrowShard(BasePlayer playerObj)
    {
        GameObject shard = PhotonNetwork.Instantiate("GlitchShard", Vector3.zero, Quaternion.identity);
        shard.GetComponent<MeshFilter>().mesh = cubeMesh;
        shard.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Glitch");

        Rigidbody rb = shard.AddComponent<Rigidbody>();

        GameObject player = playerObj.transform.Find("CameraHead").Find("Cam").Find("C").gameObject;

        shard.transform.position = player.transform.position + player.transform.forward * 1.5f;
        shard.transform.localScale = new Vector3(45f,45f,45f);
        shard.GetComponent<BoxCollider>().size = new Vector3(0.02f, 0.02f, 0.02f);
        rb.AddForce(player.transform.forward * force, ForceMode.Impulse);
        rb.useGravity = false;

        HitboxCreateArguments args = new HitboxCreateArguments
        {
            Center = shard.transform.position,
            Size = shard.transform.localScale * 0.5f,
            Rotation = shard.transform.rotation
        };
        Hitbox hitbox = HitboxCreator.CreateHitbox(args, damage, 5);
        Destroy(shard, 5f); // Clean up the shard object after 1 second
    }
}
