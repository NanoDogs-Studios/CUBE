using Unity.VisualScripting;
using UnityEngine;

public class WeaponPosePreset : PosePreset
{
    [Header("Weapon")]
    public Weapon Weapon;
    public Vector3 PositionOffset;
    public Vector3 RotationOffset;
    public Transform RotationObject;
    public Rigidbody WeaponHolders;


    private void OnDrawGizmos()
    {
        if (WeaponHolders == null || RotationObject == null)
            return;

        Vector3 worldPos = WeaponHolders.transform.position + PositionOffset;


        RotationObject.localRotation = Quaternion.Euler(RotationOffset);
        RotationObject.position = worldPos;

        // Draw gizmos
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(worldPos, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(RotationObject.position, RotationObject.forward * 2f);


        Gizmos.color = Color.red;
        Gizmos.DrawRay(RotationObject.position, RotationObject.right * 1f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(RotationObject.position, RotationObject.up * 1f);
    }
}