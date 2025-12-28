using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    [Header("References")]
    public RoundManager roundManager;
    public GameObject playerPrefab;
    public static GameObject LocalPlayerInstance;

    [Header("Spawns")]
    public List<Transform> killerSpawns;
    public List<Transform> survivorSpawns;
    public Transform intermissionSpawn;

    [Header("Maps")]
    public MapData[] maps;
    public GameObject currentMap;

    private void Awake()
    {
        // subscribe to round manager events if assigned
        if (roundManager != null)
        {
            roundManager.OnRoundStart += HandleRoundStart;
            roundManager.OnIntermissionStart += HandleIntermissionStart;

            if (intermissionSpawn != null)
            {
                roundManager.UpdateIntermissionSpawn(intermissionSpawn);
            }
        }
    }

    private void PopulateSpawns()
    {
        killerSpawns.Clear();
        survivorSpawns.Clear();

        if (currentMap == null) return;

        Transform[] allTransforms = currentMap.GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name.Contains("Spawn"))
            {
                if (t.name.Contains("Intermission"))
                {
                    intermissionSpawn = t;
                }

                if (t.name.Contains("Killer"))
                {
                    if (!killerSpawns.Exists(x => x.name == t.name))
                        killerSpawns.Add(t);
                }
                else
                {
                    if (!survivorSpawns.Exists(x => x.name == t.name))
                        survivorSpawns.Add(t);
                }
            }
        }
    }

    private void OnDestroy()
    {
        // unsubscribe to avoid leaks
        if (roundManager != null)
        {
            roundManager.OnRoundStart -= HandleRoundStart;
            roundManager.OnIntermissionStart -= HandleIntermissionStart;
        }
    }

    // This is now the main teleport method that other scripts call
    public void TeleportPlayer(Vector3 targetPosition, Quaternion? targetRotation = null)
    {
        if (LocalPlayerInstance == null)
        {
            Debug.LogWarning("LocalPlayerInstance is null!");
            return;
        }

        // Try to use the PlayerTeleportHandler if it exists
        PlayerTeleportHandler teleportHandler = LocalPlayerInstance.GetComponent<PlayerTeleportHandler>();
        if (teleportHandler != null)
        {
            teleportHandler.InitiateTeleport(targetPosition, targetRotation);
        }
        else
        {
            Debug.LogWarning("PlayerTeleportHandler component not found on LocalPlayerInstance.");
        }
    }

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master");
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined a room...");

        if (LocalPlayerInstance == null)
        {
            LocalPlayerInstance = PhotonNetwork.Instantiate(
                playerPrefab.name,
                roundManager != null ? roundManager.SyncedIntermissionSpawnPos : intermissionSpawn.position,
                roundManager != null ? roundManager.SyncedIntermissionSpawnRot : Quaternion.identity
            );
            Debug.Log("just called it");
            LocalPlayerInstance.GetComponent<PlayerCustomizer>().ChangeColorCalled(Resources.Load<Material>("PlayerMat").GetColor("_MainColor").ToHexString());
        }
        else
        {
            Vector3 spawnPos = roundManager != null ? roundManager.SyncedIntermissionSpawnPos : intermissionSpawn.position;
            Quaternion? spawnRot = roundManager != null ? roundManager.SyncedIntermissionSpawnRot : intermissionSpawn.rotation;
            TeleportPlayer(spawnPos, spawnRot);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("left room...");

        if (LocalPlayerInstance != null)
        {
            Destroy(LocalPlayerInstance);
        }
    }

    [PunRPC]
    public void ChooseMap(int mapIndex)
    {
        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("HandleRoundStart: No maps assigned in the inspector.");
            return;
        }

        if (currentMap != null)
        {
            Destroy(currentMap); // destroy old map if needed
        }

        // Everyone instantiates the same map index
        currentMap = Instantiate(maps[mapIndex].mapPrefab, new Vector3(0, 0, -200), Quaternion.identity);
        Debug.Log("Map chosen and instantiated: " + maps[mapIndex].mapName);

        // Now populate spawns after the map is created
        PopulateSpawns();

        if (roundManager != null && intermissionSpawn != null)
        {
            roundManager.UpdateIntermissionSpawn(intermissionSpawn);
        }

        // You can now choose spawn and teleport here if you want
        int killerIndex = Random.Range(0, killerSpawns.Count);
        int survivorIndex = Random.Range(0, survivorSpawns.Count);

        Transform chosenSpawn = (Random.value < 0.5f)
            ? killerSpawns[killerIndex]
            : survivorSpawns[survivorIndex];

        Debug.Log("Teleporting to: " + chosenSpawn.position);
        TeleportPlayer(chosenSpawn.position, chosenSpawn.rotation);
    }

    private void HandleRoundStart()
    {
        // Safety checks
        if (LocalPlayerInstance == null)
        {
            Debug.LogWarning("HandleRoundStart: LocalPlayerInstance is null. Player hasn't spawned yet.");
            return;
        }

        // Only the host chooses and spawns the map
        if (PhotonNetwork.IsMasterClient)
        {
            int mapIndex = Random.Range(0, maps.Length);
            photonView.RPC("ChooseMap", RpcTarget.AllBuffered, mapIndex);
        }
    }

    private void HandleIntermissionStart()
    {
        if (LocalPlayerInstance == null) return;
        Debug.Log("Intermission started — teleporting to intermission spawn.");
        Vector3 spawnPos = intermissionSpawn.position;
        Quaternion? spawnRot = intermissionSpawn.rotation;
        TeleportPlayer(spawnPos, spawnRot);
    }
}