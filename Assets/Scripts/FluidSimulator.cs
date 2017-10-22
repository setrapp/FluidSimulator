using UnityEngine;
using System;
using UnityEngine.Profiling;

public abstract class FluidSimulator : MonoBehaviour
{
	#region Structure Definitions

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

	
	#endregion

	// TODO Maybe make a build fluid flag to totally rebuild with new sizes.
	[HideInInspector]
	public bool resetFluid = false;
	[HideInInspector]
	public bool pauseFluid = false;
	[HideInInspector]
	public bool stepFluid = false;

	private string familyName = "Fluid Simulator";
	public string FamilyName { get { return familyName; } }
	public bool exclusiveProfile = true;
	public bool autoSimulate = true;

	public FluidParameters fluidParameters;
	public CellParameters cellParameters;
	public OperationParameters operationParameters;
	public OperationFlags operationFlags;
	new protected FluidRenderer renderer;
	FluidOperationPass[] operations;
	protected int operationPassNumber = 0;
	delegate void FluidOperation();

	public float GlobalDensity { get; protected set; }
	private FluidCellIndex selectedCell = new FluidCellIndex();
	public FluidCellIndex SelectedCellIndex
	{
		get { return selectedCell; }
		set
		{
			int maxIndex = fluidParameters.gridSize - 1;
			selectedCell.x = Mathf.Clamp(value.x, 0, maxIndex);
			selectedCell.y = Mathf.Clamp(value.y, 0, maxIndex);
			selectedCell.z = Mathf.Clamp(value.z, 0, maxIndex);

			#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
			#endif 
		}

	}

	protected abstract string initializeBuffers();
	protected abstract void addExternal(FluidCellIndex index, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius);
	protected abstract void setExternal(FluidCellIndex index, FluidCell applyCell);
	protected abstract FluidCell getExternal(FluidCellIndex index);
	protected abstract void getCells(ref FluidCell[] cellsSerialOut);
	protected abstract FluidCellOperationData getCellOperationData(FluidCellIndex index);
	protected abstract void applyExternalAdditions();
	protected abstract void diffuse();
	protected abstract void advect();
	protected abstract void computeDivergence();
	protected abstract void relaxDivergence();
	protected abstract void removeDivergence();
	protected abstract void clampData();
	protected abstract void emptyBoundaries();
	protected abstract void sendCellsToRenderer();
	protected abstract void prepareNextFrame();
	protected abstract void swapBuffers();
	protected abstract void reset();
	protected abstract void pause();
	protected abstract void step();

	public bool Initialize(string familyName, FluidInfo baseInfo, FluidRenderer renderer)
	{
		this.familyName = familyName;
		string result = null;
		BeginSimulatorProfilerSample();
		Profiler.BeginSample("Initialize");

		if (baseInfo == null)
		{
			result = "Attempting to initialize FluidSimulator without valid info";
		}
		fluidParameters = baseInfo.fluidParameters;
		cellParameters = baseInfo.cellParameters;
		operationParameters = baseInfo.operationParameters;
		operationFlags = baseInfo.operationFlags;

		if (string.IsNullOrEmpty(result) && this.renderer != null)
		{
			result = "Attempting to re-initialize a FluidSimulator";
		}

		if (string.IsNullOrEmpty(result))
		{
			cellParameters.cellSize = fluidParameters.physicalSize / fluidParameters.gridSize;
			fluidParameters.container = fluidParameters.container ?? transform;
			// TODO Renderer stuff should probably just be handled by the dispatcher ... if simulator and renderer can be complete ignorant of each other, that would be best. This may not be possible as the Collider depends on both the renderer and the simulator.
			this.renderer = renderer;
			this.renderer.transform.parent = fluidParameters.container;
			this.renderer.gameObject.name = string.Format("{0} Renderer", familyName);
			result = this.renderer.Initialize(this, baseInfo);
		}

		Profiler.BeginSample("InitializeBuffers");
		result = initializeBuffers();
		if (!string.IsNullOrEmpty(result))
		{
			EndSimulatorProfilerSample();
			result = string.Format("Failed to initialize buffers: {0}", result);
		}

		Profiler.EndSample();
		EndSimulatorProfilerSample();

		if (!string.IsNullOrEmpty(result))
		{
			Debug.LogError(result);
			return false;
		}

		return true;
	}

	void Update()
	{
		if (autoSimulate)
		{
			Simulate();
		}
	}

	public void Simulate()
	{
		BeginSimulatorProfilerSample();

		#region Simulation Controls
		CheckControlKeys();

		// When fluid is being reset allow one simulation to propogate data.
		// TODO Maybe this isn't a good idea, as velocity would cause advection (though if every cell is the same does it matter?)
		if (pauseFluid && !resetFluid)
		{
			pause();
			if (stepFluid)
			{
				step();
			}
			else
			{
				return;
			}
		}

		if (resetFluid)
		{
			reset();
			resetFluid = false;
		}
		#endregion

		OrderOperations();
		PerformFluidOperations();
		SendCellsToRenderer();
		PrepareNextFrame();

		EndSimulatorProfilerSample();
	}

	void CheckControlKeys()
	{
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

		if (!controlHeld && Input.GetKey(KeyCode.S))
		{
			if (shiftHeld || Input.GetKeyDown(KeyCode.S))
			{
				stepFluid = true;
			}
		}
	}

	void OrderOperations()
	{
		operations = new FluidOperationPass[]
		{
			!operationFlags.applyExternals		? null : new FluidOperationPass(ApplyExternalAdditions, 1, true),
			!operationFlags.clampData			? null : new FluidOperationPass(ClampData, 1, true),
			!operationFlags.diffuse			? null : new FluidOperationPass(Diffuse, operationParameters.relaxationIterations, true),
			!operationFlags.handleDivergence	? null : new FluidOperationPass(ComputeDivergence, 1, true),
			!operationFlags.handleDivergence	? null : new FluidOperationPass(RelaxDivergence, operationParameters.relaxationIterations, true),
			!operationFlags.handleDivergence	? null : new FluidOperationPass(RemoveDivergence, 1, true),
			!operationFlags.advect				? null : new FluidOperationPass(Advect, 1, true),
			!operationFlags.handleDivergence	? null : new FluidOperationPass(ComputeDivergence, 1, true),
			!operationFlags.handleDivergence	? null : new FluidOperationPass(RelaxDivergence, operationParameters.relaxationIterations, true),
			!operationFlags.handleDivergence	? null : new FluidOperationPass(RemoveDivergence, 1, true),
			!operationFlags.clampData			? null : new FluidOperationPass(ClampData, 1, true)
		};
	}

	void PerformFluidOperations()
	{
		Profiler.BeginSample("PerformFluidOperations");
		for (int i = 0; i < operations.Length; i++)
		{
			if (operations[i] != null)
			{
				for (int j = 0; j < operations[i].relaxationIterations; j++)
				{
					operations[i].operation();
					if (operations[i].swapAfter)
					{
						SwapBuffers();
					}
				}
			}
		}
		Profiler.EndSample();
	}

	public void AddExternal(FluidCellIndex index, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius)
	{
		if (densityChange == 0 && force.sqrMagnitude == 0)
		{
			return;
		}

		Profiler.BeginSample("AddExternal");
		addExternal(index, densityChange, densityChangeRadius, force, forceRadius);
		Profiler.EndSample();
	}

	public void SetExternal(FluidCellIndex index , FluidCell applyCell)
	{
		setExternal(index, applyCell);
	}

	public FluidCell GetExternal(FluidCellIndex index)
	{
		return getExternal(index);
	}

	public void GetCells(ref FluidCell[] cellsSerialOut)
	{
		if (cellsSerialOut == null || cellsSerialOut.Length != (fluidParameters.gridSize * fluidParameters.gridSize))
		{
			Debug.LogError("Invalid cell result array. Ensure an array of length (gridSize * gridSize) is passed.");
		}
		getCells(ref cellsSerialOut);
	}

	public FluidCellOperationData GetCellOperationData(FluidCellIndex index)
	{
		return getCellOperationData(index);
	}

	void ApplyExternalAdditions()
	{
		Profiler.BeginSample("ApplyExternalAdditions");
		applyExternalAdditions();
		Profiler.EndSample();
		SetBoundaries();
	}

	void Diffuse()
	{
		Profiler.BeginSample("Diffuse");
		diffuse();
		Profiler.EndSample();
		SetBoundaries();
	}

	void Advect()
	{
		Profiler.BeginSample("Advect");
		advect();
		Profiler.EndSample();
		SetBoundaries();
	}

	void ComputeDivergence()
	{
		Profiler.BeginSample("ComputeDivergence");
		computeDivergence();
		Profiler.EndSample();
		SetBoundaries();
	}

	void RelaxDivergence()
	{
		Profiler.BeginSample("RelaxDivergence");
		relaxDivergence();
		Profiler.EndSample();
		SetBoundaries();
	}

	void RemoveDivergence()
	{
		Profiler.BeginSample("RemoveDivergence");
		removeDivergence();
		Profiler.EndSample();
		SetBoundaries();
	}

	void ClampData()
	{
		Profiler.BeginSample("ClampData");
		clampData();
		Profiler.EndSample();
	}

	void SetBoundaries()
	{
		Profiler.BeginSample("SetBoundaries");
		switch (operationParameters.boundaryCondition)
		{
			case BoundaryCondition.Empty:
				emptyBoundaries();
				break;
		}
		Profiler.EndSample();
	}

	void SendCellsToRenderer()
	{
		if (renderer == null)
		{
			return;
		}

		Profiler.BeginSample("SendCellsToRender");
		sendCellsToRenderer();
		Profiler.EndSample();
	}

	void PrepareNextFrame()
	{
		Profiler.BeginSample("PrepareNextFrame");
		operationPassNumber = operationPassNumber % 2;
		stepFluid = false;
		prepareNextFrame();
		Profiler.EndSample();
	}

	void SwapBuffers()
	{
		swapBuffers();
		operationPassNumber++;
	}

	void BeginSimulatorProfilerSample()
	{
		if (exclusiveProfile)
		{
			Profiler.BeginSample(gameObject.name);
		}
		else
		{
			Profiler.BeginSample("FluidSimulator");
		}
	}

	void EndSimulatorProfilerSample()
	{
		Profiler.EndSample();
	}
}
