using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DynamicTxt : MonoBehaviour
{
    [Header("UI")]
    // Keep your original field names so you can just drop this in.
    public TMP_Text MOTDText;
    public TMP_Text UpdateTitle;
    public TMP_Text UpdateDesc;
    public RawImage MOTDImage;
    public RawImage UPDImage;

    [Header("Remote")]
    public string url = "https://raw.githubusercontent.com/NanoDogs-Studios/CUBE/refs/heads/main/motd.txt";

    [Tooltip("How often to refresh (seconds).")]
    [Min(5)]
    public float refreshIntervalSeconds = 120f;

    [Header("Format")]
    [Tooltip("Line prefix for MOTD. Example: 'MOTD: Hello'.")]
    public string motdPrefix = "MOTD:";

    private Coroutine loop;

    private void OnEnable()
    {
        loop = StartCoroutine(RefreshLoop());
    }

    private void OnDisable()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            yield return FetchAndApply();
            yield return new WaitForSeconds(refreshIntervalSeconds);
        }
    }

    private IEnumerator FetchAndApply()
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError($"{nameof(DynamicTxt)}: URL is empty.", this);
            yield break;
        }

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{nameof(DynamicTxt)}: Error fetching text: {req.error}\nURL: {url}", this);
                yield break;
            }

            ParseAndApply(req.downloadHandler.text);
        }
    }

    private void ParseAndApply(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return;

        // Normalize newlines.
        string[] lines = raw.Replace("\r\n", "\n").Replace("\r", "\n")
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        string motd = null;
        string title = null;
        string desc = null;

        foreach (string l in lines)
        {
            // IMPORTANT: GitHub raw files can sometimes include a UTF-8 BOM at the start of the first line.
            // That makes it look like "MOTD:" but actually starts with '\uFEFF'.
            string line = l.Trim().TrimStart('\uFEFF');
            if (line.Length == 0) continue;

            // Most-specific first.
            if (line.StartsWith("-- "))
            {
                desc = line.Substring(3).Trim();
                continue;
            }

            if (line.StartsWith("- "))
            {
                title = line.Substring(2).Trim();
                continue;
            }

            if (line.StartsWith("MOTD_IMG=", StringComparison.OrdinalIgnoreCase))
            {
                string link = line.Substring("MOTD_IMG=".Length).Trim();

                StartCoroutine(FetchImageFromUrl(link, tex =>
                {
                    if (tex != null && MOTDImage != null)
                        MOTDImage.texture = tex;
                }));
                continue;
            }

            if (line.StartsWith("UPD_IMG=", StringComparison.OrdinalIgnoreCase))
            {
                string link = line.Substring("UPD_IMG=".Length).Trim();

                StartCoroutine(FetchImageFromUrl(link, tex =>
                {
                    if (tex != null && UPDImage != null)
                        UPDImage.texture = tex;
                }));
                continue;
            }


            if (!string.IsNullOrEmpty(motdPrefix) &&
                line.StartsWith(motdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                motd = line.Substring(motdPrefix.Length).Trim();
                continue;
            }
        }

        // Apply only if we found values.
        if (MOTDText && !string.IsNullOrEmpty(motd)) MOTDText.text = motd;
        if (UpdateTitle && !string.IsNullOrEmpty(title)) UpdateTitle.text = title;
        if (UpdateDesc && !string.IsNullOrEmpty(desc)) UpdateDesc.text = desc;

        // Helpful debug while setting up.
        // Debug.Log($"Parsed: MOTD='{motd}', Title='{title}', Desc='{desc}'");
    }

    public IEnumerator FetchImageFromUrl(string url, Action<Texture2D> callback)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{nameof(DynamicTxt)}: Error fetching image: {req.error}\nURL: {url}", this);
                callback?.Invoke(null);
                yield break;
            }
            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            callback?.Invoke(tex);
        }
    }
}
