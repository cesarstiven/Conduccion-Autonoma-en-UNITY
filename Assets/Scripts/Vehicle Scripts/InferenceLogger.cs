using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;

public class InferenceLogger : MonoBehaviour
{
    [Header("Configuración del Agente de ML-Agents")]
    public Agent carAgent; 
    public string nombreAlgoritmo = "PPO"; 

    [Header("Configuración de la Evaluación")]
    public int maxIntentos = 10;

    private int intentoActual = 0;
    private List<string> datosRegistro = new List<string>();
    private float tiempoInicioEpisodio;
    private bool pruebaFinalizada = false;

    void Start()
    {
        if (carAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] ERROR: No has asignado el 'Car Agent' en el inspector.");
            enabled = false;
            return;
        }

        datosRegistro.Add("Intento,Algoritmo,RecompensaAcumulada,TiempoSegundos,Estado");
        tiempoInicioEpisodio = Time.time;
        Debug.Log($"[{nombreAlgoritmo}] Logger inicializado con éxito. Esperando {maxIntentos} intentos...");
    }

    public void RegistrarFinDeIntento(bool fueExito)
    {
        if (pruebaFinalizada) return;

        intentoActual++;
        float recompensaFinal = carAgent.GetCumulativeReward();
        float tiempoDuracion = Time.time - tiempoInicioEpisodio;
        tiempoInicioEpisodio = Time.time; 

        string estadoStr = fueExito ? "Vuelta Completa" : "Fallo Terminal";

        string filaCsv = string.Format(System.Globalization.CultureInfo.InvariantCulture, 
            "{0},{1},{2:F2},{3:F2},{4}", 
            intentoActual, nombreAlgoritmo, recompensaFinal, tiempoDuracion, estadoStr);
        
        datosRegistro.Add(filaCsv);

        Debug.Log($"<color=green>[{nombreAlgoritmo}] Intento {intentoActual}/{maxIntentos} registrado. Recompensa: {recompensaFinal:F2} | Tiempo: {tiempoDuracion:F2}s ({estadoStr})</color>");

        if (intentoActual >= maxIntentos)
        {
            pruebaFinalizada = true;
            GuardarDatosCSV();
        }
    }

    private void GuardarDatosCSV()
    {
        string nombreArchivo = $"Resultado_Inferencia_{nombreAlgoritmo}.csv";
        string rutaCompleta = Path.Combine(Application.dataPath, "..", nombreArchivo);

        try
        {
            File.WriteAllLines(rutaCompleta, datosRegistro);
            Debug.LogWarning($"<color=yellow><b>¡PRUEBA COMPLETADA EXITOSAMENTE!</b> Los datos de {nombreAlgoritmo} se guardaron en: {Path.GetFullPath(rutaCompleta)}</color>");
        }
        catch (IOException e)
        {
            Debug.LogError($"[{nombreAlgoritmo}] No se pudo escribir el archivo CSV. Asegúrate de tener el Excel cerrado. Error: {e.Message}");
        }
    }
}