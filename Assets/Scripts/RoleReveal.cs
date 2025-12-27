using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RoleReveal : MonoBehaviour
{
    public RoundManager roundManager;

    public TMP_Text text;
    public TMP_Text secondtext;
    public CanvasGroup canvasGroup;
    public CanvasGroup canvasGroup1;

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
            text.text = "You are the <color=red>KILLER";
            secondtext.text = "Eliminate all <color=green>SURVIVORS";
        }
        else if (type == BasePlayer.PlayerType.Survivor)
        {
            text.text = "You are a <color=green>SURVIVOR";
            secondtext.text = "Survive the <color=red>KILLER";
        }

        StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        fadeGroup(canvasGroup, true);
        yield return new WaitForSeconds(2f);
        fadeGroup(canvasGroup1, true);

        yield return new WaitForSeconds(6f);
        StartCoroutine(fadeGroup(canvasGroup, false));
        yield return new WaitForSeconds(1f);
        StartCoroutine(fadeGroup(canvasGroup1, false));
    }

    IEnumerator fadeGroup(CanvasGroup group, bool fadeIn)
    {
        float duration = 2f;
        float elapsed = 0f;
        if (fadeIn)
        {
            group.alpha = 0f;
        }
        else
        {
            group.alpha = 1f;
        }
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = fadeIn ? (elapsed / duration) : (1 - (elapsed / duration));
            group.alpha = alpha;
            yield return null;
        }
        canvasGroup.alpha = fadeIn ? 1f : 0f;
    }
}
