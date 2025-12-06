using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PosePreset : MonoBehaviour
{
    [Serializable]
    public class EnemyPoseTarget
    {
        public Transform target;

        public bool enabled = true;
    }
    public enum PoseMask
    {
        All = 0,
        Top = 1
    }



    public List<EnemyPoseTarget> targets;
    public Transform Hip;
    public Transform Head;
    public PoseMask Mask;
    public bool LetHeadMovement;


    public Transform GetPose(int number)
    {

        if (targets[number].enabled)
        {

            return targets[number].target;
        }
        return null;
    }
}
