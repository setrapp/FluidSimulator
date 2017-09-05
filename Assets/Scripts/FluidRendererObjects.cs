using UnityEngine;

public class FluidRendererObjects : FluidRenderer, IRenderArray
{
	[SerializeField]
	public FluidCellRenderer cellPrefab;
	FluidCellRenderer[,] cells;

	protected override string generateCells()
	{
		string result = null;

		if (cellPrefab != null)
		{
			FluidSimulator.FluidSimulatorInfo info = Simulator.info;
			float halfGridSize = info.fluidParameters.gridSize / 2;
			cells = new FluidCellRenderer[info.fluidParameters.gridSize, info.fluidParameters.gridSize];
			for (int i = 0; i < info.fluidParameters.gridSize; i++)
			{
				for (int j = 0; j < info.fluidParameters.gridSize; j++)
				{
					float cellSize = Simulator.CellSize;
					Vector3 pos = transform.position + new Vector3(cellSize * (i - halfGridSize), cellSize * (j - halfGridSize), 0);
					FluidCellRenderer newCell = (((GameObject)Instantiate(cellPrefab.gameObject, pos, Quaternion.identity, transform)).GetComponent<FluidCellRenderer>());
					newCell.Initialize(Simulator, info.cellParameters.defaultCell, i, j);
					newCell.transform.localScale = new Vector3(cellSize, cellSize, cellSize);
					cells[i, j] = newCell;
				}
			}
		}
		else
		{
			result = string.Format("No Cell Prefab provided to {0}'s renderer.", gameObject.name);
		}

		return result;
	}

	void IRenderArray.RenderCells(FluidCell[,] fluidCells)
	{
		int gridSize = Simulator.info.fluidParameters.gridSize;
		for (int i = 0; i < gridSize; i++)
		{
			for (int j = 0; j < gridSize; j++)
			{
				cells[i, j].cell.density = fluidCells[i, j].density;
				cells[i, j].cell.velocity = fluidCells[i, j].velocity;
				cells[i, j].cell.rawDivergence = fluidCells[i, j].rawDivergence;
				cells[i, j].cell.relaxedDivergence = fluidCells[i, j].relaxedDivergence;
			}
		}
	}
}
