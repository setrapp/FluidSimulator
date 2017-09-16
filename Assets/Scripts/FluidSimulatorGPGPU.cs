using System.Runtime.InteropServices;
using UnityEngine;

public class FluidSimulatorGPGPU : FluidSimulator
{
	[Header("Cell Rendering")]
	[SerializeField]
	public FluidCellRenderer cellPrefab;
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

	public FluidCellOperationData[,] operationData;
	public ComputeBuffer operationDataBuffer;

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
		threadGroups = new int[] { (int)(fluidParameters.gridSize / threadsPerGroup[0]), (int)(fluidParameters.gridSize / threadsPerGroup[1]), (int)(fluidParameters.gridSize / threadsPerGroup[2]) };

		// TODO When 3D is added this can be removed.
		threadGroups[2] = 1;

		fluidComputer.SetVector("threadsPerGroup", new Vector4(threadsPerGroup[0], threadsPerGroup[1], threadsPerGroup[2]));
		fluidComputer.SetVector("threadGroups", new Vector4(threadGroups[0], threadGroups[1], threadGroups[2], 1));

		// TODO Move these to inspector as uneditable fields.
		Debug.Log("Threads per Group: " + threadsPerGroup[0] + " " + threadsPerGroup[1] + " " + threadsPerGroup[2]);
		Debug.Log("Thread Groups: " + threadGroups[0] + " " + threadGroups[1] + " " + threadGroups[2]);

		initialCells = new FluidCell[fluidParameters.gridSize, fluidParameters.gridSize];
		externalAdditions = new FluidCell[fluidParameters.gridSize, fluidParameters.gridSize];
		operationData = new FluidCellOperationData[fluidParameters.gridSize, fluidParameters.gridSize];

		for (int i = 0; i < fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < fluidParameters.gridSize; j++)
			{
				initialCells[i, j] = new FluidCell();
				externalAdditions[i, j] = new FluidCell();
				operationData[i, j] = new FluidCellOperationData();
			}
		}

		int cellStride = Marshal.SizeOf(new FluidCell());
		fluidBuffer1 = new ComputeBuffer(fluidParameters.gridSize * fluidParameters.gridSize, cellStride);
		fluidBuffer1.SetData(initialCells);
		fluidBuffer2 = new ComputeBuffer(fluidParameters.gridSize * fluidParameters.gridSize, cellStride);
		fluidBuffer2.SetData(initialCells);

		inFluidBuffer = fluidBuffer1;
		outFluidBuffer = fluidBuffer2;

		externalAdditionBuffer = new ComputeBuffer(fluidParameters.gridSize * fluidParameters.gridSize, cellStride);
		externalAdditionBuffer.SetData(initialCells);

		int operationDataStride = Marshal.SizeOf(new FluidCellOperationData());
		operationDataBuffer = new ComputeBuffer(fluidParameters.gridSize * fluidParameters.gridSize, operationDataStride);
		operationDataBuffer.SetData(operationData);

		fluidComputer.SetFloat("cellSize", cellParameters.cellSize);
		fluidComputer.SetFloat("cellsPerSide", fluidParameters.gridSize);

		return null;
	}

	protected override void prepareNextFrame()
	{
		externalAdditionBuffer.SetData(initialCells);
		for (int i = 0; i < fluidParameters.gridSize; i++)
		{
			for (int j = 0; j < fluidParameters.gridSize; j++)
			{
				externalAdditions[i, j] = initialCells[i, j];
			}
		}
	}

	protected override void addExternal(FluidCellIndex index, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius)
	{
		if (densityChange == 0 && force.sqrMagnitude == 0)
		{
			return;
		}

		int densityCellRadius = (int)Mathf.Max(densityChangeRadius / cellParameters.cellSize, 0);
		int forceCellRadius = (int)Mathf.Max(forceRadius / cellParameters.cellSize, 0);
		int applyCellRadius = Mathf.Max(densityCellRadius, forceCellRadius);

		float densityFalloff = densityChange / (densityCellRadius + 1);
		Vector3 forceFalloff = force / (forceCellRadius + 1);

		// TODO We can calculate which cells should be affected in the compute shader. Though will it save us much?

		for (int i = (int)Mathf.Max(index.x - applyCellRadius, 1); i < Mathf.Min(index.x + applyCellRadius + 1, fluidParameters.gridSize - 1); i++)
		{
			for (int j = (int)Mathf.Max(index.y - applyCellRadius, 1); j < Mathf.Min(index.y + applyCellRadius + 1, fluidParameters.gridSize - 1); j++)
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
		return inCells[index.x, index.y];
	}

	protected override FluidCellOperationData getCellOperationData(FluidCellIndex index)
	{
		return operationData[index.x, index.y];
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
		float dtDiffusion = (operationParameters.diffusionRate * Time.deltaTime * fluidParameters.gridSize * fluidParameters.gridSize) / operationParameters.relaxationIterations;

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
		fluidComputer.SetFloat("maxDensity", cellParameters.cellMaxDensity);
		fluidComputer.SetFloat("maxSpeed", cellParameters.cellMaxSpeed);
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

	protected override void sendCellsToRenderer()
	{
		renderer.RenderCells(inFluidBuffer);
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
