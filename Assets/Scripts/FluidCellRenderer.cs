using System;
using System.Runtime.InteropServices;
using UnityEngine;

[RequireComponent(typeof(FluidColliderCell))]
public class FluidCellRenderer : MonoBehaviour {

	[SerializeField]
	Renderer solidRenderer;
	[SerializeField]
	Renderer outlineRenderer;
	[SerializeField]
	Renderer centerRenderer;
	[SerializeField]
	public FluidCell cell;
	[SerializeField]
	FluidRenderer fluidRenderer;
	public FluidRenderer FluidRenderer { get { return fluidRenderer; } }
	FluidSimulator simulator;

	// TODO Use FluidCellIndex here
	public int XIndex { get; private set; }
	public int YIndex { get; private set; }

	public void Initialize(FluidSimulator simulator, FluidRenderer fluidRenderer, FluidCell cellTemplate, int xIndex, int yIndex)
	{
		if (this.FluidRenderer != null)
		{
			Debug.LogError("Attempting to re-initialize FluidCellRenderer.");
		}

		this.fluidRenderer = fluidRenderer;
		XIndex = xIndex;
		YIndex = yIndex;
		cell = new FluidCell()
		{
			density = cellTemplate.density,
			velocity = cellTemplate.velocity,
			rawDivergence = cellTemplate.rawDivergence,
			relaxedDivergence = cellTemplate.relaxedDivergence,
		};
		this.simulator = simulator;
		GetComponent<FluidColliderCell>().Initialize(simulator, new FluidCellIndex(xIndex, yIndex, 0));
	}

	void Start()
	{
		SetRenderedColor();
	}

	void Update()
	{
		if (solidRenderer.enabled != FluidRenderer.visualizationFlags.solidVisible)
		{
			solidRenderer.enabled = FluidRenderer.visualizationFlags.solidVisible;
		}
		if (outlineRenderer.enabled != FluidRenderer.visualizationFlags.outlineVisible)
		{
			outlineRenderer.enabled = FluidRenderer.visualizationFlags.outlineVisible;
		}
		if (centerRenderer.enabled != FluidRenderer.visualizationFlags.centerVisible)
		{
			centerRenderer.enabled = FluidRenderer.visualizationFlags.centerVisible;
		}

		SetRenderedColor();
	}

	void SetRenderedColor()
	{
		if (FluidRenderer.visualizationFlags.densityVisible)
		{
			Color densityColor = new Color(0, 0, Mathf.Min(cell.density, FluidRenderer.cellParameters.cellMaxDensity) / FluidRenderer.cellParameters.cellMaxDensity, 1);
			solidRenderer.material.color = densityColor;
		}
	}

	void OnDrawGizmos()
	{
		// TODO This may not be useful beyond testing... Or maybe it be.
		float minSpeedToRender = 0;// 0.1f;

		if (FluidRenderer.visualizationFlags.velocityVisible && cell.velocity.sqrMagnitude > (minSpeedToRender * minSpeedToRender))
		{
			float speed = cell.velocity.magnitude;
			Gizmos.color = Color.red;
			Vector3 linePos = transform.position - new Vector3(0, 0, fluidRenderer.cellParameters.cellSize);
			Vector3 direction = cell.velocity / speed;
			float halfCellSize = fluidRenderer.cellParameters.cellSize * 0.5f;
			Gizmos.DrawLine(linePos, linePos + (direction * halfCellSize) + (direction * halfCellSize * (speed / FluidRenderer.cellParameters.cellMaxSpeed)));
		}
	}
}
