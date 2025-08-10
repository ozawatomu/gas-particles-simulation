using System.Runtime.InteropServices;
using Helpers;
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

    [Header("Simulation Settings")]
    [Min(0)]
    public float timeScale = 1;

    [Range(1, 10)]
    public int simulationSubsteps = 3;

    [Range(0f, 10f)]
    public float smoothingRadius = 1.2f;

    [Header("Initial Settings")]
    [Min(0)]
    public int particleCount = 1000;
    public Vector2 boundsSize;
    public Rectangle spawner;

    [Header("Visual Settings")]
    [Range(0, 0.5f)]
    public float particleRadius = 0.05f;
    public Color particleColor = new Color(1f, 1f, 1f);

    // Compute shader fields
    ComputeBuffer particleBuffer;
    ComputeBuffer densityBuffer;
    ComputeBuffer pressureForceBuffer;
    int calculateDensitiesKernel;
    int calculatePressureForcesKernel;
    int updateVelocitiesKernel;
    int updatePositionsKernel;

    // Particle rendering fields
    Material particleMaterial;
    Mesh quadMesh;
    Bounds renderBounds;

    void Start()
    {
        GetKernelIndices();
        CreateBuffers();
        SetBuffers();
        SetInitialComputeShaderData();
        SetRenderFields();
    }

    void Update()
    {
        RunSimulationFrame(Time.deltaTime * timeScale);
        RenderParticles();
    }

    void CreateBuffers()
    {
        particleBuffer = ComputeHelper.CreateBuffer<Particle>(particleCount);
        densityBuffer = ComputeHelper.CreateBuffer<float>(particleCount);
        pressureForceBuffer = ComputeHelper.CreateBuffer<Vector2>(particleCount);
    }

    void SetBuffers()
    {
        ComputeHelper.SetBuffer(
            computeShader,
            particleBuffer,
            "particleBuffer",
            calculateDensitiesKernel,
            calculatePressureForcesKernel,
            updateVelocitiesKernel,
            updatePositionsKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            densityBuffer,
            "densityBuffer",
            calculateDensitiesKernel,
            calculatePressureForcesKernel
        );
        ComputeHelper.SetBuffer(
            computeShader,
            pressureForceBuffer,
            "pressureForceBuffer",
            calculatePressureForcesKernel,
            updateVelocitiesKernel
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

    void GetKernelIndices()
    {
        calculateDensitiesKernel = computeShader.FindKernel("CalculateDensities");
        calculatePressureForcesKernel = computeShader.FindKernel("CalculatePressureForces");
        updateVelocitiesKernel = computeShader.FindKernel("UpdateVelocities");
        updatePositionsKernel = computeShader.FindKernel("UpdatePositions");
    }

    void SetRenderFields()
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

    void RunSimulationFrame(float frameDeltaTime)
    {
        float substepDeltaTime = frameDeltaTime / simulationSubsteps;
        for (int substepI = 0; substepI < simulationSubsteps; substepI++)
        {
            RunSimulationSubstep(substepDeltaTime);
        }
    }

    void RunSimulationSubstep(float substepDeltaTime)
    {
        computeShader.SetFloat("deltaTime", substepDeltaTime);
        computeShader.SetFloat("smoothingRadius", smoothingRadius);
        computeShader.SetVector("boundsSize", boundsSize);
        ComputeHelper.Dispatch(computeShader, calculateDensitiesKernel, particleCount, 1, 1);
        ComputeHelper.Dispatch(computeShader, calculatePressureForcesKernel, particleCount, 1, 1);
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
    }

    void OnDestroy()
    {
        ComputeHelper.ReleaseBuffers(particleBuffer, densityBuffer, pressureForceBuffer);
    }
}
