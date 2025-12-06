using HSVPicker;
using UnityEngine;

public class PlayerColorer : MonoBehaviour
{
    public ColorPicker picker;

    void Start()
    {
        picker.onValueChanged.AddListener(color =>
        {
            ChangePlayerColor(color);
        });
        ChangePlayerColor(picker.CurrentColor);
    }

    public static void ChangePlayerColor(Color color)
    {
        Material mat = Resources.Load<Material>("PlayerMat");
        mat.SetColor("_MainColor", color);
    }
}
