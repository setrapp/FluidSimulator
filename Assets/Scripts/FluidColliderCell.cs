using UnityEngine;
using System.Collections;

public class FluidColliderCell : FluidCollider
{
	[SerializeField]
	FluidCellIndex index = new FluidCellIndex(-1, -1, -1);

	public void Initialize(FluidSimulator simulator, FluidCellIndex index)
	{
		this.index = index;
		base.Initialize(simulator);
	}

	public override void SelectCell(Vector3 focalPoint)
	{
		Simulator.SelectedCellIndex = index;
	}

	public override void AddExternal(Vector3 focalPoint, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius)
	{
		Simulator.AddExternal(index, densityChange, densityChangeRadius, force, forceRadius);
	}

}
