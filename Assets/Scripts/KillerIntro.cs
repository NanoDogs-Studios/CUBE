using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class KillerIntroManager : MonoBehaviourPunCallbacks
{
    public Transform killerIntroRoot;
    public float introDuration = 4f;

    private RoundManager roundManager;
    private Transform activeLevel2;
    private readonly Dictionary<Camera, List<Behaviour>> disabledCamBehaviours = new Dictionary<Camera, List<Behaviour>>();

    private void Start()
    {
        roundManager = FindFirstObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnRoundStart += OnRoundStart;
        }
    }

    private void OnDestroy()
    {
        if (roundManager != null)
        {
            roundManager.OnRoundStart -= OnRoundStart;
        }
    }

    private void OnRoundStart()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        BasePlayer killerPlayer = GetKillerPlayer();
        if (killerPlayer == null)
        {
            photonView.RPC("BeginGameplay", RpcTarget.All);
            return;
        }

        string killerName = killerPlayer.GetEquippedKiller().ScriptName;
        photonView.RPC("PlayKillerIntro", RpcTarget.All, killerName);
    }

    private BasePlayer GetKillerPlayer()
    {
        BasePlayer[] players = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
        foreach (BasePlayer p in players)
        {
            if (p.GetPlayerType() == BasePlayer.PlayerType.Killer)
                return p;
        }
        return null;
    }

    private Transform FindIntroExact(string name)
    {
        if (killerIntroRoot == null) return null;
        Transform level1 = killerIntroRoot.Find(name);
        if (level1 == null) return null;
        return level1.Find(name);
    }

    [PunRPC]
    public void PlayKillerIntro(string killerName)
    {
        Transform level2 = FindIntroExact(killerName);
        if (level2 == null)
        {
            photonView.RPC("BeginGameplay", RpcTarget.All);
            return;
        }

        Transform level1 = killerIntroRoot.Find(killerName);
        if (!killerIntroRoot.gameObject.activeSelf) killerIntroRoot.gameObject.SetActive(true);
        if (level1 != null && !level1.gameObject.activeSelf) level1.gameObject.SetActive(true);
        if (!level2.gameObject.activeSelf) level2.gameObject.SetActive(true);

        activeLevel2 = level2;

        Transform introCamera = level2.Find("Camera");

        FreezeAllPlayers(introCamera);
        StartCoroutine(IntroSequence(level2.gameObject));
    }

    private void FreezeAllPlayers(Transform introCam)
    {
        BasePlayer[] players = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
        foreach (BasePlayer p in players)
        {
            MovementSC move = p.GetComponent<MovementSC>();
            CharacterInput input = p.GetComponent<CharacterInput>();
            if (move != null) move.enabled = false;
            if (input != null) input.enabled = false;

            Camera cam = p.GetComponentInChildren<Camera>(true);
            if (cam != null && introCam != null)
            {
                if (!disabledCamBehaviours.ContainsKey(cam))
                {
                    var list = new List<Behaviour>();
                    foreach (var b in cam.GetComponents<Behaviour>())
                    {
                        if (b is Camera) continue;
                        if (b.enabled)
                        {
                            b.enabled = false;
                            list.Add(b);
                        }
                    }
                    disabledCamBehaviours[cam] = list;
                }

                Transform target = cam.transform.parent != null ? cam.transform.parent : cam.transform;
                target.SetPositionAndRotation(introCam.position, introCam.rotation);
            }
        }
    }

    private IEnumerator IntroSequence(GameObject introObj)
    {
        yield return new WaitForSeconds(introDuration);
        if (introObj != null) introObj.SetActive(false);
        photonView.RPC("BeginGameplay", RpcTarget.All);
    }

    [PunRPC]
    public void BeginGameplay()
    {
        BasePlayer[] players = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
        foreach (BasePlayer p in players)
        {
            MovementSC move = p.GetComponent<MovementSC>();
            CharacterInput input = p.GetComponent<CharacterInput>();
            if (move != null) move.enabled = true;
            if (input != null) input.enabled = true;

            Camera cam = p.GetComponentInChildren<Camera>(true);
            if (cam != null && disabledCamBehaviours.TryGetValue(cam, out var list))
            {
                foreach (var b in list)
                {
                    if (b != null) b.enabled = true;
                }
            }
        }

        disabledCamBehaviours.Clear();

        if (roundManager == null)
            roundManager = FindFirstObjectByType<RoundManager>();

        if (roundManager != null)
            roundManager.roundActive = true;

        if (activeLevel2 != null)
        {
            activeLevel2.gameObject.SetActive(false);
            activeLevel2 = null;
        }
    }
}