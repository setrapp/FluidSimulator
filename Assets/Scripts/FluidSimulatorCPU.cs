﻿using System;
using UnityEngine;

public class FluidSimulatorCPU : FluidSimulator {
	[Header("Cell Rendering")]
	[SerializeField]
	public FluidCellRenderer cellPrefab;
	FluidCellRenderer[,] cells;
	FluidCell[,] cellBuffer1;
	FluidCell[,] cellBuffer2;
	FluidCell[,] inCells;
	FluidCell[,] outCells;
	private FluidCell[,] externalAdditions;
	private Vector3[,] reverseVelocities;
	public FluidCellOperationData[,] operationData;

	protected override string generatePoolCells()
	{
		string result = null;

		if (cellPrefab != null)
		{
			float halfPoolSize = info.fluidParameters.gridSize / 2;
			cells = new FluidCellRenderer[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			externalAdditions = new FluidCell[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			reverseVelocities = new Vector3[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			operationData = new FluidCellOperationData[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			for (int i = 0; i < info.fluidParameters.gridSize; i++)
			{
				for (int j = 0; j < info.fluidParameters.gridSize; j++)
				{
					Vector3 pos = transform.position + new Vector3(CellSize * (i - halfPoolSize), CellSize * (j - halfPoolSize), 0);
					FluidCellRenderer newCell = (((GameObject)Instantiate(cellPrefab.gameObject, pos, Quaternion.identity, pool.transform)).GetComponent<FluidCellRenderer>());
					newCell.Initialize(this, info.cellParameters.defaultCell, i, j);
					newCell.transform.localScale = new Vector3(CellSize, CellSize, CellSize);
					cells[i, j] = newCell;

					externalAdditions[i, j] = new FluidCell();
					reverseVelocities[i, j] = Vector3.zero;
					operationData[i, j] = new FluidCellOperationData();
				}
			}
		}
		else
		{
			result = string.Format("No Cell Prefab provided to {0}'s pool.", gameObject.name);
		}

		return result;
	}

	protected override string initializeBuffers()
	{
		string result = null;

		cellBuffer1 = new FluidCell[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
		cellBuffer2 = new FluidCell[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				cellBuffer1[i, j] = new FluidCell();
				cellBuffer2[i, j] = new FluidCell();
			}
		}

		inCells = cellBuffer1;
		outCells = cellBuffer2;

		return result;
	}

	protected override void prepareNextFrame()
	{
		// Clear out external additions.
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				externalAdditions[i, j].density = 0;
				externalAdditions[i, j].velocity = Vector3.zero;
			}
		}
	}

	protected override void addExternal(FluidCellIndex index, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius)
	{
		if (densityChange == 0 && force.sqrMagnitude == 0)
		{
			return;
		}

		int densityCellRadius = (int)Mathf.Max(densityChangeRadius / CellSize, 0);
		int forceCellRadius = (int)Mathf.Max(forceRadius / CellSize, 0);
		int applyCellRadius = Mathf.Max(densityCellRadius, forceCellRadius);

		float densityFalloff = densityChange / (densityCellRadius + 1);
		Vector3 forceFalloff = force / (forceCellRadius + 1);

		for (int i = (int)Mathf.Max(index.x- applyCellRadius, 0); i < Mathf.Min(index.x + applyCellRadius, info.fluidParameters.gridSize - 1); i++)
		{
			for (int j = (int)Mathf.Max(index.y - applyCellRadius, 0); j < Mathf.Min(index.y + applyCellRadius, info.fluidParameters.gridSize - 1); j++)
			{
				int distance = Mathf.Max(Mathf.Abs(i - index.x), Mathf.Abs(j - index.y));
				if (distance <= densityCellRadius)
				{
					externalAdditions[i, j].density = densityChange - (densityFalloff * distance);
				}
				if (distance <= forceCellRadius)
				{
					externalAdditions[i, j].velocity = force - (forceFalloff * distance);
				}
			}
		}
	}

	protected override void setExternal(FluidCellIndex index, FluidCell applyCell)
	{
		externalAdditions[index.x, index.y].density = applyCell.density;
		externalAdditions[index.x, index.y].velocity = applyCell.velocity;
	}

	protected override FluidCell getExternal(FluidCellIndex index)
	{
		return externalAdditions[index.x, index.y];
	}

	protected override FluidCell getCell(FluidCellIndex index)
	{
		return cells[index.x, index.y].cell;
	}

	protected override FluidCellOperationData getCellOperationData(FluidCellIndex index)
	{
		return operationData[index.x, index.y];
	}

	protected override void applyExternalAdditions()
	{
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				outCells[i, j].density = inCells[i, j].density + externalAdditions[i, j].density * Time.deltaTime;
				outCells[i, j].velocity = inCells[i, j].velocity + (externalAdditions[i, j].velocity / info.cellParameters.cellMass) * Time.deltaTime;
			}
		}
	}

	protected override void diffuse()
	{
		// TODO There is probably a better way to do this than on ever iteration.
		float dtDiffusion = (info.operationParameters.diffusionRate * Time.deltaTime * info.fluidParameters.gridSize * info.fluidParameters.gridSize) / info.operationParameters.relaxationIterations;
		//Debug.Log("CPU " + dtDiffusion);

		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				int left = Mathf.Max(i - 1, 0);
				int right = Mathf.Min(i + 1, info.fluidParameters.gridSize - 1);
				int down = Mathf.Max(j - 1, 0);
				int up = Mathf.Min(j + 1, info.fluidParameters.gridSize - 1);

				outCells[i, j].density = (inCells[i, j].density +
					dtDiffusion * (inCells[left, j].density + inCells[right, j].density +
					inCells[i, down].density + inCells[i, up].density)) / (1 + (4 * dtDiffusion));

				outCells[i, j].velocity = (inCells[i, j].velocity +
					dtDiffusion * (inCells[left, j].velocity + inCells[right, j].velocity +
					inCells[i, down].velocity + inCells[i, up].velocity)) / (1 + (4 * dtDiffusion));

				operationData[i, j].leftId = new FluidCellIndex(left, j, 0);
				operationData[i, j].rightId = new FluidCellIndex(right, j, 0);
				operationData[i, j].downId = new FluidCellIndex(i, down, 0);
				operationData[i, j].upId = new FluidCellIndex(i, up, 0);
			}
		}
	}

	protected override void advect()
	{
		int leftIndex, rightIndex, downIndex, upIndex;
		float leftPortion, rightPortion, downPortion, upPortion;
		Vector2 oldGridPosition;

		float reciprocalCellSize = 1.0f / CellSize;

		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				Vector2 idVelocity = ((Vector2)inCells[i, j].velocity * reciprocalCellSize);
				oldGridPosition = new Vector2(i, j) - idVelocity;
				oldGridPosition.x = Mathf.Clamp(oldGridPosition.x, 1.5f, info.fluidParameters.gridSize - 1.5f);
				oldGridPosition.y = Mathf.Clamp(oldGridPosition.y, 1.5f, info.fluidParameters.gridSize - 1.5f);

				leftIndex = (int)oldGridPosition.x;
				rightIndex = leftIndex + 1;
				downIndex = (int)oldGridPosition.y;
				upIndex = downIndex + 1;

				rightPortion = oldGridPosition.x - leftIndex;
				leftPortion = 1 - rightPortion;
				upPortion = oldGridPosition.y - downIndex;
				downPortion = 1 - upPortion;

				outCells[i, j].density =
					leftPortion * (downPortion * inCells[leftIndex, downIndex].density + upPortion * inCells[leftIndex, upIndex].density) +
					rightPortion * (downPortion * inCells[rightIndex, downIndex].density + upPortion * inCells[rightIndex, upIndex].density);

				outCells[i, j].velocity =
					leftPortion * (downPortion * inCells[leftIndex, downIndex].velocity + upPortion * inCells[leftIndex, upIndex].velocity) +
					rightPortion * (downPortion * inCells[rightIndex, downIndex].velocity + upPortion * inCells[rightIndex, upIndex].velocity);
				  
				operationData[i, j].advectIdVelocity = new Vector3(idVelocity.x, idVelocity.y, 0);
				operationData[i, j].advectPastId = new FluidCellIndex((int)oldGridPosition.x, (int)oldGridPosition.y, 0);
				operationData[i, j].advectSamplePercentages = new Vector3(rightPortion, upPortion, 0);
			}
		}
	}

	protected override void computeDivergence()
	{
		float reciprocalPoolSize = 1.0f / info.fluidParameters.gridSize;

		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				int left = Mathf.Max(i - 1, 0);
				int right = Mathf.Min(i + 1, info.fluidParameters.gridSize - 1);
				int down = Mathf.Max(j - 1, 0);
				int up = Mathf.Min(j + 1, info.fluidParameters.gridSize - 1);

				outCells[i, j].rawDivergence = -0.5f * reciprocalPoolSize *
					((inCells[right, j].velocity.x - inCells[left, j].velocity.x) +
					(inCells[i, up].velocity.y - inCells[i, down].velocity.y));
				outCells[i, j].relaxedDivergence = 0;
				outCells[i, j].density = inCells[i, j].density;
				outCells[i, j].velocity = inCells[i, j].velocity;
			}
		}
	}

	protected override void relaxDivergence()
	{
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				int left = Mathf.Max(i - 1, 0);
				int right = Mathf.Min(i + 1, info.fluidParameters.gridSize - 1);
				int down = Mathf.Max(j - 1, 0);
				int up = Mathf.Min(j + 1, info.fluidParameters.gridSize - 1);

				outCells[i, j].relaxedDivergence = (inCells[i, j].rawDivergence +
					inCells[left, j].relaxedDivergence + inCells[right, j].relaxedDivergence +
					inCells[i, down].relaxedDivergence + inCells[i, up].relaxedDivergence) * 0.25f;
				outCells[i, j].velocity = inCells[i, j].velocity;
			}
		}
	}

	protected override void removeDivergence()
	{
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				int left = Mathf.Max(i - 1, 0);
				int right = Mathf.Min(i + 1, info.fluidParameters.gridSize - 1);
				int down = Mathf.Max(j - 1, 0);
				int up = Mathf.Min(j + 1, info.fluidParameters.gridSize - 1);

				outCells[i, j].velocity = inCells[i, j].velocity - (0.5f * info.fluidParameters.gridSize *
					new Vector3(inCells[right, j].relaxedDivergence - inCells[left, j].relaxedDivergence,
					inCells[i, up].relaxedDivergence - inCells[i, down].relaxedDivergence, 0));
				outCells[i, j].density = inCells[i, j].density;
			}
		}
	}

	protected override void clampData()
	{
		float maxSqrSpeed = info.cellParameters.cellMaxSpeed * info.cellParameters.cellMaxSpeed;

		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				outCells[i, j].density = Mathf.Clamp(inCells[i, j].density, 0, info.cellParameters.cellMaxDensity);

				float sqrSpeed = outCells[i, j].velocity.sqrMagnitude;
				outCells[i, j].velocity = inCells[i, j].velocity;
				if (sqrSpeed > maxSqrSpeed)
				{
					outCells[i, j].velocity = (outCells[i, j].velocity / (Mathf.Sqrt(sqrSpeed))) * info.cellParameters.cellMaxSpeed;
				}
			}
		}
	}

	protected override void containBoundaries()
	{
		int sampleCorner;
		float cornerSample;
		Vector3 sample;
		int maxIndex = info.fluidParameters.gridSize - 1;
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			sampleCorner = (i == 0) ? 1 : (i == info.fluidParameters.gridSize - 1) ? -1 : 0;

			// Bottom
			sample = outCells[i + sampleCorner, 1].velocity;
			cornerSample = (sampleCorner == 0) ? sample.x : Mathf.Abs(sample.x) * sampleCorner;
			outCells[i, 0].velocity = Vector3.zero;// new Vector3(cornerSample, Mathf.Abs(sample.y), sample.z);
			outCells[i, 0].density = 0;

			// Top
			sample = outCells[i + sampleCorner, info.fluidParameters.gridSize - 2].velocity;
			cornerSample = (sampleCorner == 0) ? sample.x : Mathf.Abs(sample.x) * sampleCorner;
			outCells[i, maxIndex].velocity = Vector3.zero;// new Vector3(cornerSample, -Mathf.Abs(sample.y), sample.z);
			outCells[i, maxIndex].density = 0;


			// Left
			sample = outCells[1, i + sampleCorner].velocity;
			cornerSample = (sampleCorner == 0) ? sample.y : Mathf.Abs(sample.y) * sampleCorner;
			outCells[0, i].velocity = Vector3.zero;// new Vector3(Mathf.Abs(sample.x), cornerSample, sample.z);
			outCells[0, i].density = 0;


			// Right
			sample = outCells[info.fluidParameters.gridSize - 2, i + sampleCorner].velocity;
			cornerSample = (sampleCorner == 0) ? sample.y : Mathf.Abs(sample.y) * sampleCorner;
			outCells[maxIndex, i].velocity = Vector3.zero;// new Vector3(-Mathf.Abs(sample.x), cornerSample, sample.z);
			outCells[maxIndex, i].density = 0;

		}
	}

	protected override void applyCells()
	{
		UnityEngine.Profiling.Profiler.BeginSample("ApplyCells");
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				//cells[i, j].cell = outCells[i, j];
				cells[i, j].cell.density = inCells[i, j].density;
				cells[i, j].cell.velocity = inCells[i, j].velocity;
				cells[i, j].cell.rawDivergence = inCells[i, j].rawDivergence;
				cells[i, j].cell.relaxedDivergence = inCells[i, j].relaxedDivergence;

			}
		}
		UnityEngine.Profiling.Profiler.EndSample();
	}

	protected override void reset()
	{
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				inCells[i, j].density = outCells[i, j].density = cells[i, j].cell.density = info.cellParameters.defaultCell.density;
				inCells[i, j].velocity = outCells[i, j].velocity = cells[i, j].cell.velocity = info.cellParameters.defaultCell.velocity;
				inCells[i, j].rawDivergence = outCells[i, j].rawDivergence = cells[i, j].cell.rawDivergence = info.cellParameters.defaultCell.rawDivergence;
				inCells[i, j].relaxedDivergence = outCells[i, j].relaxedDivergence = cells[i, j].cell.relaxedDivergence = info.cellParameters.defaultCell.relaxedDivergence;
			}
		}
	}
	protected override void pause()	{ }
	protected override void step() { }

	protected override void swapBuffers()
	{
		if (operationPassNumber % 2 == 0)
		{
			inCells = cellBuffer1;
			outCells = cellBuffer2;
		}
		else
		{
			inCells = cellBuffer2;
			outCells = cellBuffer1;
		}
	}
}
