using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmoteTemplate : MonoBehaviour
{
    public EmoteAsset AssociatedEmote;

    public Image image;
    public TMP_Text CharName;

    private void Update()
    {
        if (AssociatedEmote != null)
        {
            image.sprite = AssociatedEmote.image;
            CharName.text = AssociatedEmote.emoteName;
        }
    }
}
