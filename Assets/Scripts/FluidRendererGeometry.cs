using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FluidColliderBox))]
public class FluidRendererGeometry : FluidRenderer, IRenderBuffer
{
	//[SerializeField] // TODO Decouple vertex grid size and cell grid size (multiple triangle pairs per cell).
	int verticesPerSide = 0;
    [SerializeField]
    FluidMesh fluidDensity = null;
    [SerializeField]
    FluidMesh fluidVelocity = null;

    protected override string generateCells()
    {
        string result = null;
        int gridSize = fluidParameters.gridSize;
        float physicalSize = fluidParameters.physicalSize;

        // Each cell requires 4 vertices to render, so 1x1 cell grid is a 2x2 vertex grid. This scales as long as the grid is square.
		// Because of this the minium number of vertices per side is (cell grid size + 1);
        verticesPerSide = Mathf.Max(verticesPerSide, gridSize + 1);

        // TODO Either prevent reinitalizing or handle it.

        if (string.IsNullOrEmpty(result))
        {
            fluidDensity.InitializeMesh(gridSize, verticesPerSide);
            fluidVelocity.InitializeMesh(gridSize, verticesPerSide);

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

        if (fluidDensity.gameObject.activeSelf != visualizationFlags.densityVisible)
        {
            fluidDensity.gameObject.SetActive(visualizationFlags.densityVisible);
        }
        if (fluidVelocity.gameObject.activeSelf != visualizationFlags.velocityVisible)
        {
            fluidVelocity.gameObject.SetActive(visualizationFlags.velocityVisible);
        }

        if (fluidDensity.gameObject.activeSelf)
        {
            fluidDensity.PreRenderMesh(fluidCells, renderParameters);
        }
        if (fluidVelocity.gameObject.activeSelf)
        {
            fluidVelocity.PreRenderMesh(fluidCells, renderParameters);
        }
    }
}
