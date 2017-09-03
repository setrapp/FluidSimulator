using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

/*[System.Serializable]
public struct ComputeFluidCell
{
	public Vector4 velocity;
	// TODO change this to Density and essence
	public Color color;
}*/
 /*
[System.Serializable]
public struct DiffuseData
{
	public Vector3 leftId;
	public Vector3 rightId;
	public Vector3 downId;
	public Vector3 upId;
};

[System.Serializable]
public struct AdvectData
{
	public Vector3 idVelocity;
	public Vector3 pastId;
	public Vector2 halfPortions;
};	 */

/*public class FluidComputer : MonoBehaviour {
	public bool resetFluid = false;
	public bool pauseFluid = false;
	public bool stepFluid = false;

	[Header("Pool Definitions")]
	public int poolGridSize = 100;
	public float poolAbsoluteSize = 100;
	public float startingDensity = 0;
	//[HideInInspector]
	public float cellSize = 5;
	public float cellMaxDensity = 1;
	public float cellMaxSpeed = 3;
	public float cellMass = 1;
	public ComputeFluidCellRenderer cellRendererPrefab;
	public ComputeFluidCellRenderer[,] cellRenderers;
	//private ComputeFluidCell[,] cells;
	private ComputeBuffer externalAdditionBuffer;
	public Vector3[,] reverseVelocities;
	[Header("Visualization")]
	public bool showSolid = true;
	public bool showOutline = false;
	public bool showCenter = false;
	public bool showDensity = true;
	public bool showVelocity = true;
	[Header("Operations")]
	public bool applyExternals = true;
	public bool diffuse = true;
	public bool advect = true;
	public bool conserveMass = true;
	private FluidOperationPass[] operations;
	[Header("Additional Definitions")]
	public BoundaryCondition boundaryCondition = BoundaryCondition.CONTAIN;
	public float densityAddRate = 1;
	public float diffusionRate = 0.05f;
	public int relaxationIterations = 20;

	// TODO Figure out where to put these.
	private int operationPassNumber = 0;
	private ComputeBuffer fluidBuffer1;
	private ComputeBuffer fluidBuffer2;
	private ComputeBuffer inFluidBuffer;
	private ComputeBuffer outFluidBuffer;
	//int copyBufferKernel;
	//int readTextureKernel;
	//int writeTextureKernel;
	int clampDataKernel;
	int applyExternalsKernel;
	int diffuseKernel;
	int advectKernel;
	int conserveMass1Kernel;
	int conserveMass2Kernel;
	int conserveMass3Kernel;
	int containBoundariesKernel;
	int wrapBoundariesKernel;
	public ComputeShader compute;
	uint[] threadsPerGroup;
	int[] threadGroups;
	ComputeBuffer threadGroupsBuffer;
	ComputeFluidCell[,] initialCells;

	public DiffuseData[,] initialDiffuseData;
	ComputeBuffer diffuseDataBuffer;
	public AdvectData[,] initialAdvectData;
	ComputeBuffer advectDataBuffer;

	public float globalDensity;

	private delegate void FluidOperation(ComputeBuffer inBuffer, ComputeBuffer outBuffer);

	public enum BoundaryCondition
	{
		CONTAIN = 0,
		//WRAP TODO Make work
		//LEAK TODO Implement
	}

	class FluidOperationPass
	{
		public FluidOperation operation;
		public int relaxationIterations;
		public bool swapAfter;

		public FluidOperationPass(FluidOperation operation, int relaxationIterations, bool swapAfter)
		{
			this.operation = operation;
			this.relaxationIterations = relaxationIterations;
			this.swapAfter = swapAfter;
		}
	}

	void Awake()
	{
		if (cellRendererPrefab != null)
		{
			cellSize = poolAbsoluteSize / poolGridSize;
			float halfPoolSize = poolGridSize / 2;
			cellRenderers = new ComputeFluidCellRenderer[poolGridSize, poolGridSize];
			reverseVelocities = new Vector3[poolGridSize, poolGridSize];
			for (int i = 0; i < poolGridSize; i++)
			{
				for (int j = 0; j < poolGridSize; j++)
				{
					Vector3 pos = transform.position + new Vector3(cellSize * (i - halfPoolSize), cellSize * (j - halfPoolSize), 0);
					ComputeFluidCellRenderer newCell = (((GameObject)Instantiate(cellRendererPrefab.gameObject, pos, Quaternion.identity)).GetComponent<ComputeFluidCellRenderer>());
					newCell.pool = this;
					newCell.poolI = i;
					newCell.poolJ = j;
					newCell.transform.parent = transform;
					newCell.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
					newCell.cell.color.a = startingDensity;
					cellRenderers[i, j] = newCell;

					reverseVelocities[i, j] = Vector3.zero;
				}
			}
		}
		else
		{
			throw new System.NullReferenceException(string.Format("No Cell Prefab Provided to {0}'s pool.", gameObject.name));
		}
	}

	void Start()
	{
		// TODO Is there any reason to not merge Awake and Start here?
		//fluidData1 = new ComputeFluidCell[poolGridSize, poolGridSize];
		//fluidData2 = new ComputeFluidCell[poolGridSize, poolGridSize];
		//inFluidData = fluidData1;
		//outFluidData = fluidData2;

		//copyBufferKernel = compute.FindKernel("CopyBuffer");
		//readTextureKernel = compute.FindKernel("ReadTexture");
		//writeTextureKernel = compute.FindKernel("WriteTexture");
		clampDataKernel = compute.FindKernel("ClampData");
		applyExternalsKernel = compute.FindKernel("ApplyExternals");
		diffuseKernel = compute.FindKernel("Diffuse");
		advectKernel = compute.FindKernel("Advect");
		conserveMass1Kernel = compute.FindKernel("ConserveMass1");
		conserveMass2Kernel = compute.FindKernel("ConserveMass2");
		conserveMass3Kernel = compute.FindKernel("ConserveMass3");
		containBoundariesKernel = compute.FindKernel("ContainBoundaries");
		//wrapBoundariesKernel = compute.FindKernel("WrapBoundaries");

		// TODO I'm not sure why dividing by numthreads breaks this ... I would expect that to be correct.
		//threadGroups = poolGridSize / 4; // TODO Is there a way to dynamically get/set numthreads in shader?
		//int threadCountKernel = compute.FindKernel("GetThreadCounts");
		/threadsPerGroup = new int[3];
		//ComputeBuffer threadCountBuffer = new ComputeBuffer(3, 32);
		//compute.SetBuffer(threadCountKernel, "threadCounts", threadCountBuffer);
		//compute.Dispatch(threadCountKernel, 1, 1, 1);
		//threadCountBuffer.GetData(threadsPerGroup);

		// This assumes all kernels of compute shader use the same number of threads per group.
		threadsPerGroup = new uint[3];
		compute.GetKernelThreadGroupSizes(applyExternalsKernel, out threadsPerGroup[0], out threadsPerGroup[1], out threadsPerGroup[2]);
		threadGroups = new int[] { (int)(poolGridSize / threadsPerGroup[0]), (int)(poolGridSize / threadsPerGroup[1]), (int)(poolGridSize / threadsPerGroup[2]) };

		// TODO When 3D is added this can be removed.
		threadGroups[2] = 1;

		//threadGroupsBuffer = new ComputeBuffer(3, 4);
		//threadGroupsBuffer.SetData(threadGroups);
		compute.SetVector("threadsPerGroup", new Vector4(threadsPerGroup[0], threadsPerGroup[1], threadsPerGroup[2]));
		compute.SetVector("threadGroups", new Vector4(threadGroups[0], threadGroups[1], threadGroups[2], 1));

		Debug.Log("Threads per Group: " + threadsPerGroup[0] + " " + threadsPerGroup[1] + " " + threadsPerGroup[2]);
		Debug.Log("Thread Groups: " + threadGroups[0] + " " + threadGroups[1] + " " + threadGroups[2]);

		initialCells = new ComputeFluidCell[poolGridSize, poolGridSize];
		initialDiffuseData = new DiffuseData[poolGridSize, poolGridSize];
		initialAdvectData = new AdvectData[poolGridSize, poolGridSize];

		for (int i = 0; i < poolGridSize; i++)
		{
			for (int j = 0; j < poolGridSize; j++)
			{
				initialCells[i, j] = new ComputeFluidCell();
				initialDiffuseData[i, j] = new DiffuseData();
				initialAdvectData[i, j] = new AdvectData();
			}
		}

		int cellStride = Marshal.SizeOf(new ComputeFluidCell());
		fluidBuffer1 = new ComputeBuffer(poolGridSize * poolGridSize, cellStride);
		fluidBuffer1.SetData(initialCells);
		fluidBuffer2 = new ComputeBuffer(poolGridSize * poolGridSize, cellStride);
		fluidBuffer2.SetData(initialCells);

		externalAdditionBuffer = new ComputeBuffer(poolGridSize * poolGridSize, cellStride);
		externalAdditionBuffer.SetData(initialCells);

		int diffuseDataStride = Marshal.SizeOf(new DiffuseData());
		diffuseDataBuffer = new ComputeBuffer(poolGridSize * poolGridSize, diffuseDataStride);
		diffuseDataBuffer.SetData(initialDiffuseData);

		int advectDataStride = Marshal.SizeOf(new AdvectData());
		advectDataBuffer = new ComputeBuffer(poolGridSize * poolGridSize, advectDataStride);
		advectDataBuffer.SetData(initialAdvectData);

		compute.SetFloat("cellSize", cellSize);
		compute.SetFloat("cellsPerSide", 1);// poolGridSize);

		SwapBuffers();

		operationPassNumber = 0;
	}

	// TODO Anytime we set a value more than once, we should use the int value.
	void Update()
	{
		//Profiler.BeginSample("UpdateParticles");
		bool controlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
		bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);


		if (!controlHeld && Input.GetKeyDown(KeyCode.R))
		{
			resetFluid = true;
		}

		if (!controlHeld && Input.GetKeyDown(KeyCode.P))
		{
			pauseFluid = !pauseFluid;
		}

		stepFluid = false;
		if (!controlHeld && Input.GetKey(KeyCode.S))
		{
			if (shiftHeld || Input.GetKeyDown(KeyCode.S))
			{
				stepFluid = true;
			}
		}

		if (resetFluid)
		{
			resetFluid = false;
			fluidBuffer1.SetData(initialCells);
			fluidBuffer2.SetData(initialCells);
			diffuseDataBuffer.SetData(initialDiffuseData);
			advectDataBuffer.SetData(initialAdvectData);
			PrepareNextFrame();
		}


		// IMPORTANT: Fluid operations beyond this point will cease when fluid is paused.
		if (pauseFluid && !stepFluid)
		{
			return;
		}

		OrderOperations();

		for (int i = 0; i < operations.Length; i++)
		{
			if (operations[i] != null)
			{
				for (int j = 0; j < operations[i].relaxationIterations; j++)
				{
					operations[i].operation(inFluidBuffer, outFluidBuffer);
					//if (operations[i].swapAfter)
					{
						SwapBuffers();
					}
				}
			}
		}

		PrepareNextFrame();
		//Profiler.EndSample();
	}

	void PrepareNextFrame()
	{
		ApplyCells(inFluidBuffer);
		operationPassNumber = operationPassNumber % 2;
		externalAdditionBuffer.SetData(initialCells);
	}

	void OrderOperations()
	{
		// TODO We may not need to clamp after every operation (maybe just apply externals and at the end).
		//	Alternatively, we may roll clamp and set boundaries in together if they both need to happen between operations.
		operations = new FluidOperationPass[]
		{
			!applyExternals	? null : new FluidOperationPass(ApplyExternalAdditions, 1, true),
			new FluidOperationPass(ClampData, 1, true),
			!diffuse		? null : new FluidOperationPass(Diffuse, relaxationIterations, !conserveMass),
			new FluidOperationPass(ClampData, 1, true),
			!conserveMass	? null : new FluidOperationPass(ConserveMass1, 1, true),
			new FluidOperationPass(ClampData, 1, true),
			!conserveMass	? null : new FluidOperationPass(ConserveMass2, relaxationIterations, true),
			new FluidOperationPass(ClampData, 1, true),
			!conserveMass	? null : new FluidOperationPass(ConserveMass3, 1, true),
			new FluidOperationPass(ClampData, 1, true),
			!advect			? null : new FluidOperationPass(Advect, 1, !conserveMass),
			new FluidOperationPass(ClampData, 1, true),
			!conserveMass || !advect	? null : new FluidOperationPass(ConserveMass1, 1, true),
			new FluidOperationPass(ClampData, 1, true),
			!conserveMass || !advect    ? null : new FluidOperationPass(ConserveMass2, relaxationIterations, true),
			new FluidOperationPass(ClampData, 1, true),
			!conserveMass || !advect    ? null : new FluidOperationPass(ConserveMass3, 1, true),
			new FluidOperationPass(ClampData, 1, true),
			//new FluidOperationPass(SetBoundaries, 1, true),
			new FluidOperationPass(ClampData, 1, true)
		};
	}

	void ApplyExternalAdditions(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
		compute.SetBuffer(applyExternalsKernel, "inBuffer", inFluidData);
		compute.SetBuffer(applyExternalsKernel, "outBuffer", outFluidData);
		compute.SetBuffer(applyExternalsKernel, "externalsBuffer", externalAdditionBuffer);
		compute.Dispatch(applyExternalsKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void Diffuse(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		compute.SetFloat("diffusionRate", diffusionRate * Time.deltaTime);
		compute.SetBuffer(diffuseKernel, "inBuffer", inFluidData);
		compute.SetBuffer(diffuseKernel, "outBuffer", outFluidData);
		compute.SetBuffer(diffuseKernel, "diffuseDataBuffer", diffuseDataBuffer);
		compute.Dispatch(diffuseKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void Advect(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		compute.SetBuffer(advectKernel, "inBuffer", inFluidData);
		compute.SetBuffer(advectKernel, "outBuffer", outFluidData);
		compute.SetBuffer(advectKernel, "advectDataBuffer", advectDataBuffer);
		compute.Dispatch(advectKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void ConserveMass1(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		compute.SetBuffer(conserveMass1Kernel, "inBuffer", inFluidData);
		compute.SetBuffer(conserveMass1Kernel, "outBuffer", outFluidData);
		compute.Dispatch(conserveMass1Kernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void ConserveMass2(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		compute.SetBuffer(conserveMass2Kernel, "inBuffer", inFluidData);
		compute.SetBuffer(conserveMass2Kernel, "outBuffer", outFluidData);
		compute.Dispatch(conserveMass2Kernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void ConserveMass3(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		compute.SetBuffer(conserveMass3Kernel, "inBuffer", inFluidData);
		compute.SetBuffer(conserveMass3Kernel, "outBuffer", outFluidData);
		compute.Dispatch(conserveMass3Kernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void ClampData(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		compute.SetFloat("maxDensity", cellMaxDensity);
		compute.SetFloat("maxSpeed", cellMaxSpeed);
		compute.SetBuffer(clampDataKernel, "inBuffer", inFluidData);
		compute.SetBuffer(clampDataKernel, "outBuffer", outFluidData);
		compute.Dispatch(clampDataKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
	}

	void SetBoundaries(ComputeBuffer inFluidData, ComputeBuffer outFluidData)
	{
		//Profiler.BeginSample("SetBoundaries");
		switch(boundaryCondition)
		{
			case BoundaryCondition.CONTAIN:
				compute.SetBuffer(containBoundariesKernel, "inBuffer", inFluidData);
				compute.SetBuffer(containBoundariesKernel, "outBuffer", outFluidData);
				compute.Dispatch(containBoundariesKernel, threadGroups[0], threadGroups[1], threadGroups[2]);
				break;
			//case BoundaryCondition.WRAP:
			//	WrapBoundaries(updatedCells);
			//	break;
			//case BoundaryCondition.LEAK:
			//	LeakBoundaries(updatedCells);
			//	break;
		}
		//Profiler.EndSample();
	}

	void ApplyCells(ComputeBuffer updatedBuffer)
	{
		globalDensity = 0;

		// TODO Should we just store this array as a member instead of just creating a new on?
		//		Not sure about the time-memory tradeoff of it.
		ComputeFluidCell[,] updatedCells = new ComputeFluidCell[poolGridSize, poolGridSize];
		updatedBuffer.GetData(updatedCells);

		DiffuseData[,] diffuseData = new DiffuseData[poolGridSize, poolGridSize];
		diffuseDataBuffer.GetData(diffuseData);

		AdvectData[,] advectData = new AdvectData[poolGridSize, poolGridSize];
		advectDataBuffer.GetData(advectData);

		//Profiler.BeginSample("ApplyCells");
		// Apply cell data to renderers as (j, i) -> (x, y)
		// TODO Make this work in 3D
		for (int i = 0; i < poolGridSize; i++)
		{
			for (int j = 0; j < poolGridSize; j++)
			{
				int x = j;
				int y = i;
				cellRenderers[x, y].cell.color.a = updatedCells[i, j].color.a;
				cellRenderers[x, y].cell.velocity = updatedCells[i, j].velocity;

				cellRenderers[x, y].diffuseData = diffuseData[i, j];
				cellRenderers[x, y].advectData = advectData[i, j];

				globalDensity += updatedCells[i, j].color.a;

				//cellRenderers[x, y].Data = new Vector4(updatedCells[i, j].velocity.x, updatedCells[i, j].velocity.y, updatedCells[i, j].velocity.z, updatedCells[i, j].density);
			}
		}
		//Debug.Log(cellRenderers[0, 0].cell.density);
		//Profiler.EndSample();
	}


	public void AddExternal(ComputeFluidCellRenderer cell, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius)
	{
		if (densityChange == 0 && force.sqrMagnitude == 0)
		{
			return;
		}

		//Profiler.BeginSample("AddExternal");

		int densityCellRadius = (int)Mathf.Max(densityChangeRadius / cellSize, 0);
		int forceCellRadius = (int)Mathf.Max(forceRadius / cellSize, 0);
		int applyCellRadius = Mathf.Max(densityCellRadius, forceCellRadius);

		int densityChangeDirection = densityChange >= 0 ? 1 : -1;

		float densityFalloff = densityChange / (densityCellRadius + 1);
		Vector3 forceFalloff = force / (forceCellRadius + 1);

		int iCenter = cell.poolI;
		int jCenter = cell.poolJ;

		// TODO We can calculate which cells should be affected in the compute shader. Though will it save us much?

		// TODO This should probably be stored as an member and just cleared.
		ComputeFluidCell[,] externalAdditionData = new ComputeFluidCell[poolGridSize, poolGridSize];



		for (int i = (int)Mathf.Max(iCenter - applyCellRadius, 1); i < Mathf.Min(iCenter + applyCellRadius + 1, poolGridSize - 1); i++)
		{
			for (int j = (int)Mathf.Max(jCenter - applyCellRadius, 1); j < Mathf.Min(jCenter + applyCellRadius + 1, poolGridSize - 1); j++)
			{
				// Order cells as (j, i) -> (x, y)
				int x = j;
				int y = i;

				// Is this necessary? Creating the array should set default values.
				//externalAdditionData[x, y].color.a = 0;
				//externalAdditionData[x, y].velocity = Vector3.zero;

				int distance = Mathf.Max(Mathf.Abs(i - iCenter), Mathf.Abs(j - jCenter));
				if (distance <= densityCellRadius)
				{
					externalAdditionData[x, y].color.a = densityChange - (densityFalloff * distance);
				}
				if (distance <= forceCellRadius)
				{
					externalAdditionData[x, y].velocity = force - (forceFalloff * distance);
				}
			}
		}
		externalAdditionBuffer.SetData(externalAdditionData);
		//Profiler.EndSample();
	}

	void SwapBuffers()
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
		operationPassNumber++;
	}

	void OnDestroy()
	{
		fluidBuffer1.Release();
		fluidBuffer2.Release();
		externalAdditionBuffer.Release();
	}
}*/
