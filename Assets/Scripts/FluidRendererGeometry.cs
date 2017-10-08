using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(FluidColliderBox))]
public class FluidRendererGeometry : FluidRenderer, IRenderBuffer
{
	[SerializeField]
	FluidMesh FluidDensity;
	[SerializeField]
	FluidMesh fluidVelocity;

	protected override string generateCells()
	{
		string result = null;
		int gridSize = fluidParameters.gridSize;
		float physicalSize = fluidParameters.physicalSize;

		// TODO Either prevent reinitalizing or handle it.

		if (string.IsNullOrEmpty(result))
		{
			FluidDensity.InitializeMesh(gridSize);
			fluidVelocity.InitializeMesh(gridSize);

			transform.localScale = new Vector3(physicalSize, physicalSize, physicalSize);
			GetComponent<FluidColliderBox>().Initialize(Simulator);
		}

		return result;
	}

	void IRenderBuffer.RenderCells(ComputeBuffer fluidCells)
	{
		FluidMeshParameters renderParameters = new FluidMeshParameters()
		{
			cellMaxDensity = cellParameters.cellMaxDensity,
			cellMaxSpeed = cellParameters.cellMaxSpeed,
			gridSize = fluidParameters.gridSize
		};

		if (FluidDensity.gameObject.activeSelf != visualizationFlags.densityVisible)
		{
			FluidDensity.gameObject.SetActive(visualizationFlags.densityVisible);
		}
		if (fluidVelocity.gameObject.activeSelf != visualizationFlags.velocityVisible)
		{
			fluidVelocity.gameObject.SetActive(visualizationFlags.velocityVisible);
		}

		if (FluidDensity.gameObject.activeSelf)
		{
			FluidDensity.PreRenderMesh(fluidCells, renderParameters);
		}
		if (fluidVelocity.gameObject.activeSelf)
		{
			fluidVelocity.PreRenderMesh(fluidCells, renderParameters);
		}
	}
}
