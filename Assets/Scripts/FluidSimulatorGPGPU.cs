using System.Runtime.InteropServices;
using UnityEngine;

public class FluidSimulatorGPGPU : FluidSimulator
{
	[Header("Cell Rendering")]
	[SerializeField]
	public FluidCellRenderer cellPrefab;
	FluidCellRenderer[,] cells;
	FluidCell[,] cellBuffer1;
	FluidCell[,] cellBuffer2;
	FluidCell[,] inCells;
	FluidCell[,] outCells;
	private FluidCell[,] externalAdditions;

	int clampDataKernel;
	int applyExternalsKernel;
	int diffuseKernel;
	int advectKernel;
	int computeDivergenceKernel;
	int relaxDivergenceKernel;
	int removeDivergenceKernel;
	int zeroedBoundariesKernel;
	int wrapBoundariesKernel;

	[Header("Computation")]
	public ComputeShader fluidComputer;
	uint[] threadsPerGroup;
	int[] threadGroups;
	ComputeBuffer threadGroupsBuffer;
	FluidCell[,] initialCells;

	private ComputeBuffer fluidBuffer1;
	private ComputeBuffer fluidBuffer2;
	private ComputeBuffer inFluidBuffer;
	private ComputeBuffer outFluidBuffer;

	private ComputeBuffer externalAdditionBuffer;
	public Vector3[,] reverseVelocities;

	public FluidCellOperationData[,] operationData;
	public ComputeBuffer operationDataBuffer;

	protected override string generatePoolCells()
	{
		string result = null;

		if (cellPrefab == null)
		{
			result = string.Format("No Cell Prefab provided to {0}'s pool.", gameObject.name);
		}
		else if (fluidComputer == null)
		{
			result = string.Format("No Fluid Computer provided to {0}'s pool.", gameObject.name);
		}
		else
		{
			float halfPoolSize = info.fluidParameters.gridSize / 2;
			cells = new FluidCellRenderer[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			externalAdditions = new FluidCell[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			reverseVelocities = new Vector3[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
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
				}
			}
		}

		return result;
	}

	protected override string initializeBuffers()
	{
		clampDataKernel = fluidComputer.FindKernel("ClampData");
		applyExternalsKernel = fluidComputer.FindKernel("ApplyExternals");
		diffuseKernel = fluidComputer.FindKernel("Diffuse");
		advectKernel = fluidComputer.FindKernel("Advect");
		computeDivergenceKernel = fluidComputer.FindKernel("ComputeDivergence");
		relaxDivergenceKernel = fluidComputer.FindKernel("RelaxDivergence");
		removeDivergenceKernel = fluidComputer.FindKernel("RemoveDivergence");
		zeroedBoundariesKernel = fluidComputer.FindKernel("EmptyBoundaries");

		// This assumes all kernels of c ompute shader use the same number of threads per group.
		threadsPerGroup = new uint[3];
		fluidComputer.GetKernelThreadGroupSizes(applyExternalsKernel, out threadsPerGroup[0], out threadsPerGroup[1], out threadsPerGroup[2]);
		threadGroups = new int[] { (int)(info.fluidParameters.gridSize / threadsPerGroup[0]), (int)(info.fluidParameters.gridSize / threadsPerGroup[1]), (int)(info.fluidParameters.gridSize / threadsPerGroup[2]) };

		// TODO When 3D is added this can be removed.
		threadGroups[2] = 1;

		fluidComputer.SetVector("threadsPerGroup", new Vector4(threadsPerGroup[0], threadsPerGroup[1], threadsPerGroup[2]));
		fluidComputer.SetVector("threadGroups", new Vector4(threadGroups[0], threadGroups[1], threadGroups[2], 1));

		// TODO Move these to inspector as uneditable fields.
		Debug.Log("Threads per Group: " + threadsPerGroup[0] + " " + threadsPerGroup[1] + " " + threadsPerGroup[2]);
		Debug.Log("Thread Groups: " + threadGroups[0] + " " + threadGroups[1] + " " + threadGroups[2]);

		initialCells = new FluidCell[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
		operationData = new FluidCellOperationData[info.fluidParameters.gridSize, info.fluidParameters.gridSize];

		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				initialCells[i, j] = new FluidCell();
				operationData[i, j] = new FluidCellOperationData();
			}
		}

		int cellStride = Marshal.SizeOf(new FluidCell());
		fluidBuffer1 = new ComputeBuffer(info.fluidParameters.gridSize * info.fluidParameters.gridSize, cellStride);
		fluidBuffer1.SetData(initialCells);
		fluidBuffer2 = new ComputeBuffer(info.fluidParameters.gridSize * info.fluidParameters.gridSize, cellStride);
		fluidBuffer2.SetData(initialCells);

		inFluidBuffer = fluidBuffer1;
		outFluidBuffer = fluidBuffer2;

		externalAdditionBuffer = new ComputeBuffer(info.fluidParameters.gridSize * info.fluidParameters.gridSize, cellStride);
		externalAdditionBuffer.SetData(initialCells);

		int operationDataStride = Marshal.SizeOf(new FluidCellOperationData());
		operationDataBuffer = new ComputeBuffer(info.fluidParameters.gridSize * info.fluidParameters.gridSize, operationDataStride);
		operationDataBuffer.SetData(operationData);

		fluidComputer.SetFloat("cellSize", CellSize);
		fluidComputer.SetFloat("cellsPerSide", info.fluidParameters.gridSize);

		return null;
	}

	protected override void prepareNextFrame()
	{
		externalAdditionBuffer.SetData(initialCells);
		externalAdditionBuffer.GetData(externalAdditions);
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

		// TODO We can calculate which cells should be affected in the compute shader. Though will it save us much?

		for (int i = (int)Mathf.Max(index.x - applyCellRadius, 1); i < Mathf.Min(index.x + applyCellRadius + 1, info.fluidParameters.gridSize - 1); i++)
		{
			for (int j = (int)Mathf.Max(index.y - applyCellRadius, 1); j < Mathf.Min(index.y + applyCellRadius + 1, info.fluidParameters.gridSize - 1); j++)
			{
				// Order cells as (j, i) -> (x, y)
				int x = j;
				int y = i;

				int distance = Mathf.Max(Mathf.Abs(i - index.x), Mathf.Abs(j - index.y));
				if (distance <= densityCellRadius)
				{
					externalAdditions[x, y].density = densityChange - (densityFalloff * distance);
				}
				if (distance <= forceCellRadius)
				{
					externalAdditions[x, y].velocity = force - (forceFalloff * distance);
				}
			}
		}
	}

	protected override void setExternal(FluidCellIndex index, FluidCell applyCell)
	{
		// Compute shader works in transpose order of how these buffers are setup.
		externalAdditions[index.y, index.x].density = applyCell.density;
		externalAdditions[index.y, index.x].velocity = applyCell.velocity;
	}

	protected override FluidCell getExternal(FluidCellIndex index)
	{
		// Compute shader works in transpose order of how these buffers are setup.
		return externalAdditions[index.y, index.x];
	}

	protected override FluidCell getCell(FluidCellIndex index)
	{
		return cells[index.x, index.y].cell;
	}

	protected override FluidCellOperationData getCellOperationData(FluidCellIndex index)
	{
		// Compute shader works in transpose order of how these buffers are setup.
		return operationData[index.y, index.x];
	}

	protected override void applyExternalAdditions()
	{
		externalAdditionBuffer.SetData(externalAdditions);

		fluidComputer.SetFloat("deltaTime", Time.deltaTime);
		fluidComputer.SetBuffer(applyExternalsKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(applyExternalsKernel, "outBuffer", outFluidBuffer);
		fluidComputer.SetBuffer(applyExternalsKernel, "externalsBuffer", externalAdditionBuffer);
		fluidComputer.Dispatch(applyExternalsKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void diffuse()
	{
		float dtDiffusion = (info.operationParameters.diffusionRate * Time.deltaTime * info.fluidParameters.gridSize * info.fluidParameters.gridSize) / info.operationParameters.relaxationIterations;
		//Debug.Log("GPGPU " + dtDiffusion);

		fluidComputer.SetFloat("diffusionRate", dtDiffusion);
		fluidComputer.SetBuffer(diffuseKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(diffuseKernel, "outBuffer", outFluidBuffer);
		fluidComputer.SetBuffer(diffuseKernel, "operationDataBuffer", operationDataBuffer);
		fluidComputer.Dispatch(diffuseKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void advect()
	{
		fluidComputer.SetBuffer(advectKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(advectKernel, "outBuffer", outFluidBuffer);
		fluidComputer.SetBuffer(advectKernel, "operationDataBuffer", operationDataBuffer);
		fluidComputer.Dispatch(advectKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void computeDivergence()
	{
		fluidComputer.SetBuffer(computeDivergenceKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(computeDivergenceKernel, "outBuffer", outFluidBuffer);
		fluidComputer.Dispatch(computeDivergenceKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void relaxDivergence()
	{
		fluidComputer.SetBuffer(relaxDivergenceKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(relaxDivergenceKernel, "outBuffer", outFluidBuffer);
		fluidComputer.Dispatch(relaxDivergenceKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void removeDivergence()
	{
		fluidComputer.SetBuffer(removeDivergenceKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(removeDivergenceKernel, "outBuffer", outFluidBuffer);
		fluidComputer.Dispatch(removeDivergenceKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void clampData()
	{
		fluidComputer.SetFloat("maxDensity", info.cellParameters.cellMaxDensity);
		fluidComputer.SetFloat("maxSpeed", info.cellParameters.cellMaxSpeed);
		fluidComputer.SetBuffer(clampDataKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(clampDataKernel, "outBuffer", outFluidBuffer);
		fluidComputer.Dispatch(clampDataKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	protected override void emptyBoundaries()
	{
		fluidComputer.SetBuffer(zeroedBoundariesKernel, "inBuffer", inFluidBuffer);
		fluidComputer.SetBuffer(zeroedBoundariesKernel, "outBuffer", outFluidBuffer);
		fluidComputer.Dispatch(zeroedBoundariesKernel, threadGroups[0], 1, 1);
	}

	protected override void applyCells()
	{
		// TODO Should we just store this array as a member instead of just creating a new on?
		//		Not sure about the time-memory tradeoff of it.
		FluidCell[,] updatedCells = new FluidCell[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
		inFluidBuffer.GetData(updatedCells);

		operationDataBuffer.GetData(operationData);

		//Profiler.BeginSample("ApplyCells");
		// Apply cell data to renderers as (j, i) -> (x, y)
		// TODO Make this work in 3D
		for (int i = 0; i < info.fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < info.fluidParameters.gridSize; j++)
			{
				int x = j;
				int y = i;
				cells[x, y].cell.density = updatedCells[i, j].density;
				cells[x, y].cell.velocity = updatedCells[i, j].velocity;

				//cells[x, y].diffuseData = diffuseData[i, j];
				//cells[x, y].advectData = advectData[i, j];

				// TODO add global denisty to base fluid pool.
				//globalDensity += updatedCells[i, j].color.a;

				//cellRenderers[x, y].Data = new Vector4(updatedCells[i, j].velocity.x, updatedCells[i, j].velocity.y, updatedCells[i, j].velocity.z, updatedCells[i, j].density);
			}
		}
		//Debug.Log(cellRenderers[0, 0].cell.density);
		//Profiler.EndSample();
	}

	protected override void reset()
	{
		fluidBuffer1.SetData(initialCells);
		fluidBuffer2.SetData(initialCells);
	}
	protected override void pause() { }
	protected override void step() { }

	protected override void swapBuffers()
	{
		if (operationPassNumber % 2 == 0)
		{
			inFluidBuffer = fluidBuffer1;
			outFluidBuffer = fluidBuffer2;
		}
		else
		{
			inFluidBuffer = fluidBuffer2;
			outFluidBuffer = fluidBuffer1;
		}
	}

	void OnDestroy()
	{
		fluidBuffer1.Release();
		fluidBuffer2.Release();
		externalAdditionBuffer.Release();
	}
}
