using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UsernameSetter : MonoBehaviour
{
    public TMP_InputField userNameField;
    
    public void SetNameUsingInputField()
    {
        SetName(userNameField.text);
    }


    void SetName(string name)
    {
        if(string.IsNullOrEmpty(name))
        {
            Debug.Log("Username is empty");
            return;
        }
        else
        {
            PlayerPrefs.SetString("Username", name);
            PhotonNetwork.NickName = name;
            Debug.Log("Username set to: " + name);
            transform.gameObject.SetActive(false);
        }
    }
}
