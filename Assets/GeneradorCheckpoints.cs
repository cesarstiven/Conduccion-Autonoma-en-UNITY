using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeneradorCheckpoints : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject pistaProBuilder; // Arrastra aquí tu pista de ProBuilder
    public GameObject prefabCheckpoint; // Arrastra aquí tu cubo de checkpoint (con Is Trigger)
    public float alturaCheckpoint = 3f; // Qué tan alto quieres el cubo del checkpoint

    [ContextMenu("¡Generar Checkpoints Automáticos!")]
    public void Generar()
    {
        if (pistaProBuilder == null || prefabCheckpoint == null)
        {
            Debug.LogError("Mano, te falta arrastrar la pista o el prefab al script.");
            return;
        }

        MeshFilter meshFilter = pistaProBuilder.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("El objeto seleccionado no tiene una malla (Mesh).");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Crear un contenedor limpio en la jerarquía
        GameObject contenedor = new GameObject("Contenedor_Checkpoints_Monza");
        contenedor.transform.position = pistaProBuilder.transform.position;

        List<Vector3> centrosDeTramos = new List<Vector3>();

        // Agrupamos los triángulos para calcular los centros de las secciones de la pista
        for (int i = 0; i < triangles.Length; i += 6)
        {
            if (i + 5 >= triangles.Length) break;

            // Tomamos los vértices del tramo actual
            Vector3 v1 = pistaProBuilder.transform.TransformPoint(vertices[triangles[i]]);
            Vector3 v2 = pistaProBuilder.transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 v3 = pistaProBuilder.transform.TransformPoint(vertices[triangles[i + 2]]);
            Vector3 v4 = pistaProBuilder.transform.TransformPoint(vertices[triangles[i + 5]]);

            // Calculamos el centro de ese tramo de asfalto
            Vector3 centroTramo = (v1 + v2 + v3 + v4) / 4f;
            centrosDeTramos.Add(centroTramo);
        }

        // Instanciar los checkpoints orientados hacia el siguiente centro
        for (int i = 0; i < centrosDeTramos.Count; i++)
        {
            Vector3 posicionActual = centrosDeTramos[i];
            Vector3 posicionSiguiente = (i + 1 < centrosDeTramos.Count) ? centrosDeTramos[i + 1] : centrosDeTramos[0];

            // Crear el checkpoint
            GameObject nuevoCP = Instantiate(prefabCheckpoint, posicionActual, Quaternion.identity, contenedor.transform);
            nuevoCP.name = "Checkpoint_" + i;

            // Orientarlo hacia la dirección de la pista
            nuevoCP.transform.LookAt(posicionSiguiente);

            // Ajustar escala para que cubra la altura (el ancho lo configuras en tu prefab)
            Vector3 escalaActual = nuevoCP.transform.localScale;
            nuevoCP.transform.localScale = new Vector3(escalaActual.x, alturaCheckpoint, escalaActual.z);
        }

        Debug.Log("¡Listo, mk! Se crearon " + centrosDeTramos.Count + " checkpoints ordenados en 'Contenedor_Checkpoints_Monza'.");
    }
}