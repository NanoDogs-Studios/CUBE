using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameSyncer : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_Text nameText;
    public TMP_Text titleText;
    public Image verificationBadge;

    [Header("Verification Badges")]
    public Sprite verifiedBadge;
    public Sprite devBadge;

    private const string PROP_NAME = "n";
    private const string PROP_TITLE = "t";
    private const string PROP_VERIFICATION = "v";

    private void Start()
    {
        if (!photonView.IsMine) return;

        // 1) Get name locally (you already do this)
        string playerName = PlayfabManager.GetPlayerName();

        // 2) Get title id from PlayFab user data (you implement this call)
        // Example: returns "Supporter" (or "" if none equipped)

        string titleId = "";
        string verification = "";

        PlayfabManager.Instance.GetUserDataValue("PlayerTitle", titleId1 =>
        {
            if (string.IsNullOrEmpty(titleId1))
                titleId = ""; // default / none

            titleId = titleId1;
            // apply
            TitleStyleRegistry.Apply(titleText, titleId);
        });

        PlayfabManager.Instance.GetUserDataValue("VerificationStatus", verification1 =>
        {
            if (string.IsNullOrEmpty(verification1))
                verification = ""; // default
            verification = verification1;
        });

        // 3) Publish BOTH to Photon custom properties (late-join safe)
        var props = new Hashtable
        {
            { PROP_NAME, playerName },
            { PROP_TITLE, titleId },
            { PROP_VERIFICATION, verification }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, Hashtable changedProps)
    {
        // Only update if this PhotonView belongs to that player
        if (photonView.Owner != targetPlayer) return;

        ApplyFromPlayer(targetPlayer);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        // In case properties already exist when this object enables
        if (photonView != null && photonView.Owner != null)
            ApplyFromPlayer(photonView.Owner);
    }

    private void ApplyFromPlayer(Photon.Realtime.Player p)
    {
        if (nameText != null && p.CustomProperties.TryGetValue(PROP_NAME, out var nObj))
            nameText.text = nObj as string ?? "";

        if (titleText != null && p.CustomProperties.TryGetValue(PROP_TITLE, out var tObj))
        {
            string titleId = tObj as string ?? "";
            TitleStyleRegistry.Apply(titleText, titleId);
        }

        if (verificationBadge != null && p.CustomProperties.TryGetValue(PROP_VERIFICATION, out var vObj))
        {
            string verification = vObj as string ?? "";
            switch (verification)
            {
                case "Verified":
                    verificationBadge.sprite = verifiedBadge;
                    verificationBadge.gameObject.SetActive(true);
                    Debug.Log("Applied verified badge");
                    break;
                case "Developer":
                    verificationBadge.sprite = devBadge;
                    verificationBadge.gameObject.SetActive(true);
                    Debug.Log("Applied developer badge");
                    break;
                default:
                    verificationBadge.gameObject.SetActive(false);
                    break;
            }
        }
    }

    public static class TitleStyleRegistry
    {
        // Assign these in a ScriptableObject if you prefer.
        public static TMP_ColorGradient SupporterGradient;
        public static TMP_ColorGradient DevGradient;

        public static void Apply(TMP_Text txt, string titleId)
        {
            // Default
            if (string.IsNullOrWhiteSpace(titleId))
            {
                txt.gameObject.SetActive(false);
                return;
            }

            txt.gameObject.SetActive(true);

            switch (titleId)
            {
                case "Developer":
                    txt.text = "Developer";
                    if (DevGradient != null)
                        txt.colorGradientPreset = DevGradient;
                    txt.enableVertexGradient = true;
                    Debug.Log("Applied developer title style");
                    break;

                case "Supporter":
                    txt.text = "Supporter";
                    if (SupporterGradient != null)
                        txt.colorGradientPreset = SupporterGradient;
                    txt.enableVertexGradient = true;
                    Debug.Log("Applied supporter title style");
                    break;

                default:
                    txt.text = titleId; // fallback
                    txt.enableVertexGradient = false;
                    break;
            }
        }
    }
}

