using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "OverclockedMind", menuName = "ScriptableObjects/Survivor Abilities/OverclockedMind", order = 1)]
public class OverclockedMind : PassiveAbility
{
    public override void PassiveEffect(BasePlayer player)
    {
        if (player.gameObject.GetComponent<OverclockedMindRunner>() == null)
            player.gameObject.AddComponent<OverclockedMindRunner>();
    }
}
