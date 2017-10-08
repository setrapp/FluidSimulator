using System;
using UnityEngine;

[Serializable]
public class FluidInfo
{
	public FluidParameters fluidParameters;
	public CellParameters cellParameters;
	public OperationParameters operationParameters;
	public OperationFlags operationFlags;
	public VisualizationFlags visualizationFlags;
}

[Serializable]
public class FluidParameters
{
	public Transform container;
	[Range(0, 128)]
	public int gridSize = 16;
	public float physicalSize = 16;
}

[Serializable]
public class CellParameters
{
	public FluidCell defaultCell;
	public float cellMaxDensity = 1;
	public float cellMaxSpeed = 20;
	public float cellMass = 1;
	[HideInInspector]
	public float cellSize = 1;
}

[Serializable]
public class OperationParameters
{
	public BoundaryCondition boundaryCondition = BoundaryCondition.Empty;
	public float diffusionRate = 0.05f;
	public int relaxationIterations = 20;
}

[Serializable]
public class VisualizationFlags
{
	public bool solidVisible = true;
	public bool outlineVisible = false;
	public bool centerVisible = false;
	public bool densityVisible = true;
	public bool velocityVisible = true;
}

[Serializable]
public class OperationFlags
{
	public bool applyExternals = true;
	public bool diffuse = true;
	public bool advect = true;
	public bool handleDivergence = true;
	public bool clampData = true;
}

public enum BoundaryCondition
{
	Empty = 0
	//Contain TODO Implement
	//Wrap TODO Implement
	//Leak TODO Implement
}