using Photon.Pun;
using System;
using UnityEngine;

public class HealCircleFunction : MonoBehaviourPunCallbacks
{
    public BasePlayer creator;
    public float healAmount = 2f;
    public float healInterval = 1f;

    private void Start()
    {
        InvokeRepeating(nameof(HealSurvivors), healInterval, healInterval);
        Destroy(gameObject, 15f); // Heal circle lasts for 15 seconds
    }

    public void HealSurvivors()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 10f);
        foreach (var hitCollider in hitColliders)
        {
            BasePlayer player = hitCollider.GetComponent<BasePlayer>();
            if (player != null && player.GetPlayerType() == BasePlayer.PlayerType.Survivor)
            {
                photonView.RPC("ApplyHeal", RpcTarget.All, player.gameObject.GetPhotonView().ViewID, healAmount);
            }
        }
    }
}