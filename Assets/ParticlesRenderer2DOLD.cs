// using UnityEngine;

// public class ParticlesRenderer2DOLD : MonoBehaviour
// {
//     public ParticleSimulation2D particleSimulation2D;
//     public Material material;

//     // public ComputeShader computeShader;

//     // struct Vertex
//     // {
//     //     public Vector3 position;
//     //     public Vector2 uv;
//     // }

//     // Vertex[] vertices;

//     struct RenderParticle
//     {
//         public Vector2 position;
//     }

//     const int RENDER_PARTICLE_SIZE = 2 * sizeof(float);

//     // const int VERTEX_SIZE = 5 * sizeof(float);
//     ComputeBuffer renderParticlesBuffer;

//     // ComputeBuffer verticesBuffer;
//     // int kernelID;

//     // Start is called once before the first execution of Update after the MonoBehaviour is created
//     void Start()
//     {
//         int particleCount = particleSimulation2D.particleCount;
//         // int verticesPerParticle = 6;
//         // int vertexCount = particleCount * verticesPerParticle;
//         // vertices = new Vertex[vertexCount];

//         // for (int particleI = 0; particleI < particleCount; particleI++)
//         // {
//         //     int vertexStartI = particleI * verticesPerParticle;
//         //     vertices[vertexStartI].uv.Set(0, 0);
//         //     vertices[vertexStartI + 1].uv.Set(0, 1);
//         //     vertices[vertexStartI + 2].uv.Set(1, 1);
//         //     vertices[vertexStartI + 3].uv.Set(0, 0);
//         //     vertices[vertexStartI + 4].uv.Set(1, 1);
//         //     vertices[vertexStartI + 5].uv.Set(1, 0);
//         // }

//         renderParticlesBuffer = new ComputeBuffer(
//             particleSimulation2D.particleCount,
//             RENDER_PARTICLE_SIZE
//         );
//         // verticesBuffer = new ComputeBuffer(vertexCount, VERTEX_SIZE);

//         RenderParticle[] renderParticles = new RenderParticle[particleCount];
//         for (int particleI = 0; particleI < particleCount; particleI++)
//         {
//             renderParticles[particleI].position = particleSimulation2D
//                 .particles[particleI]
//                 .position;
//         }
//         renderParticlesBuffer.SetData(renderParticles);
//         // verticesBuffer.SetData(vertices);

//         // kernelID = computeShader.FindKernel("CSParticleRenderer");

//         // computeShader.SetBuffer(kernelID, "particlesBuffer", particlesBuffer);
//         // computeShader.SetBuffer(kernelID, "verticesBuffer", verticesBuffer);
//         // computeShader.SetFloat("particleRadius", particleSimulation2D.particleRadius);

//         // uint threadsX;
//         // computeShader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
//         // int groupSizeX = Mathf.CeilToInt((float)particleCount / threadsX);

//         material.SetBuffer("particlesBuffer", renderParticlesBuffer);
//         material.SetFloat("_ParticleRadius", particleSimulation2D.particleRadius);
//     }

//     void OnRenderObject()
//     {
//         material.SetPass(0);
//         Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleSimulation2D.particleCount);
//     }

//     // Update is called once per frame
//     void Update() { }

//     void OnDestroy()
//     {
//         if (renderParticlesBuffer != null)
//             renderParticlesBuffer.Release();
//     }
// }
