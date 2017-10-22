using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class FluidSampler : MonoBehaviour
{
	public FluidSimulator Simulator { get; private set; }
	FluidCell[] cellsSerial;
	FluidCell[,] cells;
	bool cellUpdateRequired = true;

	public void Initialize(FluidSimulator simulator)
	{
		if (this.Simulator != null)
		{
			Debug.LogError("Attempting to reinitialize FluidSampler.");
		}

		this.Simulator = simulator;

		int gridSize = simulator.fluidParameters.gridSize;
		cellsSerial = new FluidCell[gridSize * gridSize];
		cells = new FluidCell[gridSize, gridSize]; // TODO get rid of this and just rely on serial

		simulator.fluidUpdated.AddListener(FlagUpdateRequired);
	}

	void FlagUpdateRequired()
	{
		cellUpdateRequired = true;
	}

	void UpdateFluidForFrame()
	{
		if (Simulator == null || !cellUpdateRequired)
		{
			return;
		}

		Profiler.BeginSample("FluidSampler.UpdateFluidForFrame");
		Simulator.GetCells(ref cellsSerial);
		int gridSize = Simulator.fluidParameters.gridSize;
		for (int i = 0; i < gridSize; i++)
		{
			int iIndex = i * gridSize;
			for (int j = 0; j < gridSize; j++)
			{
				cells[i, j] = cellsSerial[iIndex + j];
			}
		}
		cellUpdateRequired = false;
		Profiler.EndSample();
	}

	Vector2 PositionOnGrid (Vector3 worldPosition)
	{
		// TODO this assumes the fluids center is at it's local origin ... either guarantee this, or find the actual center.
		Vector3 localPos = Simulator.transform.InverseTransformPoint(worldPosition);
		float gridCenter = Simulator.fluidParameters.gridSize / 2.0f;
		Vector3 gridPos = (localPos / Simulator.cellParameters.cellSize) + new Vector3(gridCenter, gridCenter, gridCenter);
		return gridPos;
	}

	Vector3 FluidVectorToWorld(Vector3 fluidVector)
	{
		return fluidVector / Simulator.cellParameters.cellSize;
	}

	public FluidCell SamplePoint(Vector3 worldPosition)
	{
		Profiler.BeginSample("FluidSampler.SamplePoint");
		UpdateFluidForFrame();

		Vector2 gridPos = PositionOnGrid(worldPosition);
		int gridSize = Simulator.fluidParameters.gridSize;
		if ((gridPos.x < 0 || gridPos.x >= gridSize) || (gridPos.y < 0 || gridPos.y >= gridSize))
		{
			return new FluidCell();
		}
		FluidCell sampledCell = cells[(int)gridPos.x, (int)gridPos.y];
		sampledCell.velocity /= Simulator.cellParameters.cellSize;

		Profiler.EndSample();
		return sampledCell;
	}

	public FluidCell SampleCircle(Vector3 worldPosition, float radius)
	{
		Profiler.BeginSample("FluidSampler.SampleCircle");
		UpdateFluidForFrame();

		// TODO Figure out how to test this.
		int gridSize = Simulator.fluidParameters.gridSize;
		Vector2 centerGridPos = PositionOnGrid(worldPosition);
		float gridRadius = PositionOnGrid(new Vector3(radius, radius, radius)).x;
		int xStart = (int)Mathf.Max(centerGridPos.x - gridRadius, 0);
		int xEnd = (int)Mathf.Min(centerGridPos.x + gridRadius, gridSize);
		int yStart = (int)Mathf.Max(centerGridPos.y - gridRadius, 0);
		int yEnd = (int)Mathf.Min(centerGridPos.y + gridRadius, gridSize);

		FluidCell totalSum = new FluidCell();
		FluidCell lineSum = new FluidCell();
		Vector3 zero = Vector3.zero;
		float sqrRadius = gridRadius * gridRadius;
		uint sampleCount = 0;

		// TODO Ensure that floating point precision doesn't ruin the result.
		for (int i = xStart; i < xEnd; i++)
		{
			lineSum.density = 0;
			lineSum.velocity = zero;
			float xSqrDist = i - centerGridPos.x;
			xSqrDist *= xSqrDist;
			for (int j = yStart; j < yEnd; j++)
			{
				float ySqrDist = j - centerGridPos.y;
				ySqrDist *= ySqrDist;
				if (xSqrDist + ySqrDist <= sqrRadius)
				{
					FluidCell sampledCell = cells[i, j];
					lineSum.density += sampledCell.density;
					lineSum.velocity += sampledCell.velocity;
					sampleCount++;
				}
			}
			totalSum.density += lineSum.density;
			totalSum.velocity += lineSum.velocity;
		}

		if (sampleCount > 0)
		{
			totalSum.density /= sampleCount;
			totalSum.velocity /= sampleCount;
		}

		totalSum.velocity /= Simulator.cellParameters.cellSize;

		Profiler.EndSample();
		return totalSum;
	}
}
