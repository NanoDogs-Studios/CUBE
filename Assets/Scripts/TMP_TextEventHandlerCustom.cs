using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TMP_TextEventHandler : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    public class LinkSelectionEvent : UnityEngine.Events.UnityEvent<string, string, int> { }

    public LinkSelectionEvent onLinkSelection = new LinkSelectionEvent();

    private TMP_Text textComponent;
    private Camera uiCamera;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();

        // Screen Space - Overlay: camera can be null.
        // Screen Space - Camera / World Space: use canvas worldCamera.
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (textComponent == null) return;

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textComponent, eventData.position, uiCamera);
        if (linkIndex == -1) return;

        TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();
        string linkText = linkInfo.GetLinkText();

        onLinkSelection.Invoke(linkId, linkText, linkIndex);
    }
}
