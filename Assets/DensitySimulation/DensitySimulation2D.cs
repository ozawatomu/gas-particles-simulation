using System.Runtime.InteropServices;
using Tomu.Helpers;
using UnityEngine;

public class DensitySimulation2D : MonoBehaviour
{
    [System.Serializable]
    public struct Rectangle
    {
        public Vector2 position;
        public Vector2 size;
    }

    struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    [Header("References")]
    public ComputeShader computeShader;
    public Shader particleShader;
    public GameObject canvasQuad;

    [Header("Initial Settings")]
    [Min(0)]
    public int particleCount = 1000;
    public Vector2 boundsSize;
    public Rectangle spawner;

    [Header("Simulation Settings")]
    [Min(0)]
    public float timeScale = 1;

    [Range(1, 10)]
    public int simulationSubsteps = 3;

    [Range(0f, 10f)]
    public float smoothingRadius = 1.2f;
    public float pressureMultiplier = 200f;

    [Range(0f, 1f)]
    public float collisionDamping = 0.7f;

    [Header("Interaction Settings")]
    public float interactionStrength = 1f;
    public float interactionRadius = 1f;

    [Header("Visual Settings")]
    public bool showDensities = true;
    public bool showParticles = true;

    [Range(0, 0.5f)]
    public float particleRadius = 0.05f;
    public Color particleColor = new Color(1f, 1f, 1f);
    public Vector2Int canvasResolution = new Vector2Int(1920, 1080);

    [Range(0, 1f)]
    public float densityBrightnessMultiplier = 0.05f;

    // Compute shader fields
    ComputeBuffer particleBuffer;
    ComputeBuffer predictedParticlePositionBuffer;
    ComputeBuffer tempParticleBuffer;
    ComputeBuffer tempPredictedParticlePositionBuffer;
    ComputeBuffer densityBuffer;
    ComputeBuffer pressureForceBuffer;
    ComputeBuffer interactionForceBuffer;
    Seb.Helpers.SpatialHash spatialHash; // Sebastian Lague's SpatialHash implementation
    int calculatePredictedParticlePositionsKernel;
    int calculateCellKeysKernel;
    int reorderToTempKernel;
    int commitReorderKernel;
    int calculateDensitiesKernel;
    int calculatePressureForcesKernel;
    int calculateInteractionForcesKernel;
    int updateVelocitiesKernel;
    int updatePositionsKernel;
    int generateCanvasTextureKernel;

    // Particle rendering fields
    Material particleMaterial;
    Mesh quadMesh;
    Bounds renderBounds;

    // Canvas rendering fields
    RenderTexture canvasRenderTexture;

    void Start()
    {
        spatialHash = new Seb.Helpers.SpatialHash(particleCount);
        GetKernelIndices();
        CreateBuffers();
        SetBuffers();
        SetInitialComputeShaderData();
        SetParticleRenderFields();
        SetCanvasRenderFields();
    }

    void Update()
    {
        ApplyCanvasToggle();
        RunSimulationFrame(Time.deltaTime * timeScale);
        if (showParticles)
        {
            RenderParticles();
        }
    }

    void GetKernelIndices()
    {
        calculatePredictedParticlePositionsKernel = computeShader.FindKernel(
            "CalculatePredictedParticlePositions"
        );
        calculateCellKeysKernel = computeShader.FindKernel("CalculateCellKeys");
        reorderToTempKernel = computeShader.FindKernel("ReorderToTemp");
        commitReorderKernel = computeShader.FindKernel("CommitReorder");
        calculateDensitiesKernel = computeShader.FindKernel("CalculateDensities");
        calculatePressureForcesKernel = computeShader.FindKernel("CalculatePressureForces");
        calculateInteractionForcesKernel = computeShader.FindKernel("CalculateInteractionForces");
        updateVelocitiesKernel = computeShader.FindKernel("UpdateVelocities");
        updatePositionsKernel = computeShader.FindKernel("UpdatePositions");
        generateCanvasTextureKernel = computeShader.FindKernel("GenerateCanvasTexture");
    }

    void CreateBuffers()
    {
        particleBuffer = ComputeHelper.CreateBuffer<Particle>(particleCount);
        predictedParticlePositionBuffer = ComputeHelper.CreateBuffer<Vector2>(particleCount);
        tempParticleBuffer = ComputeHelper.CreateBuffer<Particle>(particleCount);
        tempPredictedParticlePositionBuffer = ComputeHelper.CreateBuffer<Vector2>(particleCount);
        densityBuffer = ComputeHelper.CreateBuffer<float>(particleCount);
        pressureForceBuffer = ComputeHelper.CreateBuffer<Vector2>(particleCount);
        interactionForceBuffer = ComputeHelper.CreateBuffer<Vector2>(particleCount);
        canvasRenderTexture = ComputeHelper.CreateRenderTexture(
            canvasResolution.x,
            canvasResolution.y,
            0
        );
    }

    void SetBuffers()
    {
        ComputeHelper.SetBuffer(
            computeShader,
            particleBuffer,
            "particleBuffer",
            calculatePredictedParticlePositionsKernel,
            calculateDensitiesKernel,
            calculatePressureForcesKernel,
            updateVelocitiesKernel,
            updatePositionsKernel,
            generateCanvasTextureKernel,
            calculateInteractionForcesKernel,
            calculateCellKeysKernel,
            reorderToTempKernel,
            commitReorderKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            predictedParticlePositionBuffer,
            "predictedParticlePositionBuffer",
            calculatePredictedParticlePositionsKernel,
            calculateDensitiesKernel,
            calculatePressureForcesKernel,
            calculateInteractionForcesKernel,
            reorderToTempKernel,
            commitReorderKernel,
            generateCanvasTextureKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            tempParticleBuffer,
            "tempParticleBuffer",
            reorderToTempKernel,
            commitReorderKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            tempPredictedParticlePositionBuffer,
            "tempPredictedParticlePositionBuffer",
            reorderToTempKernel,
            commitReorderKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            densityBuffer,
            "densityBuffer",
            calculateDensitiesKernel,
            calculatePressureForcesKernel,
            updateVelocitiesKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            pressureForceBuffer,
            "pressureForceBuffer",
            calculatePressureForcesKernel,
            updateVelocitiesKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            interactionForceBuffer,
            "interactionForceBuffer",
            calculateInteractionForcesKernel,
            updateVelocitiesKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            spatialHash.SpatialKeys,
            "cellKeyBuffer",
            calculateCellKeysKernel,
            calculateDensitiesKernel,
            calculatePressureForcesKernel,
            generateCanvasTextureKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            spatialHash.SpatialOffsets,
            "startIndexBuffer",
            calculateDensitiesKernel,
            calculatePressureForcesKernel,
            generateCanvasTextureKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            spatialHash.SpatialIndices,
            "sortedIndexBuffer",
            reorderToTempKernel
        );
        ComputeHelper.SetTexture(
            computeShader,
            canvasRenderTexture,
            "canvasRenderTexture",
            generateCanvasTextureKernel
        );
    }

    void SetInitialComputeShaderData()
    {
        SetInitialParticleData();
        computeShader.SetInt("particleCount", particleCount);
    }

    void SetInitialParticleData()
    {
        Particle[] initialParticles = new Particle[particleCount];
        Vector2 spawnerPosition = spawner.position;
        Vector2 spawnerSize = spawner.size;
        float spawnMinX = spawnerPosition.x - spawnerSize.x * 0.5f;
        float spawnMaxX = spawnerPosition.x + spawnerSize.x * 0.5f;
        float spawnMinY = spawnerPosition.y - spawnerSize.y * 0.5f;
        float spawnMaxY = spawnerPosition.y + spawnerSize.y * 0.5f;
        for (int particleI = 0; particleI < particleCount; particleI++)
        {
            initialParticles[particleI].position = new Vector2(
                Random.Range(spawnMinX, spawnMaxX),
                Random.Range(spawnMinY, spawnMaxY)
            );
            initialParticles[particleI].velocity = Vector2.zero;
        }
        particleBuffer.SetData(initialParticles);
    }

    void SetParticleRenderFields()
    {
        // Setup quad mesh
        quadMesh = new Mesh();
        quadMesh.name = "InstancedQuad";
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
        };
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        quadMesh.vertices = vertices;
        quadMesh.uv = uvs;
        quadMesh.triangles = triangles;
        quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one);

        // Setup other rendering fields
        particleMaterial = new Material(particleShader);
        renderBounds = new Bounds(Vector3.zero, new Vector3(boundsSize.x, boundsSize.y, 1f));
    }

    void SetCanvasRenderFields()
    {
        Renderer quadRenderer = canvasQuad.GetComponent<Renderer>();
        quadRenderer.enabled = true;
        quadRenderer.material.SetTexture("_MainTex", canvasRenderTexture);
    }

    void ApplyCanvasToggle()
    {
        canvasQuad.GetComponent<Renderer>().enabled = showDensities;
    }

    void RunSimulationFrame(float frameDeltaTime)
    {
        float substepDeltaTime = frameDeltaTime / simulationSubsteps;

        computeShader.SetFloat("deltaTime", substepDeltaTime);
        computeShader.SetFloat("smoothingRadius", smoothingRadius);
        computeShader.SetFloat("collisionDamping", collisionDamping);
        computeShader.SetFloat("pressureMultiplier", pressureMultiplier);
        computeShader.SetVector("boundsSize", boundsSize);
        computeShader.SetInts("canvasResolution", canvasResolution.x, canvasResolution.y);
        computeShader.SetFloat("densityBrightnessMultiplier", densityBrightnessMultiplier);

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool isPullInteraction = Input.GetMouseButton(0);
        bool isPushInteraction = Input.GetMouseButton(1);

        computeShader.SetVector("interactionPosition", mousePos);
        computeShader.SetFloat("interactionStrength", interactionStrength);
        computeShader.SetFloat("interactionRadius", interactionRadius);
        computeShader.SetBool("isPullInteraction", isPullInteraction);
        computeShader.SetBool("isPushInteraction", isPushInteraction);

        for (int substepI = 0; substepI < simulationSubsteps; substepI++)
        {
            RunSimulationSubstep();
        }

        if (showDensities)
        {
            ComputeHelper.Dispatch(
                computeShader,
                generateCanvasTextureKernel,
                canvasResolution.x,
                canvasResolution.y,
                1
            );
        }
    }

    void RunSimulationSubstep()
    {
        ComputeHelper.Dispatch(
            computeShader,
            calculatePredictedParticlePositionsKernel,
            particleCount,
            1,
            1
        );

        ComputeHelper.Dispatch(computeShader, calculateCellKeysKernel, particleCount, 1, 1);
        spatialHash.Run();
        ComputeHelper.Dispatch(computeShader, reorderToTempKernel, particleCount, 1, 1);
        ComputeHelper.Dispatch(computeShader, commitReorderKernel, particleCount, 1, 1);

        ComputeHelper.Dispatch(computeShader, calculateDensitiesKernel, particleCount, 1, 1);
        ComputeHelper.Dispatch(computeShader, calculatePressureForcesKernel, particleCount, 1, 1);
        ComputeHelper.Dispatch(
            computeShader,
            calculateInteractionForcesKernel,
            particleCount,
            1,
            1
        );
        ComputeHelper.Dispatch(computeShader, updateVelocitiesKernel, particleCount, 1, 1);
        ComputeHelper.Dispatch(computeShader, updatePositionsKernel, particleCount, 1, 1);
    }

    void RenderParticles()
    {
        particleMaterial.SetBuffer("_Particles", particleBuffer);
        particleMaterial.SetFloat("_Radius", particleRadius);
        particleMaterial.SetColor("_Color", particleColor);
        Graphics.DrawMeshInstancedProcedural(
            quadMesh,
            0,
            particleMaterial,
            renderBounds,
            particleCount
        );
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);
        Gizmos.color = new Color(0, 0, 1, 0.4f);
        Gizmos.DrawWireCube(spawner.position, spawner.size);
        Gizmos.color = new Color(1, 0, 0, 0.4f);

        if (Application.isPlaying)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            bool isPullInteraction = Input.GetMouseButton(0);
            bool isPushInteraction = Input.GetMouseButton(1);
            bool isInteracting = isPullInteraction || isPushInteraction;
            if (isInteracting)
            {
                Gizmos.color = isPullInteraction ? Color.green : Color.red;
                Gizmos.DrawWireSphere(mousePos, interactionRadius);
            }
        }
    }

    void OnDestroy()
    {
        ComputeHelper.ReleaseComputeBuffers(
            particleBuffer,
            predictedParticlePositionBuffer,
            tempParticleBuffer,
            tempPredictedParticlePositionBuffer,
            densityBuffer,
            pressureForceBuffer,
            interactionForceBuffer
        );
        spatialHash.Release();
        ComputeHelper.ReleaseRenderTextures(canvasRenderTexture);
    }
}
