using UnityEngine;
using UnityEngine.UI;

public class AbiltySlider : MonoBehaviour
{
    [Range(0, 100)]
    public float Value { get; private set; }

    public Image AbilityImage { get; private set; }
    public Ability ability;

    public void SetSliderValue(float value)
    {
        Value = value;
    }

    private void Awake()
    {
        AbilityImage = GetComponent<Image>();
    }

    private void Update()
    {
        AbilityImage.fillAmount = Value / 100f;
    }
}
