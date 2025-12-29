using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DynamicTxt : MonoBehaviour
{
    public string ignorePrefix = "//"; // Lines starting with this are ignored.
    public string urlPrefix = "url=";  // Supports: url=https://site.com|Custom Label

    [Header("UI")]
    public TMP_Text MOTDText;
    public TMP_Text UpdateTitle;
    public TMP_Text UpdateDesc;
    public RawImage MOTDImage;
    public RawImage UPDImage;

    [Header("Update Log")]
    public string addedPrefix = "-+";
    public string changedPrefix = "-="; // we'll display as "Fixed:"
    public string removedPrefix = "-|";
    public string infoPrefix = "-#";

    public TMP_Text UpdateLogTitle;
    public RawImage UpdateLogImage;
    public TMP_Text UpdateLogContent;

    [Header("Remote")]
    public string url = "https://raw.githubusercontent.com/NanoDogs-Studios/CUBE/refs/heads/main/motd.txt";

    [Tooltip("How often to refresh (seconds).")]
    [Min(5)]
    public float refreshIntervalSeconds = 120f;

    [Header("Format")]
    public string motdPrefix = "MOTD:";

    private Coroutine loop;

    // url= line (for UpdateLogContent link at the bottom)
    private string updateLogLinkUrl;
    private string updateLogLinkLabel;

    // Click handler for UpdateLogContent
    private TMP_TextEventHandler updateLogLinkHandler;

    private void Awake()
    {
        // Make UpdateLogContent clickable (not MOTD).
        if (UpdateLogContent != null)
        {
            UpdateLogContent.raycastTarget = true;

            updateLogLinkHandler = UpdateLogContent.GetComponent<TMP_TextEventHandler>();
            if (updateLogLinkHandler == null)
                updateLogLinkHandler = UpdateLogContent.gameObject.AddComponent<TMP_TextEventHandler>();

            updateLogLinkHandler.onLinkSelection.RemoveListener(OnUpdateLogLinkClicked);
            updateLogLinkHandler.onLinkSelection.AddListener(OnUpdateLogLinkClicked);
        }
    }

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

        // Reset so you don't keep appending each refresh.
        if (UpdateLogContent != null) UpdateLogContent.text = string.Empty;

        // Normalize newlines.
        string[] lines = raw.Replace("\r\n", "\n").Replace("\r", "\n")
            .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        string motd = null;
        string title = null;
        string desc = null;

        var addedLines = new List<string>();
        var fixedLines = new List<string>();
        var removedLines = new List<string>();
        var infoLines = new List<string>();

        // Reset url= link each refresh
        updateLogLinkUrl = null;
        updateLogLinkLabel = null;

        foreach (string l in lines)
        {
            string line = l.Trim().TrimStart('\uFEFF');
            if (line.Length == 0) continue;

            // Title/desc conventions
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

            // Update log lines
            if (line.StartsWith(removedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                removedLines.Add(line.Substring(removedPrefix.Length).Trim());
                continue;
            }
            if (line.StartsWith(changedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                fixedLines.Add(line.Substring(changedPrefix.Length).Trim());
                continue;
            }
            if (line.StartsWith(addedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                addedLines.Add(line.Substring(addedPrefix.Length).Trim());
                continue;
            }
            if (line.StartsWith(infoPrefix, StringComparison.OrdinalIgnoreCase))
            {
                infoLines.Add(line.Substring(infoPrefix.Length).Trim());
                continue;
            }

            if (line.StartsWith(ignorePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            // Images
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
                    if (tex != null)
                    {
                        if (UpdateLogImage != null) UpdateLogImage.texture = tex;
                        if (UPDImage != null) UPDImage.texture = tex;
                    }
                }));
                continue;
            }

            // MOTD
            if (!string.IsNullOrEmpty(motdPrefix) &&
                line.StartsWith(motdPrefix, StringComparison.OrdinalIgnoreCase))
            {
                motd = line.Substring(motdPrefix.Length).Trim();
                continue;
            }

            // URL line (goes at end of update log)
            if (line.StartsWith(urlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Supports: url=https://site.com|Discord
                string rawLink = line.Substring(urlPrefix.Length).Trim();

                int pipeIndex = rawLink.IndexOf('|');
                if (pipeIndex >= 0)
                {
                    updateLogLinkUrl = rawLink.Substring(0, pipeIndex).Trim();
                    updateLogLinkLabel = rawLink.Substring(pipeIndex + 1).Trim();
                }
                else
                {
                    updateLogLinkUrl = rawLink.Trim();
                    updateLogLinkLabel = rawLink.Trim(); // if no label, show the URL as text
                }

                continue;
            }
        }

        // Apply MOTD normally (no link wrapping)
        if (MOTDText != null && !string.IsNullOrEmpty(motd))
            MOTDText.text = motd;

        // Apply title/desc
        if (UpdateTitle != null && !string.IsNullOrEmpty(title))
        {
            UpdateTitle.text = title;
            if (UpdateLogTitle != null) UpdateLogTitle.text = title;
        }

        if (UpdateDesc != null && !string.IsNullOrEmpty(desc))
            UpdateDesc.text = desc;

        // Build update log
        if (UpdateLogContent != null)
        {
            foreach (string added in addedLines)
                UpdateLogContent.text += $"\nAdded: {added}";

            foreach (string fixedItem in fixedLines)
                UpdateLogContent.text += $"\nFixed: {fixedItem}";

            foreach (string removed in removedLines)
                UpdateLogContent.text += $"\nRemoved: {removed}";

            foreach (string info in infoLines)
                UpdateLogContent.text += $"\n{info}";

            // Append clickable link LAST (like your example)
            if (!string.IsNullOrEmpty(updateLogLinkUrl))
            {
                string label = string.IsNullOrEmpty(updateLogLinkLabel) ? "Link" : updateLogLinkLabel;

                // Clickable TMP link: linkId is the URL
                UpdateLogContent.text +=
                    $"\n<link=\"{EscapeForTmpLinkId(updateLogLinkUrl)}\"><u><color=#3AA0FF>{label}</color></u></link>";
            }
        }
    }

    private void OnUpdateLogLinkClicked(string linkId, string linkText, int linkIndex)
    {
        if (string.IsNullOrWhiteSpace(linkId)) return;

        if (!linkId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !linkId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"Blocked non-http(s) link: {linkId}", this);
            return;
        }

        Application.OpenURL(linkId);
    }

    private static string EscapeForTmpLinkId(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    public IEnumerator FetchImageFromUrl(string imgUrl, Action<Texture2D> callback)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(imgUrl))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"{nameof(DynamicTxt)}: Error fetching image: {req.error}\nURL: {imgUrl}", this);
                callback?.Invoke(null);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            callback?.Invoke(tex);
        }
    }
}
