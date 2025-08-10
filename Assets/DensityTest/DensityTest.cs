using Shapes;
using UnityEngine;
using UnityEngine.Rendering;

public class DensityTest : ImmediateModeShapeDrawer
{
    [Range(0f, 0.5f)]
    public float particleSpacing = 0.12f;

    [Range(0, 100)]
    public int particleRows = 42;
    public Vector2 smoothingTestPosition;

    [Range(0f, 10f)]
    public float smoothingRadius = 1.2f;

    [Header("Visual")]
    [Range(0f, 0.1f)]
    public float particleRadius = 0.056f;

    [Range(0f, 10f)]
    public float smoothingRadiusRingThickness = 0.7f;

    public Color lowColor = new Color(47, 47, 47);
    public Color highlightColor = new Color(96, 142, 190);

    Vector2[] particlePositions;
    float density = 0f;

    public void Update()
    {
        particlePositions = new Vector2[particleRows * particleRows];
        float rOffset = particleRows - 1;
        float sOffset = rOffset * 0.5f;
        float gridStart = -(rOffset * particleRadius + sOffset * particleSpacing);
        for (int rowI = 0; rowI < particleRows; rowI++)
        {
            for (int colI = 0; colI < particleRows; colI++)
            {
                int particleI = particleRows * rowI + colI;
                float particleX = gridStart + rowI * 2 * particleRadius + rowI * particleSpacing;
                float particleY = gridStart + colI * 2 * particleRadius + colI * particleSpacing;
                particlePositions[particleI] = new Vector2(particleX, particleY);
            }
        }

        density = CalculateDensity(smoothingTestPosition);
    }

    public override void DrawShapes(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.LineGeometry = LineGeometry.Billboard;
            Draw.ThicknessSpace = ThicknessSpace.Noots;
            Draw.Color = lowColor;
            Draw.Thickness = 0.25f;

            foreach (Vector2 particlePosition in particlePositions)
            {
                float particleDistanceFromTestPosition = (
                    particlePosition - smoothingTestPosition
                ).magnitude;

                float particleInfluence = SmoothingKernel(
                    smoothingRadius,
                    particleDistanceFromTestPosition
                );
                Draw.Color = Color.Lerp(lowColor, highlightColor, particleInfluence);
                Draw.Disc(particlePosition, particleRadius);
                if (particleDistanceFromTestPosition <= smoothingRadius)
                {
                    Draw.Color = new Color(1f, 1f, 1f, 0.5f);
                    Draw.Ring(particlePosition, particleRadius, 0.05f);
                }
            }

            Draw.Color = highlightColor;
            Draw.Ring(smoothingTestPosition, smoothingRadius, smoothingRadiusRingThickness);
            Draw.Ring(smoothingTestPosition, particleRadius, 0.05f);
            Draw.Color = new Color(1f, 1f, 1f);
            Draw.Text(
                new Vector2(-8.5f, 4f),
                "Density: " + density.ToString("F2"),
                TextAlign.TopLeft,
                4f
            );
        }
    }

    float CalculateDensity(Vector2 position)
    {
        float density = 0f;
        foreach (Vector2 particlePosition in particlePositions)
        {
            float particleDistanceFromPosition = (particlePosition - position).magnitude;
            if (particleDistanceFromPosition > smoothingRadius)
                continue;
            float particleInfluence = SmoothingKernel(
                smoothingRadius,
                particleDistanceFromPosition
            );
            density += particleInfluence;
        }
        return density;
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
}
