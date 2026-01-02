using Discord.Sdk;
using DiscordRPC.Message;
using Lachee.Discord;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CUBEDiscordManager : MonoBehaviour
{
    [Header("Templates (set in Inspector)")]
    public Presence mainMenuPresence;
    public Presence intermissionPresence;
    public Presence inRoundPresence;

    Dictionary<string, RoomJoinInfo> activeJoinSecrets = new Dictionary<string, RoomJoinInfo>();

    struct RoomJoinInfo
    {
        public string PhotonRoomName;
        public string ConnectAddress;
    }

    [Header("Update")]
    [Min(0.25f)]
    public float updateInterval = 5f;

    private RoundManager roundManager;
    private float nextUpdateTime;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Prevent duplicates if you accidentally have this in multiple scenes
        var all = FindObjectsByType<CUBEDiscordManager>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Use the package singleton like their example does
        if (DiscordManager.current == null)
        {
            Debug.LogError("[Discord] DiscordManager.current is null. Make sure a DiscordManager exists in the scene.");
            return;
        }

        DiscordManager.current.Initialize();

        SceneManager.activeSceneChanged += OnSceneChanged;

        // Push menu presence AFTER init (small delay helps if RPC connection is async)
        StartCoroutine(PushPresenceNextFrame(() => BuildMainMenuPresence()));
        DiscordManager.current.client.OnJoin += HandleDiscordJoin;           // someone clicked Join
        DiscordManager.current.client.OnJoinRequested += HandleJoinRequest; // someone requests a join
    }
    private void HandleJoinRequest(object sender, JoinRequestMessage args)
    {
        Debug.Log($"Discord join requested from: {args.User.Username}");
    }

    private void HandleDiscordJoin(object sender, JoinMessage args)
    {
        Debug.Log($"Discord OnJoin: secret='{args.Secret}'");

        if (string.IsNullOrEmpty(args.Secret))
            return;

        ProcessJoinSecret(args.Secret);
    }

    private void ProcessJoinSecret(string secret)
    {
        if (activeJoinSecrets.TryGetValue(secret, out var info))
        {
            Debug.Log($"Connecting to Photon room: {info.PhotonRoomName}");

            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }

            PhotonNetwork.JoinRoom(info.PhotonRoomName);
        }
        else
        {
            Debug.LogWarning($"Join secret not found: {secret}");
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        UnhookRoundManager();
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        UnhookRoundManager();

        if (newScene.name == "SampleScene")
        {
            roundManager = FindFirstObjectByType<RoundManager>();
            if (roundManager != null)
            {
                roundManager.OnRoundStart += OnRoundStart;
                roundManager.OnIntermissionStart += OnIntermissionStart;

                nextUpdateTime = Time.time;

                // Give the new scene a frame (or two) so everything is actually initialized
                StartCoroutine(PushPresenceNextFrame(() => BuildGameplayPresence()));
            }
            else
            {
                Debug.LogWarning("[Discord] No RoundManager found in SampleScene.");
            }
        }
        else
        {
            // Back to menu
            StartCoroutine(PushPresenceNextFrame(() => BuildMainMenuPresence()));
        }
    }

    private void UnhookRoundManager()
    {
        if (roundManager == null) return;

        roundManager.OnRoundStart -= OnRoundStart;
        roundManager.OnIntermissionStart -= OnIntermissionStart;
        roundManager = null;
    }

    private void Update()
    {
        if (roundManager == null) return;

        if (Time.time >= nextUpdateTime)
        {
            SendPresence(BuildGameplayPresence());
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void OnIntermissionStart()
    {
        SendPresence(BuildGameplayPresence());
    }

    private void OnRoundStart()
    {
        SendPresence(BuildGameplayPresence());
    }

    // -------- Presence builders --------

    private Presence BuildMainMenuPresence()
    {
        var p = CloneTemplate(mainMenuPresence);

        p.details = "In Main Menu";
        p.state = ""; // or "Browsing menus", etc.

        // OPTIONAL: show connection/party info in menu if you want
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            p.state = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} players (in room)";
        }

        return p;
    }

    private Presence BuildGameplayPresence()
    {
        // Pick template based on round state
        var template = (roundManager != null && roundManager.roundActive)
            ? inRoundPresence
            : intermissionPresence;

        var p = CloneTemplate(template);

        // Time left
        float secondsLeft = roundManager != null ? roundManager.GetCurrentTime() : 0f;
        string timeText = FormatTime(secondsLeft);

        // Player count
        string playerText = "";
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            playerText = $"{PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} players";
        }

        p.state = (roundManager != null && roundManager.roundActive) ? "In Round" : "Intermission";
        p.details = $"Time Left: {timeText}";

        p.party.size = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
        p.party.maxSize = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.MaxPlayers : 0;

        string secret = $"{DiscordManager.current.applicationID}:{PhotonNetwork.CurrentRoom.Name}:{Guid.NewGuid()}";
        activeJoinSecrets[secret] = new RoomJoinInfo { PhotonRoomName = PhotonNetwork.CurrentRoom.Name };

        p.secrets.joinSecret = secret;

        string partyId = PhotonNetwork.CurrentRoom.Name;
        p.party.identifer = partyId;

        SendPresence(p);

        // Put player count somewhere visible (large tooltip is common)
        if (!string.IsNullOrEmpty(playerText))
        {
            var large = p.largeAsset;
            large.tooltip = playerText;
            p.largeAsset = large;
        }

        return p;
    }

    private void SanitizePresence(Presence p)
    {
        // Make sure arrays aren’t null
        if (p.buttons == null)
            p.buttons = new Lachee.Discord.Button[0];

        // Party must always have at least an ID
        if (p.party == null)
            p.party = new Lachee.Discord.Party();

        // Party size fields must be valid
        if (p.party.size < 0) p.party.size = 0;
        if (p.party.maxSize < 0) p.party.maxSize = 0;

        if (string.IsNullOrEmpty(p.secrets.joinSecret))
            p.secrets.joinSecret = "";
    }


    // -------- Sending --------

    private void SendPresence(Presence presence)
    {
        if (DiscordManager.current == null || presence == null)
            return;

        SanitizePresence(presence);

        Debug.Log($"[Discord] Sending presence: {presence.state} | {presence.details}");
        DiscordManager.current.SetPresence(presence);
    }

    private IEnumerator PushPresenceNextFrame(System.Func<Presence> builder)
    {
        // Two frames is surprisingly helpful for “default presence overwrote mine” issues
        yield return null;
        yield return null;

        SendPresence(builder());
    }

    // -------- Helpers --------

    // Clone template so we always send a fresh Presence object (avoids reference caching issues)
    private Presence CloneTemplate(Presence src)
    {
        if (src == null) return new Presence();

        var p = new Presence
        {
            state = src.state,
            details = src.details,
            startTime = src.startTime,
            endTime = src.endTime,
            largeAsset = src.largeAsset,
            smallAsset = src.smallAsset,
            party = src.party,
            secrets = src.secrets
        };

        // If Asset is a struct, assignment above is fine. If it's a class, this still works but shares refs.
        // To be extra safe, rebuild assets explicitly:
        p.largeAsset = new Asset { image = src.largeAsset.image, tooltip = src.largeAsset.tooltip };
        p.smallAsset = new Asset { image = src.smallAsset.image, tooltip = src.smallAsset.tooltip };

        return p;
    }

    private string FormatTime(float seconds)
    {
        if (seconds < 0) seconds = 0;
        int mins = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{mins:00}:{secs:00}";
    }
}
