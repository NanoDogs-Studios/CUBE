using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class Ability : ScriptableObject
{
    public string AbilityName;
    public Sprite AbilityIcon;
    public Sprite AbilityIconGrayscale;
    public int AbilityCooldown;
    public InputActionReference AbilityActivation;

    /// <summary>
    /// This method is called when the ability is activated.
    /// You can override this method in derived classes to implement specific ability behavior.
    /// <paramref name="player"/> is the player who activated the ability.
    /// </summary>
    public virtual void ActivateAbility(BasePlayer player)
    {
        Debug.Log($"{AbilityName} activated by " + player.gameObject.GetPhotonView().Owner.NickName);
    }
}