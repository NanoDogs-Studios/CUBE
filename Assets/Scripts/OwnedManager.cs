using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OwnedManager : MonoBehaviour
{
    public GameObject ownedKillerContent;
    public GameObject ownedSurvivorContent;
    public GameObject characterPrefab;

    private List<CubePlayerBase> ownedCharacters = new List<CubePlayerBase>();

    private CubePlayerBase equippedCharacter;

    private ShopManager shopManager;

    private void Start()
    {
        shopManager = GetComponent<ShopManager>();
    }

    private void OnEnable()
    {
        shopManager.OnCharacterPurchased += HandleCharacterPurchased;
    }

    private void OnDisable()
    {
        shopManager.OnCharacterPurchased -= HandleCharacterPurchased;
    }

    private void HandleCharacterPurchased(CubePlayerBase purchased)
    {
        Debug.Log("We bought: " + purchased._Name);
        ownedCharacters.Add(purchased);

        foreach (CubePlayerBase character in ownedCharacters)
        {
            if (character is KillerType killer)
            {
                // it's a killer
                GameObject killerObj = Instantiate(characterPrefab, ownedKillerContent.transform);
                killerObj.GetComponent<CharacterTemplate>().AssociatedCharacter = character;
            }
            else if (character is SurvivorType survivor)
            {
                // it's a survivor
                GameObject survivorObj = Instantiate(characterPrefab, ownedSurvivorContent.transform);
                survivorObj.GetComponent<CharacterTemplate>().AssociatedCharacter = character;
            }
        }
    }
}
