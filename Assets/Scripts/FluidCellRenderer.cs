using System;
using System.Runtime.InteropServices;
using UnityEngine;

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
	FluidSimulator simulator;
	public FluidSimulator Simulator { get { return simulator; } }
	public int XIndex { get; private set; }
	public int YIndex { get; private set; }

	public void Initialize(FluidSimulator simulator, FluidCell cellTemplate, int xIndex, int yIndex)
	{
		if (this.Simulator != null)
		{
			Debug.LogError("Attempting to re-initialize FluidCellRenderer.");
		}

		this.simulator = simulator;
		XIndex = xIndex;
		YIndex = yIndex;
		cell = new FluidCell()
		{
			density = cellTemplate.density,
			velocity = cellTemplate.velocity,
			rawDivergence = cellTemplate.rawDivergence,
			relaxedDivergence = cellTemplate.relaxedDivergence,
		};

	}

	void Start()
	{
		SetRenderedColor();
	}

	void Update()
	{
		if (solidRenderer.enabled != Simulator.info.visualizationFlags.solidVisible)
		{
			solidRenderer.enabled = Simulator.info.visualizationFlags.solidVisible;
		}
		if (outlineRenderer.enabled != Simulator.info.visualizationFlags.outlineVisible)
		{
			outlineRenderer.enabled = Simulator.info.visualizationFlags.outlineVisible;
		}
		if (centerRenderer.enabled != Simulator.info.visualizationFlags.centerVisible)
		{
			centerRenderer.enabled = Simulator.info.visualizationFlags.centerVisible;
		}

		SetRenderedColor();
	}

	void SetRenderedColor()
	{
		solidRenderer.material.color = Color.black;
		outlineRenderer.material.color = Color.black;
		centerRenderer.material.color = Color.black;

		if (Simulator.info.visualizationFlags.densityVisible)
		{
			Color densityColor = new Color(0, 0, Mathf.Min(cell.density, Simulator.info.cellParameters.cellMaxDensity) / Simulator.info.cellParameters.cellMaxDensity, 1);
			solidRenderer.material.color = densityColor;
			outlineRenderer.material.color = densityColor * 0.75f;
			centerRenderer.material.color = densityColor * 0.75f;
		}
	}

	void OnDrawGizmos()
	{
		// TODO This may not be useful beyond testing... Or maybe it be.
		float minSpeedToRender = 0;// 0.1f;

		if (Simulator.info.visualizationFlags.velocityVisible && cell.velocity.sqrMagnitude > (minSpeedToRender * minSpeedToRender))
		{
			float speed = cell.velocity.magnitude;
			Gizmos.color = Color.red;
			Vector3 linePos = transform.position - new Vector3(0, 0, Simulator.CellSize);
			Vector3 direction = cell.velocity / speed;
			float halfCellSize = Simulator.CellSize * 0.5f;
			Gizmos.DrawLine(linePos, linePos + (direction * halfCellSize) + (direction * halfCellSize * (speed / Simulator.info.cellParameters.cellMaxSpeed)));
		}
	}
}
