using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CarAgent : Agent
{
    private Rigidbody rb;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private bool episodioTerminado = false;

    // ==========================================
    // NUEVAS VARIABLES PARA EVALUACIÓN Y LOGS
    // ==========================================
    [Header("Configuración de Evaluación (NUEVO)")]
    [Tooltip("Asigna aquí el objeto Logger correspondiente (Logger_PPO o Logger_SAC)")]
    public InferenceLogger miLogger;
    private int checkpointsCruzados = 0; // Contador interno de la vuelta actual
    private const int TOTAL_CHECKPOINTS_VUELTA = 92; // Total de tu circuito
    // ==========================================

    [Header("Configuración de Interfaz (UI)")]
    [SerializeField] private Text rewardText;

    [Header("Configuración de Checkpoints")]
    [SerializeField] private TrackCheckPoints trackCheckpoints;

    [Header("Configuración de Velocidad")]
    [SerializeField] private float maxSpeed = 45f;

    [Header("Configuración de Estancamiento")]
    [SerializeField] private float tiempoDeGraciaInicial = 5f;
    [SerializeField] private float velocidadMinimaRequerida = 1.5f;
    [SerializeField] private float tiempoMaxEstancado = 3f;
    private float tiempoDesdeInicio = 0f;
    private float tiempoEstancado = 0f;

    [Header("Configuración de Césped")]
    [SerializeField] private float tiempoMaxEnCesped = 1.5f;
    private float tiempoEnCesped = 0f;
    private bool estaEnCesped = false;

    [HideInInspector] public float inputGiro;
    [HideInInspector] public float inputMotor;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;

        if (rewardText == null && transform.parent != null)
            rewardText = transform.parent.GetComponentInChildren<Text>();

        if (trackCheckpoints != null)
        {
            trackCheckpoints.OnCarCorrectCheckpoint += OnCorrectCheckpoint;
            trackCheckpoints.OnCarWrongCheckpoint += OnWrongCheckpoint;
        }
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = posicionInicial;
        transform.localRotation = rotacionInicial;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        episodioTerminado = false;
        tiempoDesdeInicio = 0f;
        tiempoEstancado = 0f;
        tiempoEnCesped = 0f;
        estaEnCesped = false;

        // NUEVO: Reiniciar el contador de checkpoints al iniciar cada intento
        checkpointsCruzados = 0;

        if (trackCheckpoints != null)
            trackCheckpoints.ResetCheckpoint(transform);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.forward);

        if (trackCheckpoints != null)
        {
            CheckpointSingle nextCheckpoint = trackCheckpoints.GetNextCheckpointForward(transform);
            if (nextCheckpoint != null)
            {
                Vector3 dirCheckpoint = (nextCheckpoint.transform.position - transform.position).normalized;
                Vector3 localDir = transform.InverseTransformDirection(dirCheckpoint);
                sensor.AddObservation(localDir);
            }
            else
            {
                sensor.AddObservation(Vector3.zero);
            }
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
        }

        float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);
        sensor.AddObservation(forwardSpeed / maxSpeed);

        sensor.AddObservation(rb.angularVelocity.y / 10f);

        float lateralSpeed = Vector3.Dot(rb.velocity, transform.right);
        sensor.AddObservation(lateralSpeed / maxSpeed);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (episodioTerminado) return;

        inputGiro = actions.ContinuousActions[0];
        inputMotor = actions.ContinuousActions[1];

        Vector3 dirCheckpoint = transform.forward;
        if (trackCheckpoints != null)
        {
            CheckpointSingle nextCheckpoint = trackCheckpoints.GetNextCheckpointForward(transform);
            if (nextCheckpoint != null)
                dirCheckpoint = (nextCheckpoint.transform.position - transform.position).normalized;
        }

        float speedTowardsTarget = Vector3.Dot(rb.velocity, dirCheckpoint);

        if (speedTowardsTarget > 0.5f)
            AddReward(speedTowardsTarget * 0.001f);

        if (estaEnCesped)
        {
            tiempoEnCesped += Time.deltaTime;
            AddReward(-0.01f);

            if (tiempoEnCesped >= tiempoMaxEnCesped)
            {
                AddReward(-1.5f);

                // MODIFICADO: Reportar fallo por salida de césped prolongada
                if (miLogger != null) miLogger.RegistrarFinDeIntento(false);

                Terminar();
                return;
            }
        }
        else
        {
            tiempoEnCesped = 0f;
        }

        AddReward(-0.0005f);
    }

    private void FixedUpdate()
    {
        if (episodioTerminado) return;

        DetectarCesped();

        tiempoDesdeInicio += Time.fixedDeltaTime;

        if (tiempoDesdeInicio > tiempoDeGraciaInicial)
        {
            if (rb.velocity.magnitude < velocidadMinimaRequerida)
            {
                tiempoEstancado += Time.fixedDeltaTime;
                if (tiempoEstancado >= tiempoMaxEstancado)
                {
                    AddReward(-1.0f);

                    // MODIFICADO: Reportar fallo por estancamiento
                    if (miLogger != null) miLogger.RegistrarFinDeIntento(false);

                    Terminar();
                    return;
                }
            }
            else
            {
                tiempoEstancado = 0f;
            }
        }

        if (transform.position.y < posicionInicial.y - 5f)
        {
            AddReward(-1.5f);

            // MODIFICADO: Reportar fallo por caída al vacío
            if (miLogger != null) miLogger.RegistrarFinDeIntento(false);

            Terminar();
            return;
        }

        if (rewardText != null)
            rewardText.text = "Recompensa: " + GetCumulativeReward().ToString("F2");
    }

    private void DetectarCesped()
    {
        Vector3[] origenes = {
            transform.position + Vector3.up * 0.3f,
            transform.position + Vector3.up * 0.3f + transform.right * 0.7f,
            transform.position + Vector3.up * 0.3f - transform.right * 0.7f
        };

        bool detectado = false;

        foreach (Vector3 origen in origenes)
        {
            RaycastHit hit;
            if (Physics.Raycast(origen, Vector3.down, out hit, 1.5f))
            {
                if (hit.collider.CompareTag("Cesped"))
                {
                    detectado = true;
                    break;
                }
            }
#if UNITY_EDITOR
            Debug.DrawRay(origen, Vector3.down * 1.5f, detectado ? Color.red : Color.green);
#endif
        }

        estaEnCesped = detectado;
    }

    private void Terminar()
    {
        episodioTerminado = true;
        EndEpisode();
    }

    private void OnCorrectCheckpoint(object sender, CarCorrectCheckpointEventArgs e)
    {
        if (e.carTransform == transform && !episodioTerminado)
        {
            AddReward(1.0f);

            // =========================================================
            // LÓGICA DE DETECCÓN DE VUELTA COMPLETA (NUEVO)
            // =========================================================
            checkpointsCruzados++;

            if (checkpointsCruzados >= TOTAL_CHECKPOINTS_VUELTA)
            {
                // Reportar al logger que completó la vuelta limpia exitosamente
                if (miLogger != null) miLogger.RegistrarFinDeIntento(true);

                // Opcional: Puedes darle un premio extra pequeño por completar la vuelta entera
                AddReward(5.0f);

                // Reiniciar el episodio para pasar al siguiente intento de la evaluación
                Terminar();
            }
            // =========================================================
        }
    }

    private void OnWrongCheckpoint(object sender, CarWrongCheckpointEventArgs e)
    {
        if (e.carTransform == transform && !episodioTerminado)
        {
            AddReward(-1.0f);

            // MODIFICADO: Reportar fallo por ir en sentido contrario o saltar checkpoint
            if (miLogger != null) miLogger.RegistrarFinDeIntento(false);

            Terminar();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = Input.GetAxis("Horizontal");
        ca[1] = Input.GetAxis("Vertical");
    }

    private void OnDestroy()
    {
        if (trackCheckpoints != null)
        {
            trackCheckpoints.OnCarCorrectCheckpoint -= OnCorrectCheckpoint;
            trackCheckpoints.OnCarWrongCheckpoint -= OnWrongCheckpoint;
        }
    }
}