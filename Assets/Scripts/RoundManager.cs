using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviourPunCallbacks
{
    public int currentTime;
    public int roundTime = 180; // 3 minutes
    [HideInInspector] public bool roundActive = false;
    [HideInInspector] public bool intermissionActive = false;

    private int intermissionTime = 40; // 40 seconds
    public Transform intermissionSpawnPoint;

    public event Action OnRoundStart;
    public event Action OnIntermissionStart;

    private Coroutine timerCoroutine;

    private void Start()
    {
        Debug.Log($"PlayerAbilityManager instances in scene: {FindObjectsByType<PlayerAbilityManager>(FindObjectsSortMode.InstanceID).Length}");
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartIntermission", RpcTarget.All);
        }
    }

    [PunRPC]
    public void StartRound()
    {
        roundActive = true;
        intermissionActive = false;
        currentTime = roundTime;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(Timer());

        OnRoundStart?.Invoke();
        Debug.Log($"RoundManager.StartRound invoked. Subscribers: {(OnRoundStart == null ? 0 : OnRoundStart.GetInvocationList().Length)}");
        Debug.Log("Round Started");
    }

    [PunRPC]
    public void StartIntermission()
    {
        roundActive = false;
        intermissionActive = true;
        currentTime = intermissionTime;

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(Timer());

        OnIntermissionStart?.Invoke();
    }

    private IEnumerator Timer()
    {
        while (roundActive || intermissionActive)
        {
            yield return new WaitForSecondsRealtime(1);
            currentTime--;

            if (roundActive && currentTime <= 0)
            {
                photonView.RPC("EndRound", RpcTarget.All);
            }
            else if (intermissionActive && currentTime <= 0)
            {
                photonView.RPC("StartRound", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    public void EndRound()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv == null)
                    continue;

                BasePlayer basePlayer = go.GetComponent<BasePlayer>();
                BasePlayerStats stats = go.GetComponent<BasePlayerStats>();
                if (basePlayer == null || stats == null)
                    continue;

                if (basePlayer.GetPlayerType() == BasePlayer.PlayerType.Survivor)
                {
                    stats.photonView.RPC("AdjustMalice", RpcTarget.AllBuffered, +1);
                }
                else if (basePlayer.GetPlayerType() == BasePlayer.PlayerType.Killer)
                {
                    stats.photonView.RPC("AdjustMalice", RpcTarget.AllBuffered, -5);
                }
            }
        }

        photonView.RPC("StartIntermission", RpcTarget.All);
    }

    public int GetCurrentTime()
    {
        return currentTime;
    }
}
