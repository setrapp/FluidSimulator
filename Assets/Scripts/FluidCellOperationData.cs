using System;
using UnityEngine;

[Serializable]
public struct FluidCellOperationData
{
    public FluidCellIndex leftId;
    public FluidCellIndex rightId;
    public FluidCellIndex downId;
    public FluidCellIndex upId;
    public FluidCellIndex backId;
    public FluidCellIndex fowardId;
    public Vector3 advectIdVelocity;
    public FluidCellIndex advectPastId;
    public Vector3 advectSamplePercentages;
}

