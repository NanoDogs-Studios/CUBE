using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

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
    private double endTimestamp;

    public Vector3 SyncedIntermissionSpawnPos { get; private set; }
    public Quaternion SyncedIntermissionSpawnRot { get; private set; }

    private void Start()
    {
        Debug.Log($"PlayerAbilityManager instances in scene: {FindObjectsByType<PlayerAbilityManager>(FindObjectsSortMode.InstanceID).Length}");

        RefreshIntermissionSpawnFromPoint();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            double endTime = PhotonNetwork.Time + intermissionTime;
            photonView.RPC("StartIntermission", RpcTarget.All, SyncedIntermissionSpawnPos, SyncedIntermissionSpawnRot, endTime);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Send the current round state so late joiners keep timers, roles, and teleports in sync
        var actorNumbers = new System.Collections.Generic.List<int>();
        var roleValues = new System.Collections.Generic.List<int>();
        var colorHexes = new System.Collections.Generic.List<string>();

        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            PhotonView pv = go.GetComponent<PhotonView>();
            if (pv == null) continue;

            actorNumbers.Add(pv.OwnerActorNr);

            BasePlayer basePlayer = go.GetComponent<BasePlayer>();
            roleValues.Add(basePlayer != null ? (int)basePlayer.GetPlayerType() : -1);

            PlayerCustomizer customizer = go.GetComponent<PlayerCustomizer>();
            colorHexes.Add(customizer != null ? customizer.GetCurrentHex() : string.Empty);
        }

        photonView.RPC(
            "SyncState",
            newPlayer,
            roundActive,
            intermissionActive,
            endTimestamp,
            SyncedIntermissionSpawnPos,
            SyncedIntermissionSpawnRot,
            actorNumbers.ToArray(),
            roleValues.ToArray(),
            colorHexes.ToArray()
        );
    }

    [PunRPC]
    public void StartRound(double roundEndTime)
    {
        roundActive = true;
        intermissionActive = false;
        endTimestamp = roundEndTime;
        UpdateCurrentTime();

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
    public void StartIntermission(Vector3 spawnPosition, Quaternion spawnRotation, double intermissionEndTime)
    {
        roundActive = false;
        intermissionActive = true;
        SyncedIntermissionSpawnPos = spawnPosition;
        SyncedIntermissionSpawnRot = spawnRotation;
        endTimestamp = intermissionEndTime;
        UpdateCurrentTime();

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        timerCoroutine = StartCoroutine(Timer());

        OnIntermissionStart?.Invoke();
    }

    [PunRPC]
    private void SyncState(bool roundActiveState, bool intermissionActiveState, double syncedEndTimestamp, Vector3 spawnPosition, Quaternion spawnRotation, int[] actorNumbers, int[] roleValues, string[] colorHexes)
    {
        roundActive = roundActiveState;
        intermissionActive = intermissionActiveState;
        endTimestamp = syncedEndTimestamp;
        SyncedIntermissionSpawnPos = spawnPosition;
        SyncedIntermissionSpawnRot = spawnRotation;

        ApplyRolesAndColors(actorNumbers, roleValues, colorHexes);

        UpdateCurrentTime();

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        if (roundActive || intermissionActive)
        {
            timerCoroutine = StartCoroutine(Timer());

            if (intermissionActive)
            {
                OnIntermissionStart?.Invoke();
            }
            else if (roundActive)
            {
                OnRoundStart?.Invoke();
            }
        }
    }

    private IEnumerator Timer()
    {
        while (roundActive || intermissionActive)
        {
            UpdateCurrentTime();

            if (PhotonNetwork.IsMasterClient && PhotonNetwork.Time >= endTimestamp)
            {
                if (roundActive)
                {
                    photonView.RPC("EndRound", RpcTarget.All);
                }
                else if (intermissionActive)
                {
                    double nextEnd = PhotonNetwork.Time + roundTime;
                    photonView.RPC("StartRound", RpcTarget.All, nextEnd);
                }
            }

            yield return new WaitForSecondsRealtime(1);
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

        RefreshIntermissionSpawnFromPoint();
        double endTime = PhotonNetwork.Time + intermissionTime;
        photonView.RPC("StartIntermission", RpcTarget.All, SyncedIntermissionSpawnPos, SyncedIntermissionSpawnRot, endTime);
    }

    public int GetCurrentTime()
    {
        return currentTime;
    }

    public void UpdateIntermissionSpawn(Transform spawn)
    {
        intermissionSpawnPoint = spawn;
        RefreshIntermissionSpawnFromPoint();
    }

    private void RefreshIntermissionSpawnFromPoint()
    {
        if (intermissionSpawnPoint != null)
        {
            SyncedIntermissionSpawnPos = intermissionSpawnPoint.position;
            SyncedIntermissionSpawnRot = intermissionSpawnPoint.rotation;
        }
    }

    private void UpdateCurrentTime()
    {
        double remaining = Math.Max(0, endTimestamp - PhotonNetwork.Time);
        currentTime = (int)Math.Ceiling(remaining);
    }

    private void ApplyRolesAndColors(int[] actorNumbers, int[] roleValues, string[] colorHexes)
    {
        if (actorNumbers == null || roleValues == null || colorHexes == null) return;

        for (int i = 0; i < actorNumbers.Length; i++)
        {
            int actor = actorNumbers[i];
            int roleValue = i < roleValues.Length ? roleValues[i] : -1;
            string color = i < colorHexes.Length ? colorHexes[i] : string.Empty;

            foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
            {
                PhotonView pv = go.GetComponent<PhotonView>();
                if (pv == null || pv.OwnerActorNr != actor) continue;

                BasePlayer basePlayer = go.GetComponent<BasePlayer>();
                if (basePlayer != null && roleValue >= 0)
                {
                    basePlayer.SetPlayerType((BasePlayer.PlayerType)roleValue);
                }

                PlayerCustomizer customizer = go.GetComponent<PlayerCustomizer>();
                if (customizer != null && !string.IsNullOrEmpty(color))
                {
                    customizer.ChangeColor(color);
                }
            }
        }
    }
}
