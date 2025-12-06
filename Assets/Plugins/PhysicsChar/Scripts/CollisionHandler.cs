using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    public bool TouchGround;
    public float Length;

    private void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, Length))
        {
            Debug.DrawRay(transform.position, Vector3.down * hit.distance, Color.red);
            TouchGround = true;
        } else
        {
            TouchGround = false;
        }
    }
}
