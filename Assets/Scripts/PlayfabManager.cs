using HSVPicker;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayfabManager : MonoBehaviour
{
    public GameObject nameWindow;
    public TMP_InputField nameField;
    public TMP_Text infoText;
    public ColorPicker picker;

    private TMP_Text moneyText;

    string PlayFabId;
    static string displayName;

    private float searchInterval = 1f;
    private float searchTimer = 0f;

    private void InfoText(string text)
    {
        infoText.text = text;
    }

    private void Start()
    {
        SceneManager.activeSceneChanged += SceneChanged;
        DontDestroyOnLoad(gameObject);

        InfoText(Application.version + " | " + "Not Logged In");
        Login();
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
            GetVirtualCurrencies();
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.Log("Error! See error below");
        Debug.Log(error.GenerateErrorReport());
    }

    private void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Logged in! additional info: " + result.ToJson());

        if (result.PlayFabId != null)
        {
            PlayFabId = result.PlayFabId;
        }

        if (result.InfoResultPayload.PlayerProfile.DisplayName != null)
        {
            displayName = result.InfoResultPayload.PlayerProfile.DisplayName;
            InfoText(Application.version + " | " + displayName + " | " + "Logged In");
        }
        else
        {
            InfoText(Application.version + " | " + "Not Set!" + " | " + "Logged In");
        }

        if (displayName == null)
        {
            nameWindow.SetActive(true);
        }

        GetColorFromData();
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

    public void GetVirtualCurrencies()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), OnGetUserInventorySuccess, OnError);
    }

    void OnGetUserInventorySuccess(GetUserInventoryResult result)
    {
        int cc = result.VirtualCurrency["CC"];
        if (moneyText != null)
        {
            moneyText.text = cc.ToString();
            Debug.Log("Got the currency and its value: " + cc);
        }
    }

    void GetColorFromData()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest() { PlayFabId = this.PlayFabId }, onSuccessGetData, OnError);
    }

    public void onSuccessGetData(GetUserDataResult result)
    {
        foreach(var item in result.Data)
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
        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest()
        {
            Data = new System.Collections.Generic.Dictionary<string, string>()
            {
                {"PlayerColor", "#" + picker.CurrentColor.ToHexString()}
            }
        },
        OnSetColorData, OnError
        );
    }

    public void OnSetColorData(UpdateUserDataResult result)
    {
        Debug.Log("Set Color: " + result.ToString());
    }

    public static string GetPlayerName()
    {
        return displayName;
    }

    public static Color FromHex(string hex)
    {
        if (hex.Length < 6)
        {
            throw new System.FormatException("Needs a string with a length of at least 6");
        }

        var r = hex.Substring(0, 2);
        var g = hex.Substring(2, 2);
        var b = hex.Substring(4, 2);
        string alpha;
        if (hex.Length >= 8)
            alpha = hex.Substring(6, 2);
        else
            alpha = "FF";

        return new Color((int.Parse(r, NumberStyles.HexNumber) / 255f),
                        (int.Parse(g, NumberStyles.HexNumber) / 255f),
                        (int.Parse(b, NumberStyles.HexNumber) / 255f),
                        (int.Parse(alpha, NumberStyles.HexNumber) / 255f));
    }
}
