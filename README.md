# Análisis Comparativo de Algoritmos de Aprendizaje por Refuerzo Profundo (PPO vs. SAC) para Conducción Autónoma en Unity ML-Agents

Proyecto académico desarrollado para el curso de Aprendizaje por Refuerzo, enfocado en entrenar un agente de conducción autónoma dentro de un entorno de simulación 3D construido en Unity. El proyecto compara empíricamente dos algoritmos del ecosistema Deep Reinforcement Learning — **Proximal Policy Optimization (PPO)** y **Soft Actor-Critic (SAC)** — evaluando su estabilidad de entrenamiento, eficiencia de navegación y capacidad de generalización sobre una pista cerrada con checkpoints secuenciales.

## Tabla de contenidos

- [Descripción general](#descripción-general)
- [Arquitectura del proyecto](#arquitectura-del-proyecto)
- [Requisitos](#requisitos)
- [Instalación](#instalación)
- [Estructura del repositorio](#estructura-del-repositorio)
- [Configuración del agente en Unity](#configuración-del-agente-en-unity)
- [Entrenamiento](#entrenamiento)
- [Sistema de recompensas](#sistema-de-recompensas)
- [Resultados](#resultados)
- [Problemas conocidos y soluciones](#problemas-conocidos-y-soluciones)
- [Créditos y referencias](#créditos-y-referencias)

## Descripción general

El agente controla un vehículo con físicas reales basadas en `WheelCollider` de Unity, debiendo aprender a navegar una pista cerrada pasando por una secuencia de checkpoints en orden correcto, evitando salirse a una zona de césped que actúa como penalización. El entorno está construido con ProBuilder y soporta entrenamiento paralelo con múltiples instancias simultáneas para acelerar la convergencia.

El proyecto evalúa dos paradigmas algorítmicos distintos:

| Algoritmo | Tipo | Política | On/Off-policy |
|---|---|---|---|
| **PPO** | Gradiente de política con clipping | Estocástica | On-policy |
| **SAC** | Actor-crítico con entropía máxima | Estocástica | Off-policy |

## Arquitectura del proyecto

```
Agente (CarAgent.cs)
    │
    ▼ throttle, steering (acciones continuas)
    │
SimpleCarController.cs
    │
    ▼ aplica torque y ángulo de dirección
    │
WheelColliders (física real de Unity)
    │
    ▼
Rigidbody del vehículo
```

El agente actúa como controlador de alto nivel desacoplado del sistema físico: únicamente decide `throttle` (aceleración) y `steering` (dirección), valores que `SimpleCarController.cs` traduce en fuerzas reales sobre los `WheelCollider`. Este desacople evita conflictos de doble escritura sobre el mismo Rigidbody.

## Requisitos

- Unity 2020.2.7f1 (HDRP)
- Python 3.10 (ver nota de compatibilidad abajo)
- ML-Agents Toolkit 1.1.0
- PyTorch (CPU o CUDA)

> **Nota de compatibilidad:** ML-Agents 1.1.0 requiere Python 3.10.x. Versiones más recientes de Python (3.11+) no son compatibles con esta versión del toolkit.

## Instalación

1. Clona el repositorio:
```bash
git clone https://github.com/tu-usuario/tu-repositorio.git
cd tu-repositorio
```

2. Crea un entorno virtual con Python 3.10:
```bash
python3.10 -m venv venv_mlagents
venv_mlagents\Scripts\activate    # Windows
source venv_mlagents/bin/activate # Linux/Mac
```

3. Instala las dependencias desde `requirements.txt`:
```bash
pip install -r requirements.txt
```

> El archivo `requirements.txt` incluido fija las versiones exactas usadas en este proyecto, entre ellas `mlagents==1.1.0`, `mlagents-envs==1.1.0`, `torch==2.12.0` y `numpy==1.23.5`. Respetar estas versiones evita conflictos de compatibilidad, especialmente con `numpy` (debe ser <2.0) y `torch`.

4. Abre el proyecto desde Unity Hub apuntando a la carpeta raíz del repositorio. Unity regenerará automáticamente las carpetas `Library/` y `Temp/`.

## Estructura del repositorio

```
├── Assets/
│   ├── Scripts/
│   │   └── Vehicle Scripts/
│   │       ├── CarAgent.cs          # Agente de ML-Agents
│   │       ├── SimpleCarController.cs # Física del vehículo (WheelColliders)
│   │       ├── TrackCheckPoints.cs   # Gestor de checkpoints con eventos
│   │       ├── CheckpointSingle.cs   # Trigger individual de checkpoint
│   │       └── CarHUD.cs             # Interfaz visual (velocímetro, RPM, marchas)
│   ├── Scenes/
│   └── ML-Agents/
├── Config/
│   ├── trainer_ppo.yaml             # Hiperparámetros PPO
│   └── trainer_sac.yaml             # Hiperparámetros SAC
├── results/                          # Modelos entrenados (.onnx) y logs de TensorBoard
├── Resultado_Inferencia_PPO.csv      # Métricas de evaluación PPO
├── Resultado_Inferencia_SAC.csv      # Métricas de evaluación SAC
├── ProjectSettings/
├── Packages/
└── README.md
```

## Configuración del agente en Unity

El componente `Behavior Parameters` del vehículo debe configurarse así:

```
Behavior Name: MyCar
Vector Observation → Space Size: 9
Continuous Actions: 2
Discrete Branches: 0
```

### Espacio de observaciones (9 valores)

| # | Observación | Descripción |
|---|---|---|
| 1-3 | `transform.forward` | Orientación del vehículo en el mundo |
| 4-6 | Dirección local al checkpoint | Vector normalizado hacia el siguiente checkpoint, en espacio local del vehículo |
| 7 | Velocidad forward normalizada | Componente de velocidad en dirección de avance / `maxSpeed` |
| 8 | Velocidad angular Y | Detecta si el vehículo está girando |
| 9 | Velocidad lateral normalizada | Detecta derrape |

### Detección de terreno

El césped se detecta mediante **3 Raycasts** verticales (centro, izquierda, derecha) en lugar de colliders trigger, evitando falsos negativos por tunneling a alta velocidad o desalineación de mallas.

## Entrenamiento

Ejecuta desde la raíz del proyecto, con el entorno virtual activado:

```bash
# Entrenamiento con PPO
mlagents-learn Config/trainer_ppo.yaml --run-id=PPO_run1

# Entrenamiento con SAC
mlagents-learn Config/trainer_sac.yaml --run-id=SAC_run1
```

Espera el mensaje `Start training by pressing the Play button in the Unity Editor` y presiona **Play** en Unity.

### Hiperparámetros utilizados

**PPO:**
```yaml
batch_size: 2048
buffer_size: 20480
learning_rate: 3.0e-4
beta: 5.0e-3
epsilon: 0.2
time_horizon: 256
```

**SAC:**
```yaml
batch_size: 256
buffer_size: 50000
learning_rate: 3.0e-4
tau: 0.005
init_entcoef: 1.0
time_horizon: 256
```

### Optimización de rendimiento durante entrenamiento

Para entrenar con múltiples instancias paralelas sin saturar Unity, se recomienda desactivar:

- Cámaras de seguimiento (`Main Camera`)
- Sistemas de audio del motor (`EngineSoundController`)
- Controladores visuales de ruedas (`WheelVisualController`)
- Canvas / HUD de interfaz (`CarHUD.cs` y su GameObject contenedor)
- Quality Settings en `Low` o `Very Low`

Estos cambios reducen significativamente el tiempo de render por frame, evitando `UnityTimeOutException` durante la conexión con el proceso de entrenamiento en Python.

## Sistema de recompensas

| Evento | Recompensa |
|---|---|
| Avanzar hacia el siguiente checkpoint | `+velocidad útil × 0.01` |
| Cruzar checkpoint en orden correcto | `+5.0` |
| Cruzar checkpoint en orden incorrecto | `-3.0` (termina episodio) |
| Tocar césped (continuo) | `-0.03` por frame |
| Permanecer en césped más de 2s | `-3.0` (termina episodio) |
| Quedarse inmóvil más de 3s | `-3.0` (termina episodio) |
| Caer fuera de los límites del mapa | `-3.0` (termina episodio) |
| Impuesto por tiempo | `-0.003` por frame |

El diseño evita incentivos perversos comunes en RL aplicado a conducción, como el "suicidio" del agente (terminar el episodio deliberadamente para evitar acumular penalización por tiempo) o el estancamiento por miedo a penalizaciones severas.

## Resultados

Los resultados de inferencia de ambos algoritmos se encuentran en:

- `Resultado_Inferencia_PPO.csv`
- `Resultado_Inferencia_SAC.csv`

Los modelos entrenados (`.onnx`) y logs compatibles con TensorBoard están en la carpeta `results/`. Para visualizar las curvas de entrenamiento:

```bash
tensorboard --logdir results
```

## Problemas conocidos y soluciones

| Problema | Causa | Solución aplicada |
|---|---|---|
| Carro atraviesa el suelo | `Mesh Collider` con `Convex` activado en el césped | Desactivar `Convex` en el plano de césped |
| Checkpoints se saltan a alta velocidad | Tunneling físico | `Collision Detection: Continuous Dynamic` + colliders más gruesos |
| Agente se queda inmóvil | Penalización por movimiento riesgoso mayor que el costo de no moverse | Temporizador de estancamiento que termina el episodio tras 3s sin movimiento |
| Agente acelera hacia el césped deliberadamente | Recompensa de velocidad no condicionada a dirección útil | Recompensa basada en `Vector3.Dot` hacia el checkpoint, no en velocidad bruta |
| `UnityTimeOutException` al iniciar entrenamiento | Carga gráfica excesiva con múltiples instancias | Desactivar HUD, cámaras, audio y partículas durante entrenamiento |

## Créditos y referencias

- Base del sistema de conducción: [Unity-Driving-System de GalanLefont](https://github.com/GalanLefont/Unity-Driving-System)
- Framework de aprendizaje: [Unity ML-Agents Toolkit](https://github.com/Unity-Technologies/ml-agents)
- Algoritmos: Schulman et al. (2017) *Proximal Policy Optimization Algorithms*; Haarnoja et al. (2018) *Soft Actor-Critic*

---

Proyecto desarrollado como parte del curso de Aprendizaje por Refuerzo. Universidad Nacional de Colombia, Sede La Paz.
