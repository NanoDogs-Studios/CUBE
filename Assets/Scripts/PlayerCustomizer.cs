using Photon.Pun;
using System;
using UnityEngine;

public class PlayerCustomizer : MonoBehaviourPunCallbacks
{
    public Renderer[] customizedBones;
    private Material instanceMaterial;

    RoundManager roundManager;

    string localHex;

    private void Start()
    {
        roundManager = GameObject.Find("Multiplayer").GetComponent<RoundManager>();
        roundManager.OnRoundStart += RoundStarted;
        roundManager.OnIntermissionStart += IntermissionStarted;

        // If we join while an intermission is already running, immediately restore player color
        if (roundManager.intermissionActive)
        {
            IntermissionStarted();
        }
    }

    private void RoundStarted()
    {
        if (!photonView.IsMine) return;

        BasePlayer basePlayer = GetComponent<BasePlayer>();
        BasePlayer.PlayerType playerType = basePlayer.GetPlayerType();


        if (playerType == BasePlayer.PlayerType.Killer)
        {
            string killerID = basePlayer.GetEquippedKiller()._Name;
            photonView.RPC("SetKillerCustomisationByName", RpcTarget.AllBuffered, killerID);
        }
        else
        {
            string survivorID = basePlayer.GetEquippedSurvivor()._Name;
            photonView.RPC("SetSurvivorCustomisationByName", RpcTarget.AllBuffered, survivorID);
        }
    }

    private void IntermissionStarted()
    {
        if (string.IsNullOrEmpty(localHex)) return;

        // Immediately revert locally, then broadcast so others see the updated lobby color
        ChangeColor(localHex);

        if (photonView.IsMine)
        {
            photonView.RPC("ChangeColor", RpcTarget.AllBuffered, localHex);
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


    [PunRPC]
    public void SetSurvivorCustomisationByName(string id)
    {
        SurvivorType survivorType = CharacterDatabase.GetSurvivorByName(id);
        if (survivorType == null) return;


        foreach (Renderer bone in customizedBones)
            bone.material = survivorType.character.bodyMaterial;
    }


    [PunRPC]
    public void SetKillerCustomisationByName(string id)
    {
        KillerType killerType = CharacterDatabase.GetKillerByName(id);
        if (killerType == null) return;

        foreach (Renderer bone in customizedBones)
            bone.material = killerType.character.bodyMaterial;

        transform.Find("ItemPoses").GetComponent<InputArmController>().enabled = true;
    }

    public void ChangeColorCalled(string hex)
    {
        if (photonView.IsMine)
        {
            photonView.RPC("ChangeColor", RpcTarget.AllBuffered, hex);
        }
    }

    [PunRPC]
    public void ChangeColor(string hex)
    {
        localHex = hex;

        Color converted = PlayfabManager.FromHex(hex);

        if (instanceMaterial == null)
        {
            Material baseMat = Resources.Load<Material>("PlayerMat");
            instanceMaterial = new Material(baseMat);
            instanceMaterial.name = "Instanced";

            foreach (Renderer bone in customizedBones)
            {
                bone.material = instanceMaterial;
            }
        }

        instanceMaterial.SetColor("_MainColor", converted);
    }



}