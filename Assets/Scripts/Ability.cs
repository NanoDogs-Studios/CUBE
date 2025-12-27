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
    public InputActionAsset actionsAsset;
    public InputActionReference AbilityActivation;

    [Range(1, 4)]
    public int AbilityLevel = 1;

#if UNITY_EDITOR
    // This runs when you change something or focus back on the asset
    void OnValidate()
    {
        if (AbilityActivation == null && actionsAsset != null)
        {
            var action = actionsAsset.FindAction($"Ability{AbilityLevel}");
            if (action != null)
                AbilityActivation = InputActionReference.Create(action);
            EditorUtility.SetDirty(this);
        }
    }
#endif


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