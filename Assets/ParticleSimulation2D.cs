using UnityEngine;

public class ParticleSimulation2D : MonoBehaviour
{
    [Header("Simulation Settings")]
    [Min(0)]
    public float timeScale = 1;

    [Min(0)]
    public int particleCount = 1000;

    [Min(0)]
    public float particleRadius = 1;

    [Min(0)]
    public float temperature = 1;
    public Vector2 boundsSize;
    public Vector2 spawnerSize;
    public Vector2 spawnerPosition;

    [Range(1, 10)]
    public int simulationSubsteps = 3;

    struct Particle
    {
        public Vector2 position;
        public Vector2 direction;
    }

    [System.Serializable]
    public struct Obstacle
    {
        public Vector2 position;
        public Vector2 size;
    }

    public Obstacle[] obstacles;

    // Computer shader fields
    ComputeBuffer particleReadBuffer;
    ComputeBuffer particleWriteBuffer;
    ComputeBuffer obstacleBuffer;
    public ComputeShader computeShader;
    int kernelID;
    int groupSizeX;

    // Render fields
    public Shader particleShader;
    Material particleMaterial;
    Mesh quadMesh;
    Bounds renderBounds;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Particle[] particles = SpawnParticles();
        SetupComputeShader(particles);
        SetupRender();
    }

    Particle[] SpawnParticles()
    {
        Particle[] particles;
        particles = new Particle[particleCount];
        Vector2 radiusPadding = new Vector2(particleRadius, particleRadius);
        Vector2 spawnableRangeMin = spawnerPosition - (spawnerSize / 2) + radiusPadding;
        Vector2 spawnableRangeMax = spawnerPosition + (spawnerSize / 2) - radiusPadding;

        for (int i = 0; i < particleCount; i++)
        {
            float randomX = Random.Range(spawnableRangeMin.x, spawnableRangeMax.x);
            float randomY = Random.Range(spawnableRangeMin.y, spawnableRangeMax.y);

            Vector2 particlePosition = new Vector2(randomX, randomY);
            Vector2 particleDirection = Random.insideUnitCircle.normalized;

            particles[i] = new Particle
            {
                position = particlePosition,
                direction = particleDirection,
            };
        }

        return particles;
    }

    void SetupComputeShader(Particle[] particles)
    {
        int particleStride = (2 + 2) * sizeof(float);
        particleReadBuffer = new ComputeBuffer(particleCount, particleStride);
        particleWriteBuffer = new ComputeBuffer(particleCount, particleStride);
        particleReadBuffer.SetData(particles);

        int obstacleStride = (2 + 2) * sizeof(float);
        obstacleBuffer = new ComputeBuffer(obstacles.Length, obstacleStride);
        obstacleBuffer.SetData(obstacles);

        kernelID = computeShader.FindKernel("CSParticleSimulation2D");
        computeShader.SetInt("particleCount", particleCount);

        uint threadsX;
        computeShader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / threadsX);
    }

    void SetupRender()
    {
        CreateQuadMesh();
        particleMaterial = new Material(particleShader);
        renderBounds = new Bounds(
            Vector3.zero,
            new Vector3(boundsSize.x + 10f, boundsSize.y + 10f, 100f)
        );
    }

    // Update is called once per frame
    void Update()
    {
        float substepDeltaTime = (Time.deltaTime * timeScale) / simulationSubsteps;

        computeShader.SetFloat("temperature", temperature);
        computeShader.SetFloat("particleRadius", particleRadius);
        computeShader.SetVector("boundsSize", boundsSize);
        computeShader.SetInt("obstacleCount", obstacles.Length);
        obstacleBuffer.SetData(obstacles);
        computeShader.SetBuffer(kernelID, "obstacleBuffer", obstacleBuffer);

        for (int simulationSubstep = 0; simulationSubstep < simulationSubsteps; simulationSubstep++)
        {
            computeShader.SetFloat("deltaTime", substepDeltaTime);
            computeShader.SetBuffer(kernelID, "particleReadBuffer", particleReadBuffer);
            computeShader.SetBuffer(kernelID, "particleWriteBuffer", particleWriteBuffer);
            computeShader.Dispatch(kernelID, groupSizeX, 1, 1);

            var tmp = particleReadBuffer;
            particleReadBuffer = particleWriteBuffer;
            particleWriteBuffer = tmp;
        }

        particleMaterial.SetBuffer("_Particles", particleReadBuffer);
        particleMaterial.SetFloat("_Radius", particleRadius);
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
        Gizmos.DrawWireCube(spawnerPosition, spawnerSize);
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        foreach (var obstacle in obstacles)
        {
            Gizmos.DrawWireCube(obstacle.position, obstacle.size);
        }
    }

    void CreateQuadMesh()
    {
        quadMesh = new Mesh();
        quadMesh.name = "InstancedQuad";

        // Define vertices and UVs for a quad centered at (0,0)
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
        };

        // UVs are used by the shader to "cut out" the circle shape.
        Vector2[] uvs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        // Two triangles to form the quad
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };

        quadMesh.vertices = vertices;
        quadMesh.uv = uvs;
        quadMesh.triangles = triangles;
        quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one);
    }

    void OnDisable()
    {
        if (particleReadBuffer != null)
        {
            particleReadBuffer.Release();
            particleReadBuffer = null;
        }
        if (particleWriteBuffer != null)
        {
            particleWriteBuffer.Release();
            particleWriteBuffer = null;
        }
        if (obstacleBuffer != null)
        {
            obstacleBuffer.Release();
            obstacleBuffer = null;
        }
    }
}
