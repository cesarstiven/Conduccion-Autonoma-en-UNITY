using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    private TrackCheckPoints trackCheckpoints;

    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificamos si lo que chocó fue la IA del carro
        if (other.TryGetComponent<CarAgent>(out CarAgent agent))
        {
            // 2. Verificamos si este checkpoint fue inicializado correctamente
            if (trackCheckpoints != null)
            {
                trackCheckpoints.PlayerTroughCheckpoint(this, other.transform);
            }
            else
            {
                // 3. Si no fue inicializado, evitamos el error rojo y delatamos al cubo culpable en amarillo
                Debug.LogWarning(" Ojo: El checkpoint llamado '" + gameObject.name + "' fue tocado pero no tiene pista asignada. Revisa que esté dentro de la carpeta Checkpoints.");
            }
        }
    }

    public void SetTrackCheckpoints(TrackCheckPoints trackCheckpoints)
    {
        this.trackCheckpoints = trackCheckpoints;
    }
}