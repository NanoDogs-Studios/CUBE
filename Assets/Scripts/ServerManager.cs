using Photon.Pun;
using System;
using UnityEngine;
using static BasePlayer;

// class responsible of things like: setting players type (killer, survivor)
public class ServerManager : MonoBehaviourPunCallbacks
{
    RoundManager roundManager;

    public GameObject[] allPlayers;
    public BasePlayer[] allBasePlayers;

    private void Start()
    {
        roundManager = GetComponent<RoundManager>();
        roundManager.OnRoundStart += RoundStartedRPC;
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundStart -= RoundStartedRPC;
        }
    }

    private void Update()
    {
        CachePlayers();
    }

    private void CachePlayers()
    {
        allPlayers = GameObject.FindGameObjectsWithTag("Player");
        allBasePlayers = Array.FindAll(
            FindObjectsByType<BasePlayer>(FindObjectsSortMode.None),
            p => p != null && p.gameObject.CompareTag("Player")
        );
    }

    public static KillerType GetCurrentKiller()
    {
        foreach (BasePlayer player in FindFirstObjectByType<ServerManager>().allBasePlayers)
        {
            if (player.GetPlayerType() == PlayerType.Killer)
            {
                return player.GetEquippedKiller();
            }
        }
        return null;
    }

    public void RoundStartedRPC()
    {
        // only the Master Client assigns roles
        if (!PhotonNetwork.IsMasterClient) return;

        // No need to RPC to ourselves; just call directly
        HandleRoundStart();
    }

    [PunRPC]
    public void HandleRoundStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // SNAPSHOT players once
        CachePlayers();

        GameObject highestMalicePlayer = null;
        int highestMalice = int.MinValue;
        System.Collections.Generic.List<GameObject> contenders = new System.Collections.Generic.List<GameObject>();

        // PICK HIGHEST MALICE
        foreach (GameObject player in allPlayers)
        {
            if (player == null) continue;

            BasePlayerStats stats = player.GetComponent<BasePlayerStats>();
            if (stats == null) continue;

            if (stats.malice > highestMalice)
            {
                highestMalice = stats.malice;
                contenders.Clear();
                contenders.Add(player);
            }
            else if (stats.malice == highestMalice)
            {
                contenders.Add(player);
            }
        }

        if (contenders.Count > 0)
        {
            int chosenIndex = UnityEngine.Random.Range(0, contenders.Count);
            highestMalicePlayer = contenders[chosenIndex];
        }

        // ASSIGN ROLES + CHARACTER SKINS
        foreach (GameObject player in allPlayers)
        {
            if (player == null) continue;

            PhotonView pv = player.GetPhotonView();
            BasePlayer basePlayer = player.GetComponent<BasePlayer>();
            PlayerCustomizer customizer = player.GetComponent<PlayerCustomizer>();

            if (pv == null || basePlayer == null || customizer == null)
                continue;

            PlayerType type = (player == highestMalicePlayer)
                ? PlayerType.Killer
                : PlayerType.Survivor;

            // 1) Tell everyone what role this player is
            pv.RPC("SetPlayerType", RpcTarget.AllBuffered, type);

            // 2) Decide which character ID to use based on equipped Malice selection
            string characterId = null;

            if (type == PlayerType.Killer)
            {
                KillerType killer = basePlayer.GetEquippedKiller();
                if (killer == null)
                {
                    Debug.LogWarning($"[ServerManager] Player {player.name} has no equipped KillerType.");
                    continue;
                }
                characterId = killer._Name;
            }
            else
            {
                SurvivorType survivor = basePlayer.GetEquippedSurvivor();
                if (survivor == null)
                {
                    Debug.LogWarning($"[ServerManager] Player {player.name} has no equipped SurvivorType.");
                    continue;
                }
                characterId = survivor._Name;
            }

            // 3) Apply role + character skin to every client via PlayerCustomizer
            PhotonView customizerPV = customizer.photonView;
            customizerPV.RPC(
                "ApplyRoleAndCharacter",
                RpcTarget.AllBuffered,
                type,
                characterId
            );
        }
    }
}
