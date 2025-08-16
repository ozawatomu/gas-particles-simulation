using System.Collections.Generic;
using Shapes;
using Tomu.Helpers;
using UnityEngine;

public class SpatialPartitioningTest : ImmediateModeShapeDrawer
{
    [Header("Main Settings")]
    [Min(0.1f)]
    public float radius = 1;

    [Min(0)]
    public int particleCount = 1000;

    [Header("Visual Settings")]
    [Min(0)]
    public float radiusThickness = 0.5f;

    [Min(0)]
    public float particleRadius = 0.05f;

    [Min(0)]
    public float gridThickness = 0.5f;
    public Color gridColor = new Color(47, 47, 47);
    public Color nonSelectedColor = new Color(47, 47, 47);
    public Color selectedColor = new Color(0.7529413f, 0.3490196f, 0.3294118f);
    public Color radiusColor = new Color(0.4352942f, 0.5294118f, 0.7764707f);

    Rect cameraWorldBounds;
    Vector2 mousePosition;
    Vector2[] particlePositions;
    (int particleI, uint cellKey)[] spatialLookup;
    int[] startIndices;
    List<int> selectedParticles;

    public static readonly Vector2Int[] CellOffsets =
    {
        new Vector2Int(-1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 0),
        new Vector2Int(1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
    };

    void Start()
    {
        cameraWorldBounds = CameraHelper.GetCameraWorldBounds(Camera.main);
        SpawnParticles();
    }

    void Update()
    {
        mousePosition = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateSpatialLookup();
        selectedParticles = GetParticlesToCheck(mousePosition);
    }

    void SpawnParticles()
    {
        particlePositions = new Vector2[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            float randomX = Random.Range(cameraWorldBounds.xMin, cameraWorldBounds.xMax);
            float randomY = Random.Range(cameraWorldBounds.yMin, cameraWorldBounds.yMax);

            particlePositions[i] = new Vector2(randomX, randomY);
        }
    }

    void UpdateSpatialLookup()
    {
        spatialLookup = new (int, uint)[particleCount]; // (particleI, cellKey) pairs
        startIndices = new int[particleCount]; // Start index for spatial lookup for each cell key
        for (int particleI = 0; particleI < particlePositions.Length; particleI++)
        {
            Vector2 position = particlePositions[particleI];
            Vector2Int cellCoordinates = GetCellCoordinates(position);
            uint cellHash = GetCellHash(cellCoordinates);
            uint cellKey = GetCellKeyFromHash(cellHash);
            spatialLookup[particleI] = (particleI, cellKey);
            startIndices[particleI] = int.MaxValue;
        }

        System.Array.Sort(spatialLookup, (a, b) => a.cellKey.CompareTo(b.cellKey));

        for (int particleI = 0; particleI < particlePositions.Length; particleI++)
        {
            uint cellKey = spatialLookup[particleI].cellKey;
            uint previousCellKey =
                particleI == 0 ? uint.MaxValue : spatialLookup[particleI - 1].cellKey;
            if (cellKey != previousCellKey)
            {
                startIndices[cellKey] = particleI;
            }
        }
    }

    List<int> GetParticlesToCheck(Vector2 position)
    {
        List<int> particlesToCheck = new List<int>();
        Vector2Int cellCoordinates = GetCellCoordinates(position);

        for (int cellOffsetI = 0; cellOffsetI < CellOffsets.Length; cellOffsetI++)
        {
            Vector2Int cellOffset = CellOffsets[cellOffsetI];
            uint cellHash = GetCellHash(cellCoordinates + cellOffset);
            uint cellKey = GetCellKeyFromHash(cellHash);
            int cellStartIndex = startIndices[cellKey];

            for (
                int spatialLookupI = cellStartIndex;
                spatialLookupI < particleCount;
                spatialLookupI++
            )
            {
                var spatialLookupValue = spatialLookup[spatialLookupI];

                if (spatialLookupValue.cellKey != cellKey)
                    break;

                int particleI = spatialLookupValue.particleI;
                particlesToCheck.Add(particleI);
            }
        }

        return particlesToCheck;
    }

    Vector2Int GetCellCoordinates(Vector2 position)
    {
        int cellX = (int)(position.x / radius);
        int cellY = (int)(position.y / radius);
        return new Vector2Int(cellX, cellY);
    }

    uint GetCellHash(Vector2Int cellCoordinates)
    {
        uint a = (uint)cellCoordinates.x * 15823;
        uint b = (uint)cellCoordinates.y * 9737333;
        return a + b;
    }

    uint GetCellKeyFromHash(uint hash)
    {
        return hash % (uint)particleCount;
    }

    public override void DrawShapes(Camera cam)
    {
        using (Draw.Command(cam))
        {
            Draw.LineGeometry = LineGeometry.Billboard;
            Draw.ThicknessSpace = ThicknessSpace.Noots;
            Draw.Color = gridColor;
            Draw.Thickness = gridThickness;

            float xMin = cameraWorldBounds.xMin;
            float xMax = cameraWorldBounds.xMax;
            float yMin = cameraWorldBounds.yMin;
            float yMax = cameraWorldBounds.yMax;

            for (float x = 0; x <= xMax + gridThickness; x += radius)
            {
                Draw.Line(new Vector2(x, yMin), new Vector2(x, yMax));
            }

            for (float y = 0; y <= yMax + gridThickness; y += radius)
            {
                Draw.Line(new Vector2(xMin, y), new Vector2(xMax, y));
            }

            for (float x = 0; x >= xMin - gridThickness; x -= radius)
            {
                Draw.Line(new Vector2(x, yMin), new Vector2(x, yMax));
            }

            for (float y = 0; y >= yMin - gridThickness; y -= radius)
            {
                Draw.Line(new Vector2(xMin, y), new Vector2(xMax, y));
            }

            if (particlePositions != null)
            {
                for (int particleI = 0; particleI < particleCount; particleI++)
                {
                    if (selectedParticles.Contains(particleI))
                    {
                        Draw.Color = selectedColor;
                    }
                    else
                    {
                        Draw.Color = nonSelectedColor;
                    }
                    Draw.Disc(particlePositions[particleI], particleRadius);
                }
            }

            Draw.Ring(mousePosition, radius, radiusThickness, radiusColor);
        }
    }
}
