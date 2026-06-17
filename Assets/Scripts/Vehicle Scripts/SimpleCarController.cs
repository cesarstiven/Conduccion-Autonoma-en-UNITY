using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents; // Importante para detectar el modo de la IA

public class SimpleCarController : MonoBehaviour
{
    [Header("Ejes de Ruedas Físicas (WheelColliders)")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Mallas Visuales (Transforms)")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Ajustes del Motor")]
    public float maxMotorTorque = 1500f;
    public float maxSteeringAngle = 30f;
    public float brakeTorque = 3000f;

    [Header("Configuración de Control")]
    [Tooltip("Activa esto para manejar con las flechas. Desactívalo para que maneje la IA.")]
    public bool manejarConTeclado = true;

    public float m_horizontalInput;
    public float m_verticalInput;
    private CarAgent agenteIA;

    private void Start()
    {
        // Buscamos si este mismo carro tiene el componente de la IA conectado
        agenteIA = GetComponent<CarAgent>();
    }

    private void FixedUpdate()
    {
        ObtenerInputs();
        Acelerar();
        Girar();
        ActualizarPosicionRuedas();
    }

    private void ObtenerInputs()
    {
        // Si el agente IA está conectado Y le dijimos que NO queremos usar el teclado:
        if (agenteIA != null && manejarConTeclado == false)
        {
            m_horizontalInput = agenteIA.inputGiro;
            m_verticalInput = agenteIA.inputMotor;
        }
        else
        {
            // De lo contrario (modo manual o sin IA), usamos el teclado clásico de Unity
            m_horizontalInput = Input.GetAxis("Horizontal");
            m_verticalInput = Input.GetAxis("Vertical");
        }
    }

    private void Acelerar()
    {
        // Aplicamos torque a las llantas traseras (Tracción trasera)
        rearLeftCollider.motorTorque = m_verticalInput * maxMotorTorque;
        rearRightCollider.motorTorque = m_verticalInput * maxMotorTorque;
    }

    private void Girar()
    {
        // Giramos las llantas delanteras
        float steeringAngle = m_horizontalInput * maxSteeringAngle;
        frontLeftCollider.steerAngle = steeringAngle;
        frontRightCollider.steerAngle = steeringAngle;
    }

    private void ActualizarPosicionRuedas()
    {
        ActualizarRuedaIndividual(frontLeftCollider, frontLeftTransform);
        ActualizarRuedaIndividual(frontRightCollider, frontRightTransform);
        ActualizarRuedaIndividual(rearLeftCollider, rearLeftTransform);
        ActualizarRuedaIndividual(rearRightCollider, rearRightTransform);
    }

    private void ActualizarRuedaIndividual(WheelCollider collider, Transform transformVisual)
    {
        if (transformVisual == null) return;

        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);

        transformVisual.position = pos;
        transformVisual.rotation = rot;
    }
}