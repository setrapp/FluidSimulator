using System;
using UnityEngine;

[Serializable]
public struct FluidCell
{
    public float density;
    public Vector3 velocity;
    public float rawDivergence;
    public float relaxedDivergence;

    // Keep this structure 16byte aligned
    private Vector2 padding;
}

[Serializable]
public struct FluidCellIndex
{
    public int x;
    public int y;
    public int z;

    public FluidCellIndex(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override string ToString()
    {
        return string.Format("({0}, {1}, {2})", x, y, z);
    }
}
