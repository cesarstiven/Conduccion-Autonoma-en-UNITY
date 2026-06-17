using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarHUD : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody carRigidbody;
    public SimpleCarController carController; // Para leer los pedales de la IA

    [Header("UI - Velocidad")]
    public TMP_Text speedText;
    public Image speedArcFill; // <-- ¡Aquí está el arco de la velocidad!

    [Header("UI - Marcha y RPM")]
    public TMP_Text gearText;
    public TMP_Text rpmText;
    public Image rpmBar; // El arco para las RPM

    [Header("UI - Pedales (Assetto Corsa Style)")]
    public Image throttleBar;
    public TMP_Text throttleText;
    public Image brakeBar;
    public TMP_Text brakeText;

    [Header("UI - Freno (Opcional)")]
    public TMP_Text brakeZoneText;
    public Image brakeZoneColor;

    [Header("Configuración")]
    public float maxSpeed = 200f;
    public float maxRPM = 8000f;

    // Colores para las RPM
    private Color colorGreen = new Color(0.11f, 0.62f, 0.46f);
    private Color colorAmber = new Color(0.94f, 0.62f, 0.15f);
    private Color colorRed = new Color(0.89f, 0.29f, 0.29f);

    void Update()
    {
        if (carRigidbody == null) return;

        // 1. LECTURA Y ARCO DE VELOCIDAD
        float speed = carRigidbody.velocity.magnitude * 3.6f;
        if (speedText) speedText.text = Mathf.RoundToInt(speed).ToString();

        // Controla el llenado del arco de velocidad (de 0 a maxSpeed)
        if (speedArcFill)
        {
            speedArcFill.fillAmount = Mathf.Clamp01(speed / maxSpeed);
        }

        // 2. MARCHAS Y RPM (Simulación Visual)
        int gear = 1;
        if (speed > 30) gear = 2;
        if (speed > 60) gear = 3;
        if (speed > 90) gear = 4;
        if (speed > 120) gear = 5;
        if (speed > 150) gear = 6;
        if (gearText) gearText.text = gear.ToString();

        float rpm = 1000f + (speed % 40f) * 80f + gear * 200f;
        float rpmPct = Mathf.Clamp01(rpm / maxRPM);

        if (rpmText) rpmText.text = Mathf.RoundToInt(rpm).ToString("N0");
        if (rpmBar)
        {
            rpmBar.fillAmount = rpmPct;
            rpmBar.color = rpmPct > 0.8f ? colorRed : rpmPct > 0.6f ? colorAmber : colorGreen;
        }

        // 3. TELEMETRÍA DE PEDALES
        UpdatePedalsTelemetry();
    }

    void UpdatePedalsTelemetry()
    {
        if (carController == null) return;

        // Leemos el input vertical desde tu SimpleCarController
        float inputVertical = carController.m_verticalInput;

        float currentThrottle = Mathf.Clamp01(inputVertical);
        float currentBrake = Mathf.Clamp01(-inputVertical);

        // Actualizar barras visuales de los pedales
        if (throttleBar) throttleBar.fillAmount = currentThrottle;
        if (brakeBar) brakeBar.fillAmount = currentBrake;

        // Textos de porcentaje
        if (throttleText) throttleText.text = Mathf.RoundToInt(currentThrottle * 100f) + "%";
        if (brakeText) brakeText.text = Mathf.RoundToInt(currentBrake * 100f) + "%";
    }
}