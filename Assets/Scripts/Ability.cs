using Photon.Pun;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

public class Ability : ScriptableObject
{
    public string AbilityName;
    public Sprite AbilityIcon;
    public Sprite AbilityIconGrayscale;
    public int AbilityCooldown;

    [Header("Input")]
    public InputActionAsset actionsAsset;
    public string actionMapName = "Player";
    public string actionName = "Ability1";

    // only for clarity, passive abilities cannot be activated
    public bool isPassive = false;

    [Range(1, 4)]
    public int AbilityLevel = 1;

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