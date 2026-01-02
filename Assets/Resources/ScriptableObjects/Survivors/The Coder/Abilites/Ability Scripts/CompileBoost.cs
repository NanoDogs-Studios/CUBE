using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "CompileBoost", menuName = "ScriptableObjects/Survivor Abilities/CompileBoost", order = 1)]
public class CompileBoost : Ability
{
    GameObject playerObj;
    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        playerObj = player.gameObject;

        BoostCompileSpeed();
    }

    public void BoostCompileSpeed()
    {
        float force = playerObj.GetComponentInChildren<MovementSC>().Force;
        float speed = force * 2;
        playerObj.GetComponentInChildren<MovementSC>().Force = speed;
        playerObj.GetComponent<BasePlayerStats>().photonView.RPC("TakeDamage", RpcTarget.Others, 5);
        playerObj.GetComponentInChildren<MovementSC>().ResetAfter(2.5f, force);
    }
}
