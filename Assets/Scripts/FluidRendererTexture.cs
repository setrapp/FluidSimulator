using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class FluidRendererTexture : FluidRenderer, IRenderBuffer
{
	[SerializeField]
	Material operationsMaterial;
	RenderTexture cellsTexture;
	MeshRenderer mesh;

	void Start()
	{
		mesh = GetComponent<MeshRenderer>();
	}

	protected override string generateCells()
	{
		string result = null;
		FluidSimulator.FluidSimulatorInfo info = Simulator.info;
		int gridSize = info.fluidParameters.gridSize;
		float physicalSize = info.fluidParameters.physicalSize;

		bool isPowerOfTwo = (gridSize & (gridSize - 1)) == 0;
		if (!isPowerOfTwo)
		{
			result = string.Format("{0} must use power-of-2 size texture. Please alter grid size.", gameObject.name);
		}
		else if (operationsMaterial == null)
		{
			result = string.Format("{0} has now attached Cells Material", gameObject.name);
		}

		// Texture must be power-of-two.
		if (string.IsNullOrEmpty(result))
		{
			cellsTexture = new RenderTexture(gridSize, gridSize, 0);
			cellsTexture.enableRandomWrite = true;
			cellsTexture.format = RenderTextureFormat.ARGBFloat;
			cellsTexture.Create();

			operationsMaterial.SetVector("_TextureSize", new Vector4(gridSize, gridSize, 1, 0));
			mesh.material.SetTexture("_MainTex", cellsTexture);
			mesh.transform.localScale = new Vector3(physicalSize, physicalSize, physicalSize);
		}

		return result;
	}

	void IRenderBuffer.RenderCells(ComputeBuffer fluidCells)
	{
		operationsMaterial.SetBuffer("_FluidCells", fluidCells);
		Graphics.Blit(cellsTexture, cellsTexture, operationsMaterial);
	}

	void OnDestroy()
	{
		if (cellsTexture != null)
		{
			cellsTexture.Release();
		}
	}
}
