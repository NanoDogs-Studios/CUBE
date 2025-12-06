using TMPro;
using UnityEngine;

public class LevelLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public TMP_Text percent;

    public void LoadLevel(string levelName)
    {
        StartCoroutine(LoadAsynchronously(levelName));
    }

    System.Collections.IEnumerator LoadAsynchronously(string levelName)
    {
        AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(levelName);
        loadingScreen.SetActive(true);
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            percent.text = (progress * 100).ToString("F0") + "%";
            Debug.Log("Loading progress: " + (progress * 100) + "%");
            yield return null;
        }
    }
}
