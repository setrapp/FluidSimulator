﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct FluidMeshParameters
{
	public float cellMaxDensity;
	public float cellMaxSpeed;
	public float gridSize;
}

public class FluidMesh : MonoBehaviour
{
	MeshRenderer meshRenderer;

	public void InitializeMesh(int gridSize, Material geometryMaterial)
	{
		float cellSize = 1f / gridSize;
		float center = (gridSize - 1) / 2f;
		List<Vector3> vertices = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		int[] indices = new int[gridSize * gridSize];

		for (int i = 0; i < gridSize; i++)
		{
			float u = (float)i / gridSize;
			for (int j = 0; j < gridSize; j++)
			{
				float v = (float)j / gridSize;
				int index = (i * gridSize) + j;
				vertices.Add(new Vector3((i - center) * cellSize, (j - center) * cellSize, 0));
				uvs.Add(new Vector2(u, v));
				indices[index] = index;
			}
		}
		Mesh mesh = new Mesh();
		mesh.SetVertices(vertices);
		mesh.SetUVs(0, uvs);
		mesh.SetIndices(indices, MeshTopology.Points, 0);
		GetComponent<MeshFilter>().mesh = mesh;

		meshRenderer = GetComponent<MeshRenderer>();
		meshRenderer.material = geometryMaterial;
		meshRenderer.material.SetVector("_GridSize", new Vector4(gridSize, gridSize, 1, 0));
		meshRenderer.material.SetFloat("_CellSize", 0.5f / gridSize);
	}

	public void PreRenderMesh(ComputeBuffer fluidCells, FluidMeshParameters parameters)
	{
		meshRenderer.material.SetBuffer("_FluidCells", fluidCells);
		meshRenderer.material.SetFloat("_MaxDensity", parameters.cellMaxDensity);
		meshRenderer.material.SetFloat("_MaxSpeed", parameters.cellMaxSpeed);
		meshRenderer.material.SetFloat("_CellSize", 0.5f / parameters.gridSize);
	}
}
