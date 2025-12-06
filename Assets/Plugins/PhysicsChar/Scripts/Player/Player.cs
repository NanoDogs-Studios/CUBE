using UnityEngine;

public class Player : MonoBehaviour
{
   public static Player Instance;
    public bool LocalPlayer;

    private void Start()
    {
        if (LocalPlayer)
        {
            Instance = this;
        }
    }
}
