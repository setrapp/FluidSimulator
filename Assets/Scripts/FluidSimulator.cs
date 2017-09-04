using UnityEngine;
using System;
using UnityEngine.Profiling;

public abstract class FluidSimulator : MonoBehaviour
{
	#region Structure Definitions
	[Serializable]
	public class FluidParameters
	{
		public Transform container;
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
	}

	[Serializable]
	public class OperationParameters
	{
		public BoundaryCondition boundaryCondition = BoundaryCondition.EMPTY;
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

	[Serializable]
	public class FluidSimulatorInfo
	{
		public FluidParameters fluidParameters;
		public CellParameters cellParameters;
		public OperationParameters operationParameters;
		public OperationFlags operationFlags;
		public VisualizationFlags visualizationFlags;
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

	public enum BoundaryCondition
	{
		EMPTY = 0
		//CONTAIN Implement
		//WRAP TODO Implement
		//LEAK TODO Implement
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

	public FluidSimulatorInfo info;
	new protected FluidRenderer renderer;
	FluidOperationPass[] operations;
	protected int operationPassNumber = 0;
	public float CellSize { get; private set; }
	delegate void FluidOperation();

	public float GlobalDensity { get; protected set; }
	private FluidCellIndex selectedCell = new FluidCellIndex();
	public FluidCellIndex SelectedCellIndex
	{
		get { return selectedCell; }
		set
		{
			int maxIndex = info.fluidParameters.gridSize - 1;
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
	protected abstract FluidCell getCell(FluidCellIndex index);
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

	public bool Initialize(string familyName, FluidRenderer renderer)
	{
		this.familyName = familyName;
		string result = null;
		BeginSimulatorProfilerSample();
		Profiler.BeginSample("Initialize");

		if (this.renderer != null)
		{
			result = "Attempting to re-initialize a FluidSimulator";
		}

		if (string.IsNullOrEmpty(result))
		{
			CellSize = info.fluidParameters.physicalSize / info.fluidParameters.gridSize;
			info.fluidParameters.container = info.fluidParameters.container ?? transform;
			this.renderer = renderer;
			this.renderer.transform.parent = info.fluidParameters.container;
			this.renderer.gameObject.name = string.Format("{0} Renderer", familyName);
			result = this.renderer.Initialize(this);
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
			!info.operationFlags.applyExternals		? null : new FluidOperationPass(ApplyExternalAdditions, 1, true),
			!info.operationFlags.clampData			? null : new FluidOperationPass(ClampData, 1, true),
			!info.operationFlags.diffuse			? null : new FluidOperationPass(Diffuse, info.operationParameters.relaxationIterations, true),
			!info.operationFlags.handleDivergence	? null : new FluidOperationPass(ComputeDivergence, 1, true),
			!info.operationFlags.handleDivergence	? null : new FluidOperationPass(RelaxDivergence, info.operationParameters.relaxationIterations, true),
			!info.operationFlags.handleDivergence	? null : new FluidOperationPass(RemoveDivergence, 1, true),
			!info.operationFlags.advect				? null : new FluidOperationPass(Advect, 1, true),
			!info.operationFlags.handleDivergence	? null : new FluidOperationPass(ComputeDivergence, 1, true),
			!info.operationFlags.handleDivergence	? null : new FluidOperationPass(RelaxDivergence, info.operationParameters.relaxationIterations, true),
			!info.operationFlags.handleDivergence	? null : new FluidOperationPass(RemoveDivergence, 1, true),
			!info.operationFlags.clampData			? null : new FluidOperationPass(ClampData, 1, true)
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

	public FluidCell GetCell(FluidCellIndex index)
	{
		return getCell(index);
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
		switch (info.operationParameters.boundaryCondition)
		{
			case BoundaryCondition.EMPTY:
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
