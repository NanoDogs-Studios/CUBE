using Photon.Pun;
using System.Linq;
using UnityEngine;

public class ProxSong : MonoBehaviour
{
    public float distanceThreshold = 10f;
    public float fadeSpeed = 2f; // how fast volume fades per second

    private SongLayer[] layers;
    private int maxLayer = 1;

    private RoundManager roundManager;

    public BasePlayer basePlayer;         // this instance's BasePlayer
    private KillerType killer;
    private CubeCharacterData data;

    private GameObject localPlayer;        // who we are locally

    private void Awake()
    {
        // find our local player (this only works after Photon or your netcode has spawned players)
        localPlayer = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None)
                      .FirstOrDefault(p => p.gameObject.GetPhotonView().IsMine) // use your own “is mine” check
                      ?.gameObject;
    }

    private void Start()
    {
        roundManager = FindFirstObjectByType<RoundManager>();
        if (roundManager != null)
        {
            // wait until the round actually starts
            roundManager.OnRoundStart += RoundStarted;
        }
        else
        {
            Debug.LogWarning("ProxSong: no RoundManager found.");
        }
    }

    private void RoundStarted()
    {
        // only run this on killers
        if (basePlayer == null || basePlayer.GetPlayerType() != BasePlayer.PlayerType.Killer)
        {
            // not a killer, no audio needed
            enabled = false;
            return;
        }

        killer = basePlayer.GetEquippedKiller();
        if (killer == null)
        {
            Debug.LogWarning("ProxSong: Killer data not found.");
            enabled = false;
            return;
        }

        data = killer.character;
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        AudioClip[] killerLayers = new AudioClip[]
        {
            data.layer1,
            data.layer2,
            data.layer3,
            data.layer4
        };

        layers = new SongLayer[killerLayers.Length];
        for (int i = 0; i < killerLayers.Length; i++)
        {
            var srcGO = new GameObject("AudioSource Layer" + (i + 1));
            srcGO.transform.SetParent(transform);
            AudioSource src = srcGO.AddComponent<AudioSource>();
            src.transform.localPosition = Vector3.zero;

            layers[i] = new SongLayer
            {
                layer = i + 1,
                audio = killerLayers[i],
                source = src
            };

            src.clip = killerLayers[i];
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            src.spatialBlend = 0f; // or 1f if you want 3D audio
            src.Play();
        }

        if (layers.Length > 0)
            maxLayer = layers.Max(l => l.layer);
    }

    private void Update()
    {
        if (localPlayer == null || layers == null) return;

        Transform localPlayerHips = localPlayer.transform.Find("RIG").Find("Hip");

        float distance = Vector3.Distance(transform.position, localPlayerHips.transform.position);

        foreach (var layer in layers)
        {
            if (layer == null || layer.source == null) continue;

            float activationRadius = (maxLayer - layer.layer + 1) * distanceThreshold;
            float targetVolume = (distance < activationRadius) ? 1f : 0f;

            layer.source.volume = Mathf.MoveTowards(
                layer.source.volume,
                targetVolume,
                fadeSpeed * Time.deltaTime);
        }
    }
}

[System.Serializable]
public class SongLayer
{
    public int layer;
    public AudioClip audio;
    public AudioSource source;
}
