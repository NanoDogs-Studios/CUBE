using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterTemplate : MonoBehaviour
{
    public CubePlayerBase AssociatedCharacter;

    public Image image;
    public TMP_Text CharName;

    private void Update()
    {
        if (AssociatedCharacter != null)
        {
            image.sprite = AssociatedCharacter.image;
            CharName.text = AssociatedCharacter._Name;
        }
    }
}
