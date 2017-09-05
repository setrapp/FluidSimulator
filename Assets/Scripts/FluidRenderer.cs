using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

public abstract class FluidRenderer : MonoBehaviour
{
	public FluidSimulator Simulator { get; private set; }

	protected abstract string generateCells();
	FluidCell[,] temporaryCellArray;
	ComputeBuffer temporaryCellBuffer;

	static string noSimulator = "FluidRenderer is not attached to a FluidSimulator";
	static string noRenderingDefinition = "FluidRenderer defines no way to render cells.";

	public string Initialize(FluidSimulator simulator)
	{
		string result = null;
		if (Simulator != null)
		{
			result = "Attempting to re-initialize FluidRenderer";
		}

		if (string.IsNullOrEmpty(result))
		{
			this.Simulator = simulator;
			Profiler.BeginSample("FluidRenderer.GenerateCells");
			result = generateCells();
			Profiler.EndSample();
		}

		Profiler.EndSample();
		return result;
	}

	public void RenderCells(FluidCell[,] cells)
	{
		if (Simulator == null)
		{
			Debug.LogError(noSimulator, this);
			return;
		}

		Profiler.BeginSample("FluidRenderer.RenderCells");
		if (this is IRenderArray)
		{
			((IRenderArray)this).RenderCells(cells);
		}
		else if(this is IRenderBuffer)
		{
			TranslateCellsToBuffer(cells);
			((IRenderBuffer)this).RenderCells(temporaryCellBuffer);
		}
		else
		{
			Debug.LogError(noRenderingDefinition, this);
		}
		Profiler.EndSample();
	}

	public void RenderCells(ComputeBuffer cells)
	{
		if (Simulator == null)
		{
			Debug.LogError(noSimulator, this);
			return;
		}

		Profiler.BeginSample("FluidRenderer.RenderCells");
		if (this is IRenderArray)
		{
			TranslateCellsToArray(cells);
			((IRenderArray)this).RenderCells(temporaryCellArray);
		}
		else if (this is IRenderBuffer)
		{
			((IRenderBuffer)this).RenderCells(cells);
		}
		else
		{
			Debug.LogError(noRenderingDefinition, this);
		}
		Profiler.EndSample();
	}

	void TranslateCellsToArray(ComputeBuffer cellBuffer)
	{
		if (temporaryCellArray == null)
		{
			int gridSize = Simulator.info.fluidParameters.gridSize;
			temporaryCellArray = new FluidCell[gridSize, gridSize];
		}
		cellBuffer.GetData(temporaryCellArray);
	}

	void TranslateCellsToBuffer(FluidCell[,] cellArray)
	{
		if (temporaryCellBuffer == null)
		{
			int gridSize = Simulator.info.fluidParameters.gridSize;
			temporaryCellBuffer = new ComputeBuffer(gridSize * gridSize, Marshal.SizeOf(new FluidCell()));
		}
		temporaryCellBuffer.SetData(cellArray);
	}
}
