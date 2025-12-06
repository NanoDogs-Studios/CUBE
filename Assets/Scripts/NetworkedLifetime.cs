using Photon.Pun;
using UnityEngine;

public class NetworkedLifetime : MonoBehaviour
{
    public float Lifetime = 0.1f; // default

    void Start()
    {
        if (Lifetime > 0f)
            Invoke(nameof(DestroySelf), Lifetime);
    }

    void DestroySelf()
    {
        // Only the owner/master client should call PhotonNetwork.Destroy for networked objects,
        // but Photon will ignore destroy from non-owners in many setups. If you prefer, you
        // can use RPC to have master destroy. For basic setups PhotonNetwork.Destroy is fine.
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Destroy(gameObject);
        else
            Destroy(gameObject);
    }
}
