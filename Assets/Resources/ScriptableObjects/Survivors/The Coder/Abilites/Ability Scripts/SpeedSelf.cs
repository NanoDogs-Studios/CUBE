using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "SpeedSelf", menuName = "ScriptableObjects/Survivor Abilities/SpeedSelf", order = 1)]
public class SpeedSelf : Ability
{
    public override void ActivateAbility(BasePlayer player)
    {
        float speed = player.GetComponentInChildren<MovementSC>().Force;
        player.GetComponentInChildren<MovementSC>().Force = speed * 1.5f;

        player.photonView.RPC("SpeedSelfAbility", RpcTarget.AllBuffered);
    }
}
