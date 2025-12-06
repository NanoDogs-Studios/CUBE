using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    public float After;
    private void Awake()
    {
        Destroy(gameObject, After);
    }
    
}
