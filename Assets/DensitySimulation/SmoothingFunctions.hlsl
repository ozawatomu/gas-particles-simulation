static const float PI = 3.14159265358979323846;

inline float pow2(float x) { return x * x; }
inline float pow3(float x) { return x * x * x; }
inline float pow4(float x) { return x * x * x * x; }

inline float SpikyPow2Kernel(float radius, float distance)
{
    if (distance >= radius) return 0.0;

    float numerator = 6.0 * pow2(radius - distance);
    float denominator = PI * pow4(radius);
    return numerator / denominator;
}

inline float SpikyPow2KernelDerivative(float radius, float distance)
{
    if (distance >= radius) return 0.0;

    float numerator = -12.0 * (radius - distance);
    float denominator = PI * pow4(radius);
    return numerator / denominator;
}
