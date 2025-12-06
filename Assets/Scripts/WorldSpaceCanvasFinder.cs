using UnityEngine;

public class WorldSpaceCanvasFinder : MonoBehaviour
{
    private Camera cam;

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        else
        {
            this.GetComponent<Canvas>().worldCamera = cam;
        }
    }
}
