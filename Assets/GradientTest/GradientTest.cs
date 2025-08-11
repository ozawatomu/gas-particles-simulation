using Shapes;
using UnityEngine;
using UnityEngine.InputSystem;

public class GradientTest : ImmediateModeShapeDrawer
{
    [Min(0)]
    public int particleCount = 1000;

    [Range(0f, 10f)]
    public float smoothingRadius = 1.2f;

    [Range(0f, 1f)]
    public float mass = 1.0f;

    [Header("Visual")]
    [Min(0)]
    public float particleRadius = 0.05f;
    public Color particleColor = new Color(96, 142, 190);
    public Color smoothingRadiusRingColor = new Color(96, 142, 190);
    public float smoothingRadiusRingThickness = 0.7f;
    public Vector2Int textureResolution = new Vector2Int(1920, 1080);
    public bool showParticles = true;
    public bool showDensities = true;

    [Range(0f, 0.5f)]
    public float forceVectorMultiplier = 1f;

    [Header("Input Settings")]
    public float dragSensitivity = 0.01f;
    bool dragging;

    [Header("References")]
    public ComputeShader computeShader;
    public GameObject canvasQuad;

    Vector2[] particlePositions;
    Vector2[] particleForces;

    float[] particleDensities;
    RenderTexture outputTexture;
    ComputeBuffer particalPositionBuffer;
    int kernelID;
    int groupSizeX;
    int groupSizeY;

    void Start()
    {
        particlePositions = SpawnParticles();
        CalculateParticleDensities();
        CalculateParticleForces();
        SetupRenderTexture();
        SetupComputeShader();
    }

    Vector2[] SpawnParticles()
    {
        Vector2[] particlePositions;
        particlePositions = new Vector2[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float randomX = Random.Range(-19.2f / 2f, 19.2f / 2f) * 0.8f;
            float randomY = Random.Range(-10.8f / 2f, 10.8f / 2f) * 0.8f;

            particlePositions[i] = new Vector2(randomX, randomY);
        }

        return particlePositions;
    }

    void CalculateParticleDensities()
    {
        particleDensities = new float[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float density = 0f;
            Vector2 particleIPosition = particlePositions[i];
            for (int j = 0; j < particleCount; j++)
            {
                Vector2 particleJPosition = particlePositions[j];
                float particleDistanceFromPosition = (
                    particleIPosition - particleJPosition
                ).magnitude;
                if (particleDistanceFromPosition > smoothingRadius)
                    continue;
                float particleInfluence = SmoothingKernel(
                    smoothingRadius,
                    particleDistanceFromPosition
                );
                density += particleInfluence;
            }
            particleDensities[i] = density;
        }
    }

    void CalculateParticleForces()
    {
        particleForces = new Vector2[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            Vector2 particleForce = Vector2.zero;
            for (int j = 0; j < particleCount; j++)
            {
                if (i == j)
                    continue;

                Vector2 particleIPosition = particlePositions[i];
                Vector2 particleJPosition = particlePositions[j];

                Vector2 delta = particleJPosition - particleIPosition;
                float distance = delta.magnitude;
                float influence = SmoothingKernelDerivative(smoothingRadius, distance);
                particleForce += delta.normalized * influence / particleDensities[j];
            }
            particleForces[i] = particleForce;
        }
        float totalParticleForcesMagnitude = 0;
        foreach (Vector2 particleForce in particleForces)
        {
            totalParticleForcesMagnitude += particleForce.magnitude;
        }
        float averageParticleForcesMagnitude = totalParticleForcesMagnitude / particleCount;
        for (int i = 0; i < particleCount; i++)
        {
            particleForces[i] = particleForces[i] / averageParticleForcesMagnitude;
        }
    }

    void SetupRenderTexture()
    {
        outputTexture = new RenderTexture(textureResolution.x, textureResolution.y, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        Renderer quadRenderer = canvasQuad.GetComponent<Renderer>();
        quadRenderer.enabled = true;
        quadRenderer.material.SetTexture("_MainTex", outputTexture);
    }

    void SetupComputeShader()
    {
        int particlePositionStride = 2 * sizeof(float);
        particalPositionBuffer = new ComputeBuffer(particleCount, particlePositionStride);
        kernelID = computeShader.FindKernel("GenerateTexture");
        computeShader.SetTexture(kernelID, "Result", outputTexture);
        computeShader.SetInt("particleCount", particleCount);

        uint threadsX;
        uint threadsY;
        computeShader.GetKernelThreadGroupSizes(kernelID, out threadsX, out threadsY, out _);
        groupSizeX = Mathf.CeilToInt((float)textureResolution.x / threadsX);
        groupSizeY = Mathf.CeilToInt((float)textureResolution.y / threadsY);
    }

    void Update()
    {
        CalculateParticleForces();
        particalPositionBuffer.SetData(particlePositions);
        computeShader.SetBuffer(kernelID, "particalPositionBuffer", particalPositionBuffer);
        computeShader.SetFloat("smoothingRadius", smoothingRadius);
        computeShader.SetFloat("mass", mass);
        computeShader.SetBool("showDensities", showDensities);
        computeShader.Dispatch(kernelID, groupSizeX, groupSizeY, 1);

        var mouse = Mouse.current;
        if (mouse.leftButton.wasPressedThisFrame)
            dragging = true;
        if (mouse.leftButton.wasReleasedThisFrame)
            dragging = false;
        if (dragging)
        {
            float deltaX = mouse.delta.ReadValue().x;
            smoothingRadius += deltaX * dragSensitivity;
            smoothingRadius = Mathf.Max(0, smoothingRadius);
        }
    }

    public override void DrawShapes(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.ThicknessSpace = ThicknessSpace.Noots;
            Draw.Color = particleColor;

            if (showParticles)
            {
                for (int particleI = 0; particleI < particleCount; particleI++)
                {
                    Vector2 particlePosition = particlePositions[particleI];
                    Vector2 particleForce = particleForces[particleI];
                    Draw.Disc(particlePosition, particleRadius);
                    Draw.Line(
                        particlePosition,
                        particlePosition + particleForce * forceVectorMultiplier
                    );
                }
            }

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Draw.Color = smoothingRadiusRingColor;
                Draw.Ring(new Vector2(0, 0), smoothingRadius, smoothingRadiusRingThickness);
                Draw.Color = new Color(1f, 1f, 1f);
                Draw.Text(new Vector2(0, 0), smoothingRadius.ToString("F2"), TextAlign.Center, 4f);
            }
        }
    }

    public void OnDestroy()
    {
        if (particalPositionBuffer != null)
        {
            particalPositionBuffer.Release();
            particalPositionBuffer = null;
        }
    }

    float SmoothingKernel(float radius, float distance)
    {
        if (distance < radius)
        {
            float volume = Mathf.PI * Mathf.Pow(radius, 5) / 10;
            float value = radius - distance;
            return value * value * value / volume;
        }
        return 0;
    }

    float SmoothingKernelDerivative(float radius, float distance)
    {
        if (distance >= radius)
            return 0;

        return -(30f / (Mathf.PI * Mathf.Pow(radius, 5))) * Mathf.Pow(radius - distance, 2);
    }
}
