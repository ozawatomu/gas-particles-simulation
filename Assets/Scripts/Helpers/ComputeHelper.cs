using System.Runtime.InteropServices;
using UnityEngine;

namespace Tomu.Helpers
{
    public static class ComputeHelper
    {
        public static void ReleaseComputeBuffers(params ComputeBuffer[] computeBuffers)
        {
            for (int computeBufferI = 0; computeBufferI < computeBuffers.Length; computeBufferI++)
            {
                if (computeBuffers[computeBufferI] != null)
                {
                    computeBuffers[computeBufferI].Release();
                    computeBuffers[computeBufferI] = null;
                }
            }
        }

        public static void ReleaseRenderTextures(params RenderTexture[] renderTextures)
        {
            for (int renderTextureI = 0; renderTextureI < renderTextures.Length; renderTextureI++)
            {
                if (renderTextures[renderTextureI] != null)
                {
                    renderTextures[renderTextureI].Release();
                    renderTextures[renderTextureI] = null;
                }
            }
        }

        public static ComputeBuffer CreateBuffer<T>(int count)
        {
            int stride = GetStride<T>();
            return new ComputeBuffer(count, stride);
        }

        public static RenderTexture CreateRenderTexture(int width, int height, int depth)
        {
            RenderTexture renderTexture = new RenderTexture(width, height, depth);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
            return renderTexture;
        }

        public static int GetStride<T>()
        {
            return Marshal.SizeOf(typeof(T));
        }

        public static void Dispatch(
            ComputeShader computeShader,
            int kernelIndex,
            int numIterationsX,
            int numIterationsY = 1,
            int numIterationsZ = 1
        )
        {
            Vector3Int threadGroupSizes = GetThreadGroupSizes(computeShader, kernelIndex);
            int groupSizeX = Mathf.CeilToInt(numIterationsX / (float)threadGroupSizes.x);
            int groupSizeY = Mathf.CeilToInt(numIterationsY / (float)threadGroupSizes.y);
            int groupSizeZ = Mathf.CeilToInt(numIterationsZ / (float)threadGroupSizes.z);
            computeShader.Dispatch(kernelIndex, groupSizeX, groupSizeY, groupSizeZ);
        }

        public static Vector3Int GetThreadGroupSizes(ComputeShader computeShader, int kernelIndex)
        {
            uint x,
                y,
                z;
            computeShader.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
            return new Vector3Int((int)x, (int)y, (int)z);
        }

        public static void SetBuffer(
            ComputeShader computeShader,
            ComputeBuffer buffer,
            string nameID,
            params int[] kernelIndices
        )
        {
            for (int kernelI = 0; kernelI < kernelIndices.Length; kernelI++)
            {
                computeShader.SetBuffer(kernelIndices[kernelI], nameID, buffer);
            }
        }

        public static void SetTexture(
            ComputeShader compute,
            Texture texture,
            string nameID,
            params int[] kernelIndices
        )
        {
            for (int kernelI = 0; kernelI < kernelIndices.Length; kernelI++)
            {
                compute.SetTexture(kernelIndices[kernelI], nameID, texture);
            }
        }
    }
}
