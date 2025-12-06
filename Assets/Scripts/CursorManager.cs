using UnityEngine;

public class CursorManager : MonoBehaviour
{
    private void Start()
    {
        SetLocked();
    }

    public static void SetState(bool locked)
    {
        if (locked)
        { SetLocked(); }
        else
        { SetVisible(); }
    }

    public static void SetLocked()
    {
        Camera.main.transform.parent.GetComponent<CameraInput>().enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false ;
    }

    public static void SetVisible()
    {
        Camera.main.transform.parent.GetComponent<CameraInput>().enabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
