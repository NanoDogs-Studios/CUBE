using HSVPicker;
using Photon.Pun;
using Photon.Realtime;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayfabManager : MonoBehaviourPunCallbacks
{
    public static PlayfabManager Instance { get; private set; }

    [Header("Login UI")]
    public GameObject nameWindow;
    public TMP_InputField nameField;
    public TMP_Text infoText;
    public ColorPicker picker;

    [Header("Currency")]
    [Tooltip("PlayFab Virtual Currency code used for Cube Coins")]
    public string cubeCoinsCode = "CC";

    private TMP_Text moneyText;

    public int CachedCC { get; private set; }
    public event Action<int> OnCubeCoinsChanged;

    string PlayFabId;
    static string displayName;

    private float searchInterval = 1f;
    private float searchTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void InfoText(string text)
    {
        if (infoText != null) infoText.text = text;
    }

    private void Start()
    {
        SceneManager.activeSceneChanged += SceneChanged;

        InfoText(Application.version + " | " + "Not Logged In");
        Login();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.activeSceneChanged -= SceneChanged;
    }

    void Login()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true,
            InfoRequestParameters = new GetPlayerCombinedInfoRequestParams
            {
                GetPlayerProfile = true
            }
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
    }

    void SceneChanged(Scene prev, Scene next)
    {
        moneyText = null;
    }

    private void Update()
    {
        if (moneyText == null)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchInterval)
            {
                searchTimer = 0f;
                TryFindMoneyText();
            }
        }
    }

    void TryFindMoneyText()
    {
        // Your current hierarchy:
        // Canvas/Stats/Money/MaliceValue (this is weirdly named, but fine)
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;

        Transform stats = canvas.transform.Find("Stats");
        if (stats == null) return;

        Transform money = stats.transform.Find("Money");
        if (money == null) return;

        Transform text = money.transform.Find("MaliceValue");
        if (text == null) return;

        moneyText = text.GetComponent<TMP_Text>();
        if (moneyText != null)
        {
            Debug.Log("Money text found!");
            RefreshCubeCoins();
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.Log("PlayFab Error:");
        Debug.Log(error.GenerateErrorReport());
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Logged in! additional info: " + result.ToJson());

        if (!string.IsNullOrEmpty(result.PlayFabId))
            PlayFabId = result.PlayFabId;

        var profile = result.InfoResultPayload?.PlayerProfile;
        if (profile != null && !string.IsNullOrEmpty(profile.DisplayName))
        {
            displayName = profile.DisplayName;
            InfoText(Application.version + " | " + displayName + " | " + "Logged In");
        }
        else
        {
            InfoText(Application.version + " | " + "Not Set!" + " | " + "Logged In");
            if (nameWindow != null) nameWindow.SetActive(true);
        }

        GetColorFromData();
        PhotonAuth(result.PlayFabId);

        // If UI already exists, pull CC immediately.
        // If not, TryFindMoneyText() will eventually find it and call RefreshCubeCoins().
        RefreshCubeCoins();
    }

    public void PhotonAuth(string playFabId)
    {
        var request = new GetPhotonAuthenticationTokenRequest
        {
            PhotonApplicationId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime
        };

        PlayFabClientAPI.GetPhotonAuthenticationToken(
            request,
            result => OnPhotonAuthSuccess(result, playFabId),
            error => Debug.LogError(error.GenerateErrorReport())
        );
    }

    private void OnPhotonAuthSuccess(GetPhotonAuthenticationTokenResult result, string playFabId)
    {
        Debug.Log("Got Photon Auth Token");

        var auth = new AuthenticationValues(playFabId);
        auth.AuthType = CustomAuthenticationType.Custom;
        auth.AddAuthParameter("username", playFabId);
        auth.AddAuthParameter("token", result.PhotonCustomAuthenticationToken);

        PhotonNetwork.AuthValues = auth;
        PhotonNetwork.NickName = playFabId;

        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon connected + authenticated to Master.");
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.LogError("Custom auth failed: " + debugMessage);
    }

    public void SubmitName()
    {
        var request = new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = nameField.text
        };
        PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnDisplayNameUpdate, OnError);
    }

    private void OnDisplayNameUpdate(UpdateUserTitleDisplayNameResult result)
    {
        Debug.Log("Updated name to: " + result.DisplayName);
        displayName = result.DisplayName;
        InfoText(Application.version + " | " + displayName + " | " + "Logged In");
    }

    // =============================
    // CUBE COINS (CC) API
    // =============================

    /// <summary>
    /// Re-reads CC from PlayFab and updates UI + cache.
    /// </summary>
    public void RefreshCubeCoins()
    {
        // GetUserInventory returns VirtualCurrency dict.
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            result =>
            {
                int cc = 0;
                if (result.VirtualCurrency != null && result.VirtualCurrency.TryGetValue(cubeCoinsCode, out var v))
                    cc = v;

                SetCachedCC(cc);
            },
            OnError);
    }

    private void SetCachedCC(int cc)
    {
        CachedCC = cc;

        if (moneyText != null)
            moneyText.text = cc.ToString();

        OnCubeCoinsChanged?.Invoke(cc);
    }

    /// <summary>
    /// Adds CC. Use for rewards, end-of-round payouts, etc.
    /// </summary>
    public void AddCubeCoins(int amount, string reason = "")
    {
        if (amount <= 0)
        {
            Debug.LogWarning("AddCubeCoins called with non-positive amount.");
            return;
        }

        var request = new AddUserVirtualCurrencyRequest
        {
            VirtualCurrency = cubeCoinsCode,
            Amount = amount
        };

        PlayFabClientAPI.AddUserVirtualCurrency(request,
            result =>
            {
                // result.Balance is the NEW balance after add.
                SetCachedCC(result.Balance);
                Debug.Log($"+{amount} {cubeCoinsCode} (reason: {reason}) -> balance {result.Balance}");
            },
            OnError);
    }

    /// <summary>
    /// Tries to spend CC. Calls back with success and the balance (new or unchanged).
    /// </summary>
    public void TrySpendCubeCoins(int cost, string reason, Action<bool, int> onResult)
    {
        if (cost <= 0)
        {
            onResult?.Invoke(true, CachedCC);
            return;
        }

        // Optional fast-fail (UI responsiveness). Real authority is PlayFab’s response.
        if (CachedCC < cost)
        {
            onResult?.Invoke(false, CachedCC);
            return;
        }

        var request = new SubtractUserVirtualCurrencyRequest
        {
            VirtualCurrency = cubeCoinsCode,
            Amount = cost
        };

        PlayFabClientAPI.SubtractUserVirtualCurrency(request,
            result =>
            {
                SetCachedCC(result.Balance);
                Debug.Log($"-{cost} {cubeCoinsCode} (reason: {reason}) -> balance {result.Balance}");
                onResult?.Invoke(true, result.Balance);
            },
            error =>
            {
                // If PlayFab rejects (insufficient funds, etc.) we keep cache as-is and notify.
                Debug.LogWarning("Spend failed: " + error.GenerateErrorReport());
                onResult?.Invoke(false, CachedCC);
            });
    }

    // =============================
    // Your existing color + misc
    // =============================

    void GetColorFromData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest() { PlayFabId = this.PlayFabId }, onSuccessGetData, OnError);
    }

    public void onSuccessGetData(GetUserDataResult result)
    {
        foreach (var item in result.Data)
        {
            if (item.Value.Value.Contains("#"))
            {
                string colorTrim = item.Value.Value.Trim('#');
                Color color = FromHex(colorTrim);
                picker.CurrentColor = color;
            }
        }
    }

    public static void IsItemOwned(string itemID, Action<bool> callback)
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            result =>
            {
                bool owned = false;
                foreach (var item in result.Inventory)
                {
                    if (item.ItemId == itemID)
                    {
                        owned = true;
                        break;
                    }
                }
                callback?.Invoke(owned);
            },
            error =>
            {
                Debug.LogError("Got error!");
                Debug.LogError(error.GenerateErrorReport());
                callback?.Invoke(false);
            });
    }

    public void SetColorToPicker()
    {
        Color color = picker.CurrentColor;
        string hex = ColorUtility.ToHtmlStringRGBA(color);

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"PlayerColor", "#" + hex}
            }
        },
        OnSetColorData, OnError);
    }

    public void OnSetColorData(UpdateUserDataResult result)
    {
        Debug.Log("Set Color: " + result.ToString());
    }

    public static string GetPlayerName() => displayName;

    /// <summary>
    /// Gets a single UserData value by key.
    /// Calls back with null if missing.
    /// </summary>
    public void GetUserDataValue(string key, Action<string> onResult)
    {
        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest
            {
                PlayFabId = PlayFabId,
                Keys = new System.Collections.Generic.List<string> { key }
            },
            result =>
            {
                if (result.Data != null &&
                    result.Data.TryGetValue(key, out var entry))
                {
                    onResult?.Invoke(entry.Value);
                }
                else
                {
                    onResult?.Invoke(null);
                }
            },
            error =>
            {
                Debug.LogWarning($"GetUserDataValue failed for key '{key}':\n{error.GenerateErrorReport()}");
                onResult?.Invoke(null);
            }
        );
    }


    public static Color FromHex(string hex)
    {
        if (hex.Length < 6) throw new System.FormatException("Needs a string with a length of at least 6");

        var r = hex.Substring(0, 2);
        var g = hex.Substring(2, 2);
        var b = hex.Substring(4, 2);
        string alpha = (hex.Length >= 8) ? hex.Substring(6, 2) : "FF";

        return new Color(
            (int.Parse(r, NumberStyles.HexNumber) / 255f),
            (int.Parse(g, NumberStyles.HexNumber) / 255f),
            (int.Parse(b, NumberStyles.HexNumber) / 255f),
            (int.Parse(alpha, NumberStyles.HexNumber) / 255f)
        );
    }
}

