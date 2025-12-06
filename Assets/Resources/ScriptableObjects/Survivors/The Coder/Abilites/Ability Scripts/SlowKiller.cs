using UnityEngine;

[CreateAssetMenu(fileName = "SlowKiller", menuName = "ScriptableObjects/Survivor Abilities/SlowKiller", order = 1)]
public class SlowKiller : Ability
{
    public override void ActivateAbility(BasePlayer player)
    {
        base.ActivateAbility(player);
        float force = CubeReferences.GetKiller().GetComponentInChildren<MovementSC>().Force;
        CubeReferences.GetKiller().GetComponentInChildren<MovementSC>().Force = force / 2;
    }
}
