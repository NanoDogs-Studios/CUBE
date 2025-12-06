using PlayFab;
using PlayFab.ClientModels;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    public GameObject shop;
    public InputActionReference toggleShop;

    public GameObject shopKillerContent;
    public GameObject shopSurvivorContent;
    public GameObject characterPrefab;

    public KillerType[] killers;
    public SurvivorType[] survivors;

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

    // 🔹 Public event so other scripts can subscribe
    public event Action<CubePlayerBase> OnCharacterPurchased;

    private void Start()
    {
        toggleShop.action.performed += ShopTriggered;

        shop.SetActive(false);

        foreach (KillerType killer in killers)
        {
            GameObject charKiller = Instantiate(characterPrefab, shopKillerContent.transform);
            CharacterTemplate template = charKiller.GetComponent<CharacterTemplate>();
            template.AssociatedCharacter = killer;

            charKiller.GetComponent<Button>().onClick.AddListener(() =>
                OnCharacterClicked(template)
            );
        }

        foreach (SurvivorType survivor in survivors)
        {
            GameObject charSurvivor = Instantiate(characterPrefab, shopSurvivorContent.transform);
            CharacterTemplate template = charSurvivor.GetComponent<CharacterTemplate>();
            template.AssociatedCharacter = survivor;

            charSurvivor.GetComponent<Button>().onClick.AddListener(() =>
                OnCharacterClicked(template)
            );
        }
    }

    private void ShopTriggered(InputAction.CallbackContext context)
    {
        shop.SetActive(!shop.activeInHierarchy);
        CursorManager.SetState(!shop.activeInHierarchy);
    }

    private void OnCharacterClicked(CharacterTemplate template)
    {
        selectedCharacter = template.AssociatedCharacter as CubePlayerBase;

        CharDetails.SetActive(true);
        Name.text = selectedCharacter._Name;
        Description.text = selectedCharacter.Description;
        image.sprite = selectedCharacter.image;
        Difficulty.text = selectedCharacter.Difficulty + "/5 Difficulty";
        Cost.text = selectedCharacter.Cost + " Cubes";
        Health.text = selectedCharacter.Health + " HP";
        Speed.text = selectedCharacter.Speed + " Speed";
        Stamina.text = selectedCharacter.Stamina + " Stamina";

        // optionally check inventory here to update purchase button
    }

    public void PurchaseSelected()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("No character selected");
            return;
        }

        PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(),
            inv =>
            {
                bool alreadyOwned = inv.Inventory.Exists(i => i.ItemId == selectedCharacter._PlayFabItemID);

                purchaseButton.interactable = !alreadyOwned;

                if (alreadyOwned)
                {
                    Debug.Log("Player already owns " + selectedCharacter._PlayFabItemID);
                    return;
                }

                var request = new PurchaseItemRequest
                {
                    CatalogVersion = "shop",
                    ItemId = selectedCharacter._PlayFabItemID,
                    Price = selectedCharacter.Cost,
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
        purchaseButton.interactable = false;

        // 🔹 Fire the event so subscribers know which character was purchased
        OnCharacterPurchased?.Invoke(selectedCharacter);
    }

    private void OnPurchaseError(PlayFabError error)
    {
        Debug.LogError("Purchase failed: " + error.GenerateErrorReport());
    }
}
