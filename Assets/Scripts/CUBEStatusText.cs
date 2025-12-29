using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CUBEStatusText : MonoBehaviour
{
    /// <summary>
    /// Creates a new text entry using the specified content.
    /// </summary>
    /// <param name="text">The text content to be used for the new entry. Cannot be null or empty, can include rich text with tmp</param>
    public static GameObject CreateText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("CUBEStatusText: Cannot create text entry with null or empty content.");
            return null;
        }

        GameObject textObj = GameObject.Instantiate(Resources.Load<GameObject>("CUBEStatusTextPrefab"), GameObject.Find("Canvas").transform);
        TMP_Text tmpTextComponent = textObj.GetComponent<TMP_Text>();
        if (tmpTextComponent == null)
        {
            Debug.LogWarning("CUBEStatusText: The prefab does not contain a TMP_Text component.");
            GameObject.Destroy(textObj);
            return null;
        }
        else
        {
            tmpTextComponent.text = text;
            Debug.Log($"CUBEStatusText: Created text entry with content: {text}");
            return textObj;
        }
    }

    public static GameObject FadeInAndCreate(string text, float duration)
    {
        GameObject textObj = CreateText(text);
        if (textObj == null) return null;
        TMP_Text tmpTextComponent = textObj.GetComponent<TMP_Text>();
        if (tmpTextComponent == null)
        {
            Debug.LogWarning("CUBEStatusText: The created GameObject does not contain a TMP_Text component.");
            return null;
        }
        CanvasGroup cg = textObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        textObj.GetComponent<MonoBehaviour>().StartCoroutine(FadeInCoroutine(cg, duration));
        return textObj;
    }

    private static IEnumerator FadeInCoroutine(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
    }

    public static void FadeOutAndDestroy(GameObject textObj, float duration)
    {
        if (textObj == null)
        {
            Debug.LogWarning("CUBEStatusText: Cannot fade out and destroy a null GameObject.");
            return;
        }
        TMP_Text tmpTextComponent = textObj.GetComponent<TMP_Text>();
        if (tmpTextComponent == null)
        {
            Debug.LogWarning("CUBEStatusText: The provided GameObject does not contain a TMP_Text component.");
            return;
        }

        CanvasGroup cg = textObj.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        textObj.GetComponent<MonoBehaviour>().StartCoroutine(FadeOutCoroutine(cg, duration, textObj));
    }

    private static IEnumerator FadeOutCoroutine(CanvasGroup cg, float duration, GameObject textObj)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        GameObject.Destroy(textObj);
    }
}
