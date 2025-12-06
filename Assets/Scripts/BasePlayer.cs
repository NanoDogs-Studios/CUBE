using Photon.Pun;
using System;
using UnityEngine;

public class BasePlayer : MonoBehaviourPunCallbacks
{
    #region Player Type
    public enum PlayerType
    {
        Killer,
        Survivor
    }

    public PlayerType playerType;

    public PlayerType GetPlayerType()
    {
        return playerType;
    }

    [PunRPC]
    public void SetPlayerType(PlayerType type)
    {
        Debug.Log($"Setting player type to: {type}");
        playerType = type;
    }

    #endregion

    #region Player Character

    public SurvivorType equippedSurvivor;
    public KillerType equippedKiller;

    public SurvivorType GetEquippedSurvivor()
    {
        return equippedSurvivor;
    }
    public KillerType GetEquippedKiller()
    {
        return equippedKiller;
    }

    [PunRPC]
    public void SetKiller(KillerType character)
    {
        Debug.Log($"Setting Killer to: {character}");
        equippedKiller = character;
    }
    [PunRPC]
    public void SetSurvivor(SurvivorType character)
    {
        Debug.Log($"Setting Survivor to: {character}");
        equippedSurvivor = character;
    }

    #endregion
}
