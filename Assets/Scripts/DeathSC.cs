using System.Collections;
using UnityEngine;

public class DeathSC : MonoBehaviour
{
    public BasePlayerStats stats;
    public MonoBehaviour[] scriptsToDisableOnDeath;
    StandingSC standing;
    Rig rig;
    PhotonLauncher photonLauncher;

    bool died = false;

    void Start()
    {
        standing = GetComponent<StandingSC>();
        rig = GetComponent<Rig>();

        photonLauncher = FindFirstObjectByType<PhotonLauncher>();
    }

    void Update()
    {
        if (stats.health <= 0)
        {
            // Only execute once
            if (!died)
            {
                Debug.Log("Player has died.");

                // mark as dead immediately so Update won't call again
                died = true;

                // disable rigs
                foreach (RigWithMultiplier rig in standing.rigToLift)
                {
                    rig.multiplier = 0;
                }
                rig.control = 0;

                // disable scripts
                foreach (MonoBehaviour mb in scriptsToDisableOnDeath)
                {
                    mb.enabled = false;
                }

                // start respawn coroutine once
                StartCoroutine(Respawn());
            }
        }
    }

    IEnumerator Respawn()
    {
        // wait respawn time
        yield return new WaitForSeconds(5f);

        // restore health
        stats.health = 100;

        // re-enable scripts
        foreach (MonoBehaviour mb in scriptsToDisableOnDeath)
        {
            mb.enabled = true;
        }

        // re-enable rigs
        foreach (RigWithMultiplier rig in standing.rigToLift)
        {
            rig.multiplier = 1;
        }
        rig.control = 1;

        // teleport
        Vector3 spawnPos = photonLauncher.roundManager != null
            ? photonLauncher.roundManager.SyncedIntermissionSpawnPos
            : photonLauncher.intermissionSpawn.position;
        Quaternion? spawnRot = photonLauncher.roundManager != null
            ? photonLauncher.roundManager.SyncedIntermissionSpawnRot
            : photonLauncher.intermissionSpawn.rotation;
        photonLauncher.TeleportPlayer(spawnPos, spawnRot);

        // allow death to happen again
        died = false;
    }

}
