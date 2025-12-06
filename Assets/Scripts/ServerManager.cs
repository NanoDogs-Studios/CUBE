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

    private void Update()
    {
        CachePlayers();
        //allBasePlayers = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
        // skip any null entries or players without "BaseCharacter" in the name
        //allBasePlayers = Array.FindAll(allBasePlayers, p => p != null && p.name.Contains("BaseCharacter"));
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
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("HandleRoundStart", RpcTarget.MasterClient);
        }
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

        // ASSIGN ROLES
        foreach (GameObject player in allPlayers)
        {
            PhotonView pv = player.GetPhotonView();
            if (pv == null) continue;

            PlayerType type = (player == highestMalicePlayer)
                ? PlayerType.Killer
                : PlayerType.Survivor;

            pv.RPC("SetPlayerType", RpcTarget.AllBuffered, type);
        }
    }


}
