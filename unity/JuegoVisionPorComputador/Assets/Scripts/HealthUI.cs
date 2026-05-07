using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider healthSlider;         // Slider de Unity (arrastra aquí)
    public Image fillImage;             // La imagen de relleno del slider

    [Header("Colores")]
    public Color colorFull = Color.green;
    public Color colorMid = Color.yellow;
    public Color colorLow = Color.red;

    // Umbrales (porcentaje de vida)
    [Range(0f, 1f)] public float midThreshold = 0.5f;   // Bajo del 50% → amarillo
    [Range(0f, 1f)] public float lowThreshold = 0.25f;  // Bajo del 25% → rojo

    public void UpdateUI(float currentHP, float maxHP)
    {
        float ratio = currentHP / maxHP;

        // Actualiza el slider
        healthSlider.value = ratio;

        // Cambia el color según la vida restante
        if (fillImage != null)
        {
            if (ratio > midThreshold)
                fillImage.color = colorFull;
            else if (ratio > lowThreshold)
                fillImage.color = colorMid;
            else
                fillImage.color = colorLow;
        }
    }
}