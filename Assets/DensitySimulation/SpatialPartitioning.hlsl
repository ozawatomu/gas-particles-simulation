static const int2 CELL_OFFSETS[9] =
{
	int2(-1, 1),
	int2(0, 1),
	int2(1, 1),
	int2(-1, 0),
	int2(0, 0),
	int2(1, 0),
	int2(-1, -1),
	int2(0, -1),
	int2(1, -1),
};

static const uint PRIME_1 = 15823;
static const uint PRIME_2 = 9737333;

int2 GetCellCoordinates(float2 position, float radius)
{
	return (int2)floor(position / radius);
}

uint GetCellHash(int2 cellCoordinates)
{
	cellCoordinates = (uint2)cellCoordinates;
	uint a = cellCoordinates.x * PRIME_1;
	uint b = cellCoordinates.y * PRIME_2;
	return (a + b);
}

uint GetCellKeyFromHash(uint cellHash, uint tableSize)
{
	return cellHash % tableSize;
}