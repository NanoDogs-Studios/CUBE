using Photon.Pun;
using UnityEngine;

public class PlayerCustomizer : MonoBehaviourPunCallbacks
{
    [Header("Renderers to skin / color")]
    public Renderer[] customizedBones;

    private Material instanceMaterial; // PlayerMat instance used in intermission
    private RoundManager roundManager;

    // Stored player color (hex) so we can restore it on intermission
    private string localHex;

    private void Start()
    {
        roundManager = GameObject.Find("Multiplayer")?.GetComponent<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnRoundStart += RoundStarted;
            roundManager.OnIntermissionStart += IntermissionStarted;

            // If we join while an intermission is already running, immediately restore player color
            if (roundManager.intermissionActive)
            {
                IntermissionStarted();
            }
        }
        else
        {
            Debug.LogWarning("[PlayerCustomizer] RoundManager not found (Multiplayer object missing?)");
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundStart -= RoundStarted;
            roundManager.OnIntermissionStart -= IntermissionStarted;
        }
    }

    /// <summary>
    /// We no longer decide the skin here – that comes from ServerManager via RPC.
    /// This is mostly a hook if you want to clear intermission state when the round starts.
    /// </summary>
    private void RoundStarted()
    {
        // Going into a round; we’ll get an ApplyRoleAndCharacter RPC shortly.
        // Clear reference so we don’t think we’re still using PlayerMat.
        instanceMaterial = null;
    }

    /// <summary>
    /// Called by RoundManager when intermission begins.
    /// Revert back to PlayerMat with the stored color.
    /// </summary>
    private void IntermissionStarted()
    {
        // If we don't have a saved color yet, you can either early-out
        // or choose a default (e.g. white). I'll fallback to default white.
        string hexToUse = string.IsNullOrEmpty(localHex) ? "FFFFFF" : localHex;

        // Immediately revert locally
        ChangeColor(hexToUse);

        // Make sure everybody sees the lobby color
        if (photonView.IsMine)
        {
            photonView.RPC("ChangeColor", RpcTarget.AllBuffered, hexToUse);
        }
    }

    // ---------------------------
    //  CENTRALIZED ROLE + SKIN
    // ---------------------------

    /// <summary>
    /// Centralized RPC: ServerManager (or whoever assigns roles) should call this
    /// once per player when the round setup is finished.
    ///
    /// Example call:
    /// playerCustomizer.photonView.RPC(
    ///     \"ApplyRoleAndCharacter\",
    ///     RpcTarget.AllBuffered,
    ///     BasePlayer.PlayerType.Killer,
    ///     killerType._Name);
    /// </summary>
    [PunRPC]
    public void ApplyRoleAndCharacter(BasePlayer.PlayerType playerType, string characterId)
    {
        // We’re in round mode now, no longer using PlayerMat instance.
        instanceMaterial = null;

        if (playerType == BasePlayer.PlayerType.Killer)
        {
            ApplyKillerCustomisation(characterId);
        }
        else
        {
            ApplySurvivorCustomisation(characterId);
        }
    }

    // Keep these as RPCs for backward compatibility if anything still uses them.
    // Internally they just call the shared helpers.

    [PunRPC]
    public void SetSurvivorCustomisationByName(string id)
    {
        ApplySurvivorCustomisation(id);
    }

    [PunRPC]
    public void SetKillerCustomisationByName(string id)
    {
        ApplyKillerCustomisation(id);
    }

    private void ApplySurvivorCustomisation(string id)
    {
        SurvivorType survivorType = CharacterDatabase.GetSurvivorByName(id);
        if (survivorType == null)
        {
            Debug.LogWarning($"[PlayerCustomizer] SurvivorType '{id}' not found in CharacterDatabase.");
            return;
        }

        Material mat = survivorType.character.bodyMaterial;
        foreach (Renderer bone in customizedBones)
        {
            bone.material = mat;
        }

        foreach (Accessory accessory in survivorType.character.accessories)
        {
            Transform accTransform = transform.Find(accessory.boneName);
            if (accTransform != null)
            {
                GameObject accInstance = Instantiate(accessory.prefab, accTransform);
                accInstance.transform.localPosition = Vector3.zero;
                accInstance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Debug.LogWarning($"[PlayerCustomizer] Accessory bone '{accessory.boneName}' not found on player.");
            }
        }
    }

    private void ApplyKillerCustomisation(string id)
    {
        KillerType killerType = CharacterDatabase.GetKillerByName(id);
        if (killerType == null)
        {
            Debug.LogWarning($"[PlayerCustomizer] KillerType '{id}' not found in CharacterDatabase.");
            return;
        }

        Material mat = killerType.character.bodyMaterial;
        foreach (Renderer bone in customizedBones)
        {
            bone.material = mat;
        }

        // Enable weapon-hand IK/animation once we're a Killer
        Transform itemPoses = transform.Find("ItemPoses");
        if (itemPoses != null)
        {
            InputArmController armController = itemPoses.GetComponent<InputArmController>();
            if (armController != null)
            {
                armController.enabled = true;
            }
        }
    }

    // ---------------------------
    //  INTERMISSION COLOR LOGIC
    // ---------------------------

    /// <summary>
    /// Called from UI to change the player's lobby color.
    /// This wraps the RPC so it propagates to everyone.
    /// </summary>
    public void ChangeColorCalled(string hex)
    {
        if (!photonView.IsMine) return;

        photonView.RPC("ChangeColor", RpcTarget.AllBuffered, hex);
    }

    public string GetCurrentHex()
    {
        return localHex;
    }

    [PunRPC]
    public void ChangeColor(string hex)
    {
        localHex = hex;

        Color converted = PlayfabManager.FromHex(hex);

        if (instanceMaterial == null)
        {
            Material baseMat = Resources.Load<Material>("PlayerMat");
            if (baseMat == null)
            {
                Debug.LogError("[PlayerCustomizer] Could not load PlayerMat from Resources.");
                return;
            }

            instanceMaterial = new Material(baseMat);
            instanceMaterial.name = "Instanced_PlayerMat";

            foreach (Renderer bone in customizedBones)
            {
                bone.material = instanceMaterial;
            }
        }

        instanceMaterial.SetColor("_MainColor", converted);
    }
}
