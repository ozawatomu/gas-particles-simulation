using UnityEngine;

namespace Tomu.SmoothingFunctions
{
    public static class SmoothingFunctions
    {
        public static float SpikyPow3(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            float value = radius - distance;
            return value * value * value;
        }

        public static float SpikyPow3Volume(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            return Mathf.PI * Mathf.Pow(radius, 5) / 10f;
        }

        public static float SpikyPow3Kernel(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            return SpikyPow3(radius, distance) / SpikyPow3Volume(radius, distance);
        }

        public static float SpikyPow3KernelDerivative(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            return -(30f / (Mathf.PI * Mathf.Pow(radius, 5))) * Mathf.Pow(radius - distance, 2);
        }

        public static float SpikyPow2(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            float value = radius - distance;
            return value * value;
        }

        public static float SpikyPow2Volume(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            return Mathf.PI * Mathf.Pow(radius, 4) / 6f;
        }

        public static float SpikyPow2Kernel(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            return SpikyPow2(radius, distance) / SpikyPow2Volume(radius, distance);
        }

        public static float SpikyPow2KernelDerivative(float radius, float distance)
        {
            if (distance >= radius)
                return 0;

            return -(12f / (Mathf.PI * Mathf.Pow(radius, 4))) * (radius - distance);
        }
    }
}
