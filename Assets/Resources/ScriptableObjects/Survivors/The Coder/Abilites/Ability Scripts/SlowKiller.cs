using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "SlowKiller", menuName = "ScriptableObjects/Survivor Abilities/SlowKiller", order = 1)]
public class SlowKiller : Ability
{
    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        float force = CubeReferences.GetKiller().GetComponentInChildren<MovementSC>().Force / 2;

        // rpc
        player.photonView.RPC("SlowKillerAbility", RpcTarget.AllBuffered, force);
    }
}
