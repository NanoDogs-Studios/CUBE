using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Transform Point;
    public GameObject Particles;
    public BulletType BulletType;


    public void ShootGun()
    {
        Instantiate(Particles, Point.position, Point.rotation);
        transform.parent.GetComponent<Rigidbody>().AddForce(-Point.forward * BulletType.GunRecoil, ForceMode.Impulse);
        RaycastHit hit;
        if (Physics.Raycast(Point.position, FindObjectOfType<Camera>().transform.forward, out hit, Mathf.Infinity))
        {
            Debug.Log(hit.collider.name);
            hit.collider.GetComponentInParent<Rigidbody>().AddForce(Point.forward * BulletType.HitRecoil * 100);
        } else
        {
            Debug.Log("hit Nothing");
        }
        //transform.parent.GetComponent<Rigidbody>().AddTorque(-Point.forward * Recoil, ForceMode.Impulse);
    }




}
[Serializable]
public class BulletType
{
    public GameObject Bullet;
    public float GunRecoil = 120f;
    public float HitRecoil = 20f;
}
