using UnityEngine;
using UnityEngine.EventSystems;
public class ButtonSounds : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public AudioClip click;
    public AudioClip hoverenter;
    public AudioClip hoverexit;
    
    public void OnPointerClick(PointerEventData pointerEventData)
    {
        //Use this to tell when the user left-clicks on the Button
        if (pointerEventData.button == PointerEventData.InputButton.Left)
        {
            Debug.Log(name + " Game Object Left Clicked!");
            AudioSource.PlayClipAtPoint(click, this.transform.position);
        }
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        AudioSource.PlayClipAtPoint(hoverenter, this.transform.position);
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        AudioSource.PlayClipAtPoint(hoverexit, this.transform.position);
    }
}