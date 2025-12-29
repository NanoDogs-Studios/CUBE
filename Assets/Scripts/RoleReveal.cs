using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RoleReveal : MonoBehaviour
{
    public RoundManager roundManager;

    GameObject text1;
    GameObject text2;


    private void Start()
    {
        if (roundManager == null)
        {
            var go = GameObject.Find("Multiplayer");
            roundManager = go != null ? go.GetComponent<RoundManager>() : FindFirstObjectByType<RoundManager>();
        }
        if (roundManager != null)
        {
            roundManager.OnRoundStart += OnRoundStart;
        }
    }

    private void OnRoundStart()
    {
        BasePlayer localPlayer = null;

        BasePlayer[] players = FindObjectsByType<BasePlayer>(FindObjectsSortMode.None);
        foreach (BasePlayer player in players)
        {
            if (player.photonView.IsMine)
            {
                localPlayer = player;
                break;
            }
        }

        BasePlayer.PlayerType type = localPlayer.GetPlayerType();
        if (type == BasePlayer.PlayerType.Killer)
        {
            text1 = CUBEStatusText.FadeInAndCreate("You are the <color=red>KILLER</color>! Hunt down the <color=green>SURVIVORS</color>!", 2);
        }
        else if (type == BasePlayer.PlayerType.Survivor)
        {
            text2 = CUBEStatusText.FadeInAndCreate("You are a <color=green>SURVIVOR</color>! Work together to escape the <color=red>KILLER</color>!", 2);
        }
        StartCoroutine(Fade());
    }

    IEnumerator Fade()
    {
        yield return new WaitForSeconds(6);
        CUBEStatusText.FadeOutAndDestroy(text1, 2);
        yield return new WaitForSeconds(2f);
        CUBEStatusText.FadeOutAndDestroy(text2, 2);
    }
}
