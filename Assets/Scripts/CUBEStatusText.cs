using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CUBEStatusText : MonoBehaviour
{
    private static CUBEStatusText instance;

    private void Awake()
    {
        instance = this;
    }

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
        CanvasGroup cg = textObj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = textObj.AddComponent<CanvasGroup>();
        }

        cg.alpha = 0f;
        instance.StartCoroutine(FadeInCoroutine(cg, duration));
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
        if (textObj == null) return;

        // Get existing CanvasGroup or add one
        var cg = textObj.GetComponent<CanvasGroup>();
        if (cg == null) cg = textObj.AddComponent<CanvasGroup>();

        // Make sure it can actually block the UI alpha
        cg.alpha = 1f;

        instance.StartCoroutine(FadeOutCoroutine(cg, duration, textObj));
    }

    private static IEnumerator FadeOutCoroutine(CanvasGroup cg, float duration, GameObject textObj)
    {
        if (cg == null || textObj == null) yield break;

        float startAlpha = cg.alpha;
        float t = 0f;

        while (t < duration && textObj != null)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(t / duration));
            yield return null;
        }

        if (textObj != null)
            UnityEngine.Object.Destroy(textObj);
    }
}
