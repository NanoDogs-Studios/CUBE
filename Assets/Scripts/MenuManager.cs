using Photon.Pun;
using UnityEngine;
using WebSocketSharp;

public class MenuManager : MonoBehaviour
{
    public GameObject usernameSetter;
    public LevelLoader levelLoader;

    public Sprite cusor;

    private void Start()
    {
        Cursor.SetCursor(cusor.texture, Vector2.zero, CursorMode.Auto);
    }

    public void Play()
    {
        //if (string.IsNullOrEmpty(PlayerPrefs.GetString("Username")))
        //{
           // Debug.Log("Username is empty");
           // usernameSetter.SetActive(true);
           // return;
        //}
        //else
       // {
           // usernameSetter.SetActive(false);
            levelLoader.LoadLevel("SampleScene");
       // }
    }
}
