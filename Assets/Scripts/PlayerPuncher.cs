using Photon.Pun;
using System;
using UnityEngine;

public class PlayerPuncher : MonoBehaviour
{
    public BasePlayer player;
    private KillerType killerType;
    private RoundManager roundManager;

    public PoseHandler armHandler;

    private void Start()
    {

        if (armHandler != null)
        {
            armHandler.onArmHandle += Punch;
        }
        roundManager = FindFirstObjectByType<RoundManager>();
        if (roundManager != null)
        {
            roundManager.OnRoundStart += RoundStarted;
        }
    }

    private void RoundStarted()
    {
        killerType = player.GetEquippedKiller();
    }

    private void Punch()
    {
        if (killerType == null) return;

        Vector3 size = killerType.punchHitboxSize;

        Transform hips = player.transform.Find("CameraHead").Find("Cam").Find("C");
        if (hips == null) return;

        // base center is slightly in front of hips
        Vector3 center = hips.position + hips.forward * (size.z / 2f + 0.6f);
        Quaternion yRotation = Quaternion.Euler(0f, hips.eulerAngles.y, 0f);
       
        // new hitbox system
    }
}
