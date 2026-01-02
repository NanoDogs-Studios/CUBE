// ShopManager (survivor visibility debug + fix)
//
// What this version adds:
// 1) Loud debug logging so you can instantly see WHY survivors aren't showing
//    - survivors array count
//    - whether shopSurvivorContent / ownedSurvivorContent are assigned
//    - how many survivor entries were instantiated
// 2) A safer SetActiveContent() that also ensures the TARGET's parents are active
//    (common Unity UI gotcha: the child is set active but an inactive parent keeps it invisible)
// 3) Optional: Default tab can be set in inspector (Killers/Survivors/Emotes)
//
// Most common causes of "I can't see survivors":
// - survivors[] not assigned / empty
// - shopSurvivorContent reference is null or points to the WRONG object
// - the survivor panel is under an inactive parent (tab root, scroll view, etc.)
// - your UI buttons aren't calling ShowShopSurvivors()

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
    public enum DefaultTab
    {
        ShopKillers,
        ShopSurvivors,
        ShopEmotes,
        OwnedKillers,
        OwnedSurvivors,
        OwnedEmotes
    }

    [Header("References")]
    public GameObject shop;
    public InputActionReference toggleShop;

    [Header("Shop Grids")]
    public GameObject shopKillerContent;
    public GameObject shopSurvivorContent;
    public GameObject shopEmoteContent;

    [Header("Owned Grids")]
    public GameObject ownedKillerContent;
    public GameObject ownedSurvivorContent;
    public GameObject ownedEmoteContent;

    [Header("Entry Prefabs")]
    public GameObject characterPrefab;
    public GameObject emotePrefab;

    [Header("Data")]
    public KillerType[] killers;
    public SurvivorType[] survivors;
    public EmoteAsset[] emotes;

    [Header("PlayFab")]
    public string catalogVersion = "shop";
    public string virtualCurrencyCode = "CC";

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

    [Header("Behavior")]
    public DefaultTab defaultTab = DefaultTab.ShopKillers;

    private CubePlayerBase selectedCharacter;
    private EmoteAsset selectedEmote;
    private HashSet<string> ownedItemIds = new HashSet<string>();

    public event Action<CubePlayerBase> OnCharacterPurchased;
    public event Action<EmoteAsset> OnEmotePurchased;

    private void OnEnable()
    {
        if (toggleShop != null)
            toggleShop.action.performed += ShopTriggered;

        if (purchaseButton != null)
        {
            // Prevent duplicate listeners (e.g., if this component is enabled/disabled or
            // if you also wired the button in the Inspector)
            purchaseButton.onClick.RemoveListener(PurchaseSelected);
            purchaseButton.onClick.AddListener(PurchaseSelected);
        }

        if (PlayfabManager.Instance != null)
            PlayfabManager.Instance.OnCubeCoinsChanged += HandleCoinsChanged;
    }

    private void OnDisable()
    {
        if (toggleShop != null)
            toggleShop.action.performed -= ShopTriggered;

        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(PurchaseSelected);

        if (PlayfabManager.Instance != null)
            PlayfabManager.Instance.OnCubeCoinsChanged -= HandleCoinsChanged;
    }

    private bool purchaseInFlight;

    private void Start()
    {
        if (shop != null) shop.SetActive(false);
        if (CharDetails != null) CharDetails.SetActive(false);
        RefreshShopFromInventory();
    }

    private void HandleCoinsChanged(int _)
    {
        UpdatePurchaseButtonState();
    }

    private void ShopTriggered(InputAction.CallbackContext context)
    {
        if (shop == null) return;

        bool newState = !shop.activeInHierarchy;
        shop.SetActive(newState);

        if (newState)
            RefreshShopFromInventory();

        CursorManager.SetState(!newState);
    }

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
                Debug.LogError("[Shop] Failed to get inventory: " + error.GenerateErrorReport());
                ownedItemIds.Clear();
                BuildShopUI();
            });
    }

    private void BuildShopUI()
    {
        // --- DEBUG SUMMARY ---
        Debug.Log($"[Shop] BuildShopUI()" +
                  $" | killers={(killers == null ? -1 : killers.Length)}" +
                  $" | survivors={(survivors == null ? -1 : survivors.Length)}" +
                  $" | emotes={(emotes == null ? -1 : emotes.Length)}" +
                  $" | shopSurvivorContent={(shopSurvivorContent ? shopSurvivorContent.name : "NULL")}" +
                  $" | ownedSurvivorContent={(ownedSurvivorContent ? ownedSurvivorContent.name : "NULL")}");

        ClearChildren(shopKillerContent?.transform);
        ClearChildren(ownedKillerContent?.transform);
        ClearChildren(shopSurvivorContent?.transform);
        ClearChildren(ownedSurvivorContent?.transform);
        ClearChildren(shopEmoteContent?.transform);
        ClearChildren(ownedEmoteContent?.transform);

        selectedCharacter = null;
        selectedEmote = null;
        if (CharDetails != null) CharDetails.SetActive(false);

        int spawnedKillers = 0;
        int spawnedSurvivors = 0;
        int spawnedEmotes = 0;

        // Killers
        if (killers != null)
        {
            foreach (var killer in killers)
            {
                if (killer == null) continue;

                bool isOwned = !string.IsNullOrEmpty(killer._PlayFabItemID) && ownedItemIds.Contains(killer._PlayFabItemID);
                Transform parent = (isOwned ? ownedKillerContent : shopKillerContent)?.transform;
                if (parent == null)
                {
                    Debug.LogWarning("[Shop] Killer parent is null (check shopKillerContent/ownedKillerContent assignments)");
                    continue;
                }

                var entry = Instantiate(characterPrefab, parent);
                if (!TryWireCharacterEntry(entry, killer))
                    Destroy(entry);
                else
                    spawnedKillers++;
            }
        }

        // Survivors
        if (survivors != null)
        {
            foreach (var survivor in survivors)
            {
                if (survivor == null) continue;

                bool isOwned = !string.IsNullOrEmpty(survivor._PlayFabItemID) && ownedItemIds.Contains(survivor._PlayFabItemID);
                Transform parent = (isOwned ? ownedSurvivorContent : shopSurvivorContent)?.transform;

                if (parent == null)
                {
                    Debug.LogWarning("[Shop] Survivor parent is null. You likely didn't assign shopSurvivorContent and/or ownedSurvivorContent in the inspector.");
                    continue;
                }

                var entry = Instantiate(characterPrefab, parent);
                if (!TryWireCharacterEntry(entry, survivor))
                    Destroy(entry);
                else
                    spawnedSurvivors++;
            }
        }

        // Emotes
        if (emotes != null)
        {
            foreach (var asset in emotes)
            {
                if (asset == null) continue;

                bool isOwned = !string.IsNullOrEmpty(asset.PlayFabItemId) && ownedItemIds.Contains(asset.PlayFabItemId);
                Transform parent = (isOwned ? ownedEmoteContent : shopEmoteContent)?.transform;
                if (parent == null)
                {
                    Debug.LogWarning("[Shop] Emote parent is null (check shopEmoteContent/ownedEmoteContent assignments)");
                    continue;
                }

                var prefab = (emotePrefab != null) ? emotePrefab : characterPrefab;
                var entry = Instantiate(prefab, parent);
                if (!TryWireEmoteEntry(entry, asset))
                    Destroy(entry);
                else
                    spawnedEmotes++;
            }
        }

        Debug.Log($"[Shop] Spawned entries -> Killers:{spawnedKillers} Survivors:{spawnedSurvivors} Emotes:{spawnedEmotes}");

        // Default active tab
        ApplyDefaultTab();

        UpdatePurchaseButtonState();
    }

    private void ApplyDefaultTab()
    {
        switch (defaultTab)
        {
            case DefaultTab.ShopKillers: SetActiveContent(shopKillerContent); break;
            case DefaultTab.OwnedKillers: SetActiveContent(ownedKillerContent); break;
            case DefaultTab.ShopSurvivors: SetActiveContent(shopSurvivorContent); break;
            case DefaultTab.OwnedSurvivors: SetActiveContent(ownedSurvivorContent); break;
            case DefaultTab.ShopEmotes: SetActiveContent(shopEmoteContent); break;
            case DefaultTab.OwnedEmotes: SetActiveContent(ownedEmoteContent); break;
        }
    }

    private bool TryWireCharacterEntry(GameObject entry, CubePlayerBase data)
    {
        if (entry == null || data == null) return false;

        var button = entry.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[Shop] Entry prefab missing Button", entry);
            return false;
        }

        var template = entry.GetComponent<CharacterTemplate>();
        if (template == null)
        {
            Debug.LogError("[Shop] Character entry prefab missing CharacterTemplate", entry);
            return false;
        }

        template.AssociatedCharacter = data;
        button.onClick.AddListener(() => OnCharacterClicked(template));
        return true;
    }

    private bool TryWireEmoteEntry(GameObject entry, EmoteAsset data)
    {
        if (entry == null || data == null) return false;

        var button = entry.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("[Shop] Emote entry prefab missing Button", entry);
            return false;
        }

        var template = entry.GetComponent<EmoteTemplate>();
        if (template == null)
        {
            Debug.LogError("[Shop] Emote entry prefab missing EmoteTemplate", entry);
            return false;
        }

        template.AssociatedEmote = data;
        button.onClick.AddListener(() => OnEmoteClicked(template));
        return true;
    }

    private void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    // --- TAB VISIBILITY ---

    private void SetActiveContent(GameObject target)
    {
        GameObject[] all =
        {
            shopKillerContent,
            ownedKillerContent,
            shopSurvivorContent,
            ownedSurvivorContent,
            shopEmoteContent,
            ownedEmoteContent
        };

        // Disable all
        foreach (var go in all)
            if (go != null) go.SetActive(false);

        if (target == null) return;

        // Ensure all parents are active (common issue: parent panel/scrollview disabled)
        EnsureParentsActive(target.transform);

        // Enable target
        target.SetActive(true);
    }

    private void EnsureParentsActive(Transform t)
    {
        // Walk up until root canvas, enabling parents.
        // If your UI structure has dedicated tab roots, this guarantees visibility.
        var cur = t;
        while (cur != null)
        {
            if (!cur.gameObject.activeSelf)
                cur.gameObject.SetActive(true);

            // Stop at Canvas
            if (cur.GetComponent<Canvas>() != null)
                break;

            cur = cur.parent;
        }
    }

    public void ShowShopKillers() => SetActiveContent(shopKillerContent);
    public void ShowOwnedKillers() => SetActiveContent(ownedKillerContent);
    public void ShowShopSurvivors() => SetActiveContent(shopSurvivorContent);
    public void ShowOwnedSurvivors() => SetActiveContent(ownedSurvivorContent);
    public void ShowShopEmotes() => SetActiveContent(shopEmoteContent);
    public void ShowOwnedEmotes() => SetActiveContent(ownedEmoteContent);

    // --- SELECTION ---

    private void OnCharacterClicked(CharacterTemplate template)
    {
        if (template == null || template.AssociatedCharacter == null) return;

        selectedCharacter = template.AssociatedCharacter as CubePlayerBase;
        selectedEmote = null;

        if (selectedCharacter == null)
        {
            Debug.LogWarning("[Shop] AssociatedCharacter is not a CubePlayerBase");
            return;
        }

        if (CharDetails != null) CharDetails.SetActive(true);

        if (Name != null) Name.text = selectedCharacter._Name;
        if (Description != null) Description.text = selectedCharacter.Description;
        if (image != null) image.sprite = selectedCharacter.image;

        if (Difficulty != null) Difficulty.text = selectedCharacter.Difficulty + "/5 Difficulty";
        if (Cost != null) Cost.text = selectedCharacter.Cost + " Cubes";
        if (Health != null) Health.text = selectedCharacter.Health + " HP";
        if (Speed != null) Speed.text = selectedCharacter.Speed + " Speed";
        if (Stamina != null) Stamina.text = selectedCharacter.Stamina + " Stamina";

        UpdatePurchaseButtonState();
    }

    private void OnEmoteClicked(EmoteTemplate template)
    {
        if (template == null || template.AssociatedEmote == null) return;

        selectedEmote = template.AssociatedEmote;
        selectedCharacter = null;

        if (CharDetails != null) CharDetails.SetActive(true);

        if (Name != null) Name.text = selectedEmote.emoteName;
        if (Description != null) Description.text = selectedEmote.Description;
        if (image != null) image.sprite = selectedEmote.image;
        if (Cost != null) Cost.text = selectedEmote.cost + " Cubes";

        if (Difficulty != null) Difficulty.text = string.Empty;
        if (Health != null) Health.text = string.Empty;
        if (Speed != null) Speed.text = string.Empty;
        if (Stamina != null) Stamina.text = string.Empty;

        UpdatePurchaseButtonState();
    }

    private void UpdatePurchaseButtonState()
    {
        if (purchaseButton == null) return;

        if (selectedCharacter == null && selectedEmote == null)
        {
            purchaseButton.interactable = false;
            return;
        }

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

        bool owned = !string.IsNullOrEmpty(itemId) && ownedItemIds.Contains(itemId);

        bool canAfford = true;
        if (PlayfabManager.Instance != null)
            canAfford = PlayfabManager.Instance.CachedCC >= cost;

        purchaseButton.interactable = !owned && canAfford;
    }

    // --- PURCHASING (unchanged logic) ---

    public void PurchaseSelected()
    {
        if (purchaseInFlight)
        {
            Debug.Log("[Shop] Purchase ignored: request already in flight");
            return;
        }
        if (selectedCharacter == null && selectedEmote == null)
        {
            Debug.LogWarning("[Shop] No selection");
            return;
        }

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

        if (string.IsNullOrEmpty(itemId))
        {
            Debug.LogError("[Shop] Selected item has no PlayFab ItemId");
            return;
        }

        if (ownedItemIds.Contains(itemId))
        {
            Debug.Log("[Shop] Already owned: " + itemId);
            UpdatePurchaseButtonState();
            return;
        }

        if (PlayfabManager.Instance != null && PlayfabManager.Instance.CachedCC < cost)
        {
            Debug.Log("[Shop] Not enough CC to buy: " + itemId);
            UpdatePurchaseButtonState();
            return;
        }

        purchaseInFlight = true;

        if (purchaseButton != null) purchaseButton.interactable = false;

        var request = new PurchaseItemRequest
        {
            CatalogVersion = catalogVersion,
            ItemId = itemId,
            Price = cost,
            VirtualCurrency = virtualCurrencyCode
        };

        PlayFabClientAPI.PurchaseItem(request,
            result => OnPurchaseSuccess(result, itemId),
            error => OnPurchaseError(error, itemId));
    }

    private void OnPurchaseSuccess(PurchaseItemResult result, string itemId)
    {
        purchaseInFlight = false;
        {
            Debug.Log("[Shop] Purchase successful: " + itemId);
            CUBEStatusText.FadeOutAndDestroy(CUBEStatusText.CreateText("Successfully purchased!"), 1f);

            ownedItemIds.Add(itemId);

            if (PlayfabManager.Instance != null)
                PlayfabManager.Instance.RefreshCubeCoins();

            if (selectedCharacter != null) OnCharacterPurchased?.Invoke(selectedCharacter);
            else if (selectedEmote != null) OnEmotePurchased?.Invoke(selectedEmote);

            RefreshShopFromInventory();
        }
    }

    private void OnPurchaseError(PlayFabError error, string itemId)
    {
        purchaseInFlight = false;
        {
            Debug.LogError("[Shop] Purchase failed (" + itemId + "): " + error.GenerateErrorReport());
            Debug.Log("[Shop] PlayFab error code: " + error.Error);

            CUBEStatusText.FadeOutAndDestroy(CUBEStatusText.CreateText("Purchase failed: " + error.ErrorMessage), 1f);
            UpdatePurchaseButtonState();
        }
    }
}