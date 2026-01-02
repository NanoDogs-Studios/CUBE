using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class OverclockedMindRunner : PassiveRunner
{
    public float range = 10f;

    // Track who is currently affected (by PhotonViewID)
    private readonly HashSet<int> affected = new HashSet<int>();

    private void SendToggle(PhotonView targetPV, bool on)
    {
        // RPC method is on BasePlayer (exists on all players)
        targetPV.RPC(nameof(PlayerRPCHandler.RPC_ApplyClientEffect), targetPV.Owner,
            (byte)ClientEffectId.UnstablePresence, on, 1f);
    }

    public override void Tick()
    {
        base.Tick();

        // who is in range this tick
        var inRangeNow = new HashSet<int>();

        foreach (var collider in Physics.OverlapSphere(transform.position, range))
        {
            var survivor = collider.GetComponentInParent<BasePlayer>();
            if (survivor == null) continue;
            if (survivor.playerType != BasePlayer.PlayerType.Survivor) continue;

            var pv = survivor.photonView;
            if (pv == null) continue;

            inRangeNow.Add(pv.ViewID);

            // ENTER
            if (!affected.Contains(pv.ViewID))
            {
                affected.Add(pv.ViewID);
                SendToggle(pv, true);
            }
        }

        // EXIT
        // remove anyone who was affected but is no longer in range
        // (use temp list because we modify the set)
        var toRemove = new List<int>();
        foreach (var viewId in affected)
            if (!inRangeNow.Contains(viewId))
                toRemove.Add(viewId);

        foreach (var viewId in toRemove)
        {
            affected.Remove(viewId);
            var pv = PhotonView.Find(viewId);
            if (pv != null) SendToggle(pv, false);
        }
    }
}
