using Photon.Pun;
using TMPro;
using UnityEngine;

public class PlayerNameSyncer : MonoBehaviourPun
{
    public string Name;
    public TMP_Text nameText;

    private void Start()
    {
        if (photonView.IsMine)
        {
            Name = PlayfabManager.GetPlayerName();
            photonView.RPC("SyncName", RpcTarget.AllBuffered, Name);
        }
    }

    [PunRPC]
    public void SyncName(string name)
    {
        this.Name = name;
        if (nameText != null)
        {
            nameText.text = name;
        }
    }
}