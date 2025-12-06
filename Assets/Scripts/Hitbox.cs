using Photon.Pun;
using UnityEngine;

[ExecuteInEditMode]
public class Hitbox : MonoBehaviour
{
    public GameObject lastHit;
    public int Damage = 10;

    private void Start()
    {
        // Destroy the hitbox after 0.2 seconds to prevent lingering
        Destroy(gameObject, 0.2f);
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            Debug.Log("Hitbox hit something that is not a player: " + other.name);
            return;
        }

        lastHit = other.gameObject;
        string nameHit = lastHit.transform.parent.parent.parent.name;

        if (!nameHit.Contains("BaseCharacter"))
        {
            Debug.Log("Hit something else: " + lastHit.name);
            return;
        }

        GameObject player = lastHit.transform.parent.parent.parent.gameObject;
        var bp = player.GetComponent<BasePlayer>();

        if (bp.GetPlayerType() == BasePlayer.PlayerType.Killer)
        {
            Debug.Log("Hit a killer, ignoring.");
            // Do NOT destroy here, just return so hitbox keeps existing
            return;
        }

        // Otherwise deal damage to survivors
        player.GetPhotonView().RPC("TakeDamage", RpcTarget.AllBuffered, Damage);
        Debug.Log("Dealt " + Damage + " damage to " + nameHit);

        // Optionally destroy the hitbox after hitting a valid target
        Destroy(gameObject);
    }
}
