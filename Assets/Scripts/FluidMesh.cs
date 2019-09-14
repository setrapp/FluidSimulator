using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FluidMeshParameters
{
    public float cellMaxDensity;
    public float cellMaxSpeed;
    public float gridSize;
}

[RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class FluidMesh : MonoBehaviour
{
    MeshRenderer meshRenderer;

    public void InitializeMesh(int cellGridSize, int vertexGridSize)
    {
		// TODO INFINITE LOOP AT SIZE 1 (and maybe 2)

        float vertexGapSize = 1f / (vertexGridSize - 1);
		float startPoint = -0.5f;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        	
        for (int j = 0; j < vertexGridSize; j++)
        {
            float v = (float)j / (vertexGridSize - 1);
            for (int i = 0; i < vertexGridSize; i++)
            {
                float u = (float)i / (vertexGridSize - 1);
                vertices.Add(new Vector3(startPoint + (vertexGapSize * i), startPoint + (vertexGapSize * j), 0));
                uvs.Add(new Vector2(u, v));
            }
        }

        int vertArea = vertexGridSize * vertexGridSize;
        int outsideVertCount = (vertexGridSize * 4) - 4; // Perimeter - corner duplicates
        int insideVertCount = vertArea - outsideVertCount;

        int indexCount = 
            (insideVertCount * 6) +			// All interior vertices touch 6 triangles.
            ((outsideVertCount - 4) * 3) +	// All edge vertices touch 3 triangles.
            (2 * 2) +						// Half of corners touch 2 triangles.
            (2 * 1);						// Half of corners touch 1 triangle.

        int[] indices = new int[indexCount];

		// Populate indices in order of where they'd appear in a 2-triangle rectangle.
		int iPerTriPair = 6;
		int triPairs = indexCount / iPerTriPair;
		int[] startingVertices = new int[] { 0, 1, vertexGridSize, 1, vertexGridSize + 1, vertexGridSize };
		for (int indexInGroup = 0; indexInGroup < iPerTriPair; indexInGroup++)
		{
			int vertex = 0;
			for (int i = 0; i < triPairs; i++)
			{
				if ((vertex % vertexGridSize) == (vertexGridSize - 1))
				{
					vertex++;
				}

				indices[(i * iPerTriPair) + indexInGroup] = vertex + startingVertices[indexInGroup];
				vertex++;
			}
		}

		var mesh = GetComponent<MeshFilter>().mesh;
		mesh.SetVertices(vertices);
		mesh.SetUVs(0, uvs);
		mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        meshRenderer = GetComponent<MeshRenderer>();
        Material meshMaterial = meshRenderer.material;

        // TODO what do these do, should they use cellGridSize or vert
        meshMaterial.SetVector("_GridSize", new Vector4(cellGridSize, cellGridSize, 1, 0));
        //meshMaterial.SetFloat("_CellSize", 0.5f / vertexGridSize);
    }

    public void PreRenderMesh(ComputeBuffer fluidCells, FluidMeshParameters parameters)
    {
		Material meshMaterial = meshRenderer.material;

        //TODO: Compute buffers seem occasionally get unbound in Editor when using Metal. This does not appear to happen in builds. FIX?
        // TODO: This requires Shader Model 4.5. Should less efficient support be added for older versions?
        // Bind _fluidCells as the first target of random access writting. This is required in for proper shader access in Metal.
        Graphics.ClearRandomWriteTargets();
        meshMaterial.SetBuffer("_FluidCells", fluidCells);
        Graphics.SetRandomWriteTarget(1, fluidCells, false);

        if (meshMaterial.HasProperty("_MaxDensity"))
        {
            meshRenderer.material.SetFloat("_MaxDensity", parameters.cellMaxDensity);
        }
        if (meshMaterial.HasProperty("_MaxSpeed"))
        {
            meshMaterial.SetFloat("_MaxSpeed", parameters.cellMaxSpeed);
        }
        meshMaterial.SetFloat("_CellSize", 0.5f / parameters.gridSize);
    }
}
