using UnityEngine;

public class PlayerTeleporter : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger entered by: " + other.name);
        if (other.transform.parent.parent.name == "RIG")
        {
            Debug.Log("parent is rig");
            foreach (Transform child in other.transform.parent.parent.GetComponentsInChildren<Transform>())
            {
                Debug.Log("Teleporting child: " + child.name);
                child.position = new Vector3(0, 5, 0);
            }
        }
        else
        {
            Debug.Log("Parent is not rig");
        }
    }
}

