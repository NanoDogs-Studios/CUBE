using Photon.Pun;
using UnityEngine;

[CreateAssetMenu(fileName = "HealCircle", menuName = "ScriptableObjects/Survivor Abilities/HealCircle", order = 1)]
public class HealCircle : Ability
{
    public override void ActivateAbility(BasePlayer player)
    {
        GameObject healCircle = PhotonNetwork.Instantiate("HealCirclePrefab", player.transform.Find("RIG").Find("Foot_Right").transform.position, Quaternion.identity);
        healCircle.GetComponent<HealCircleFunction>().creator = player;
    }
}
