using UnityEngine;
using System.Collections;

public abstract class FluidCollider : MonoBehaviour
{
	public FluidSimulator Simulator { get; private set; }

	public void Initialize(FluidSimulator simulator)
	{
		if (Simulator == null)
		{
			Simulator = simulator;
		}
	}

	public abstract void SelectCell(Vector3 focalPoint);

	public abstract void AddExternal(Vector3 focalPoint, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius);
}
