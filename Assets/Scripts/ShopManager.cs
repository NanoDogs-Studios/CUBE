using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    public GameObject shop;
    public InputActionReference toggleShop;

    [Header("Shop Grids")]
    public GameObject shopKillerContent;
    public GameObject shopSurvivorContent;
    public GameObject shopEmoteContent;

    [Header("Owned Grids")]
    public GameObject ownedKillerContent;   // grid for owned killers
    public GameObject ownedSurvivorContent; // grid for owned survivors
    public GameObject ownedEmoteContent;    // grid for owned emotes

    public GameObject characterPrefab;

    [Header("Data")]
    public KillerType[] killers;
    public SurvivorType[] survivors;
    public EmoteAsset[] emotes;

    [Header("UI")]
    public GameObject CharDetails;
    public Image image;
    public TMP_Text Name;
    public TMP_Text Description;
    public TMP_Text Difficulty;
    public TMP_Text Cost;
    public TMP_Text Health;
    public TMP_Text Speed;
    public TMP_Text Stamina;
    public Button purchaseButton;

    private CubePlayerBase selectedCharacter;
    private EmoteAsset selectedEmote;

    // PlayFab owned items cache
    private HashSet<string> ownedItemIds = new HashSet<string>();

    // 🔹 Public event so other scripts can subscribe
    public event Action<CubePlayerBase> OnCharacterPurchased;

    private void Start()
    {
        if (toggleShop != null)
        {
            toggleShop.action.performed += ShopTriggered;
        }

        if (shop != null)
        {
            shop.SetActive(false);
        }

        if (CharDetails != null)
        {
            CharDetails.SetActive(false);
        }

        // Initial population based on PlayFab inventory
        RefreshShopFromInventory();
    }

    private void OnDestroy()
    {
        if (toggleShop != null)
        {
            toggleShop.action.performed -= ShopTriggered;
        }
    }

    private void ShopTriggered(InputAction.CallbackContext context)
    {
        if (shop == null)
            return;

        bool newState = !shop.activeInHierarchy;
        shop.SetActive(newState);

        // Assuming CursorManager.SetState(true) means lock/hide cursor for gameplay
        CursorManager.SetState(!newState);
    }

    #region Inventory / Build UI

    private void RefreshShopFromInventory()
    {
        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            inv =>
            {
                ownedItemIds = new HashSet<string>(inv.Inventory.Select(i => i.ItemId));
                BuildShopUI();
            },
            error =>
            {
                Debug.LogError("Failed to get inventory for shop: " + error.GenerateErrorReport());
                // Even if inventory fails, build UI with everything as unowned
                ownedItemIds.Clear();
                BuildShopUI();
            });
    }

    private void BuildShopUI()
    {
        // Clear previous entries
        ClearChildren(shopKillerContent?.transform);
        ClearChildren(ownedKillerContent?.transform);
        ClearChildren(shopSurvivorContent?.transform);
        ClearChildren(ownedSurvivorContent?.transform);
        ClearChildren(shopEmoteContent?.transform);
        ClearChildren(ownedEmoteContent?.transform);

        selectedCharacter = null;
        selectedEmote = null;
        if (CharDetails != null)
            CharDetails.SetActive(false);

        // Populate killers
        foreach (KillerType killer in killers)
        {
            bool isOwned = killer != null && ownedItemIds.Contains(killer._PlayFabItemID);
            Transform parent = isOwned && ownedKillerContent != null
                ? ownedKillerContent.transform
                : shopKillerContent.transform;

            if (parent == null) continue;

            GameObject charKiller = Instantiate(characterPrefab, parent);
            CharacterTemplate template = charKiller.GetComponent<CharacterTemplate>();
            template.AssociatedCharacter = killer;

            charKiller.GetComponent<Button>().onClick.AddListener(() =>
                OnCharacterClicked(template)
            );
        }

        // Populate survivors
        foreach (SurvivorType survivor in survivors)
        {
            bool isOwned = survivor != null && ownedItemIds.Contains(survivor._PlayFabItemID);
            Transform parent = isOwned && ownedSurvivorContent != null
                ? ownedSurvivorContent.transform
                : shopSurvivorContent?.transform;

            if (parent == null) continue;

            GameObject charSurvivor = Instantiate(characterPrefab, parent);
            CharacterTemplate template = charSurvivor.GetComponent<CharacterTemplate>();
            template.AssociatedCharacter = survivor;

            charSurvivor.GetComponent<Button>().onClick.AddListener(() =>
                OnCharacterClicked(template)
            );
        }

        // Populate emotes
        foreach (EmoteAsset asset in emotes)
        {
            bool isOwned = asset != null && ownedItemIds.Contains(asset.PlayFabItemId);
            Transform parent = isOwned && ownedEmoteContent != null
                ? ownedEmoteContent.transform
                : shopEmoteContent.transform;

            if (parent == null) continue;

            GameObject emoteEntry = Instantiate(characterPrefab, parent);
            EmoteTemplate template = emoteEntry.GetComponent<EmoteTemplate>();
            template.AssociatedEmote = asset;

            emoteEntry.GetComponent<Button>().onClick.AddListener(() =>
                OnEmoteClicked(template)
            );
        }

        // ✅ Default active tab after rebuild (only one tab visible)
        SetActiveContent(shopKillerContent);
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            Destroy(t.GetChild(i).gameObject);
        }
    }

    #endregion

    #region Tabs

    private void SetActiveContent(GameObject target)
    {
        // Disable all grids
        GameObject[] all =
        {
            shopKillerContent,
            ownedKillerContent,
            shopSurvivorContent,
            ownedSurvivorContent,
            shopEmoteContent,
            ownedEmoteContent
        };

        foreach (var go in all)
        {
            if (go != null)
                go.SetActive(false);
        }

        // Enable only target
        if (target != null)
            target.SetActive(true);
    }

    // Hook these up to your tab buttons in the Inspector

    public void ShowShopKillers()
    {
        SetActiveContent(shopKillerContent);
    }

    public void ShowOwnedKillers()
    {
        SetActiveContent(ownedKillerContent);
    }

    public void ShowShopSurvivors()
    {
        SetActiveContent(shopSurvivorContent);
    }

    public void ShowOwnedSurvivors()
    {
        SetActiveContent(ownedSurvivorContent);
    }

    public void ShowShopEmotes()
    {
        SetActiveContent(shopEmoteContent);
    }

    public void ShowOwnedEmotes()
    {
        SetActiveContent(ownedEmoteContent);
    }

    #endregion

    #region Selection Handlers

    private void OnCharacterClicked(CharacterTemplate template)
    {
        if (template == null || template.AssociatedCharacter == null)
            return;

        selectedCharacter = template.AssociatedCharacter as CubePlayerBase;
        selectedEmote = null; // clear emote selection

        if (selectedCharacter == null)
        {
            Debug.LogWarning("AssociatedCharacter is not a CubePlayerBase.");
            return;
        }

        if (CharDetails != null)
            CharDetails.SetActive(true);

        Name.text = selectedCharacter._Name;
        Description.text = selectedCharacter.Description;
        image.sprite = selectedCharacter.image;
        Difficulty.text = selectedCharacter.Difficulty + "/5 Difficulty";
        Cost.text = selectedCharacter.Cost + " Cubes";
        Health.text = selectedCharacter.Health + " HP";
        Speed.text = selectedCharacter.Speed + " Speed";
        Stamina.text = selectedCharacter.Stamina + " Stamina";
    }

    private void OnEmoteClicked(EmoteTemplate template)
    {
        if (template == null || template.AssociatedEmote == null)
            return;

        selectedEmote = template.AssociatedEmote;
        selectedCharacter = null; // clear character selection

        if (CharDetails != null)
            CharDetails.SetActive(true);

        Name.text = selectedEmote.emoteName;
        Description.text = selectedEmote.Description;
        image.sprite = selectedEmote.image;
        Cost.text = selectedEmote.cost + " Cubes";

        // Clear character-only stats when showing an emote
        Difficulty.text = string.Empty;
        Health.text = string.Empty;
        Speed.text = string.Empty;
        Stamina.text = string.Empty;
    }

    #endregion

    #region Purchasing

    public void PurchaseSelected()
    {
        if (selectedCharacter == null && selectedEmote == null)
        {
            Debug.LogWarning("No character or emote selected");
            return;
        }

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            inv =>
            {
                // Work out which item we're trying to buy
                string itemId;
                int cost;

                if (selectedCharacter != null)
                {
                    itemId = selectedCharacter._PlayFabItemID;
                    cost = selectedCharacter.Cost;
                }
                else
                {
                    itemId = selectedEmote.PlayFabItemId;
                    cost = selectedEmote.cost;
                }

                bool alreadyOwned = inv.Inventory.Exists(i => i.ItemId == itemId);

                if (purchaseButton != null)
                    purchaseButton.interactable = !alreadyOwned;

                if (alreadyOwned)
                {
                    Debug.Log("Player already owns " + itemId);
                    return;
                }

                var request = new PurchaseItemRequest
                {
                    CatalogVersion = "shop",
                    ItemId = itemId,
                    Price = cost,
                    VirtualCurrency = "CC"
                };

                PlayFabClientAPI.PurchaseItem(request, OnPurchaseSuccess, OnPurchaseError);
            },
            OnPurchaseError);
    }

    private void OnPurchaseSuccess(PurchaseItemResult result)
    {
        Debug.Log("Purchase successful!");
        FindFirstObjectByType<PlayfabManager>()?.GetVirtualCurrencies();
        if (purchaseButton != null)
        {
            purchaseButton.interactable = false;
        }

        // 🔹 Fire the event only if a character was purchased
        if (selectedCharacter != null)
        {
            OnCharacterPurchased?.Invoke(selectedCharacter);
        }

        // Rebuild shop UI so the newly bought item moves to the owned grid
        RefreshShopFromInventory();
    }

    private void OnPurchaseError(PlayFabError error)
    {
        Debug.LogError("Purchase failed: " + error.GenerateErrorReport());
    }

    #endregion
}
