using UnityEngine;

[CreateAssetMenu(fileName = "UnstablePresence", menuName = "ScriptableObjects/Killers/The Glitch/Abilities/UnstablePresence", order = 1)]
public class UnstablePresence : PassiveAbility
{
    public override void PassiveEffect(BasePlayer player)
    {
        if (player.gameObject.GetComponent<UnstablePresenceRunner>() == null)
            player.gameObject.AddComponent<UnstablePresenceRunner>();
    }
}
