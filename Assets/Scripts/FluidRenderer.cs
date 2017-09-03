using UnityEngine;
using System.Collections;

namespace junk
{
	public class FluidRenderer : MonoBehaviour
	{

		public bool resetFluid = false;
		public Color clearColor = new Color(0, 0, 0, 1);
		public Collider inputCollider;
		public Renderer outputRenderer;
		public RenderTexture fluidTexture1;
		public RenderTexture fluidTexture2;
		public Material fluidOperationMaterial;
		public Material externalAdditionsMaterial;
		public Texture externalAdditionsTexture;
		public bool applyExternals = true;
		public bool diffuse = true;
		public bool advect = true;
		public bool conserveMass = true;
		private FluidOperationPass[] operations;
		public float diffusion = 0.05f;
		public int relaxationIterations = 20;
		// TODO Figure out how to render velocity field (probably a separate quad)

		RenderTexture inputTexture;
		RenderTexture outputTexture;
		int operationPassNumber = 0;

		//private delegate void FluidOperation(FluidCell[,] inputCells, FluidCell[,] outputCells);
		private enum FluidOperation
		{
			// TODOConsider reorganizing these to Reset, Advect, ApplyExternals, Diffuse, Project (better name for ConserveMass)
			// This better aligns with process outlined in book.
			Reset = 0,
			ApplyExternals,
			Diffuse,
			Advect,
			ConserveMass1,
			ConserveMass2,
			ConserveMass3,
			ConserveMass4
		}

		public enum BoundaryCondition
		{
			CONTAIN = 0,
			//WRAP TODO Make work
			//LEAK TODO Implement
		}

		class FluidOperationPass
		{
			public FluidOperation operation;
			public int relaxationIterations;
			public bool swapAfter;

			public FluidOperationPass(FluidOperation operation, int relaxationIterations, bool swapAfter)
			{
				this.operation = operation;
				this.relaxationIterations = relaxationIterations;
				this.swapAfter = swapAfter;
			}
		}

		void Awake()
		{
			inputTexture = fluidTexture1;
			outputTexture = fluidTexture2;

			// Reset storage textures to default value.
			ResetFluid();
			RenderFluid(outputTexture);
		}

		void Update()
		{
			if (resetFluid)
			{
				resetFluid = false;
				ResetFluid();
				RenderFluid(inputTexture);
			}

			// temporary
			//RenderFluid(GetComponent<FluidRendererMouseInput>().tempStorageTexture);

			// TODO reset external additions and figure out where this goes
			//Graphics.Blit(GetComponent<FluidRendererMouseInput>().storageTexture, GetComponent<FluidRendererMouseInput>().storageTexture, externalAdditionsMaterial, 0);
			fluidOperationMaterial.SetVector("_TextureSize", new Vector4(inputTexture.width, inputTexture.height, 0, 0));
			fluidOperationMaterial.SetColor("_ClearColor", clearColor);
			fluidOperationMaterial.SetFloat("_DTDiffusion", (diffusion * Time.deltaTime * inputTexture.width * inputTexture.height) / relaxationIterations);

			Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 1);
			SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);

			if (diffuse)
			{
				for (int i = 0; i < relaxationIterations; i++)
				{
					Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 2);
					SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);
				}
			}
			if (advect)
			{
				if (conserveMass)
				{
					tempConserveMass();
				}

				Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 3);
				SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);
			}
			if (conserveMass)
			{
				tempConserveMass();
			}




			RenderFluid(inputTexture);


			SendMessage("ClearAuxiliaryBuffers");

			// TODO Does this need to happen every frame?
			OrderOperations();

			for (int i = 0; i < operations.Length; i++)
			{
				if (operations[i] != null)
				{
					for (int j = 0; j < operations[i].relaxationIterations; j++)
					{
						//operations[i].operation(inputCells, outputCells);
						/*if (operations[i].swapAfter)
						{
							SwapBuffers(cellBuffer1, cellBuffer2, out inputCells, out outputCells, operationPassNumber++);
						}*/
					}
				}
			}

			//ApplyCells(inputCells);
			operationPassNumber = operationPassNumber % 2;

			// Clear out external additions.
			/*for (int j = 1; j < poolGridSize - 1; j++)
			{
				for (int i = 1; i < poolGridSize - 1; i++)
				{
					externalAdditions[i, j].density = 0;
					externalAdditions[i, j].velocity = Vector3.zero;
				}
			}*/
		}

		void tempConserveMass()
		{
			Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 4);
			SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);

			Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 5);
			SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);

			for (int i = 0; i < relaxationIterations; i++)
			{
				Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 6);
				SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);
			}

			Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 7);
			SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);

			Graphics.Blit(inputTexture, outputTexture, fluidOperationMaterial, 8);
			SwapBuffers(fluidTexture1, fluidTexture2, out inputTexture, out outputTexture, operationPassNumber++);
		}

		void OrderOperations()
		{
			operations = new FluidOperationPass[]
			{
			!applyExternals ? null : new FluidOperationPass(FluidOperation.ApplyExternals, 1, true),
			!diffuse        ? null : new FluidOperationPass(FluidOperation.Diffuse, relaxationIterations, !conserveMass),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass1, 1, false),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass2, 1, false),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass3, relaxationIterations, false),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass4, 1, true),
			!advect         ? null : new FluidOperationPass(FluidOperation.Advect, 1, !conserveMass),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass1, 1, false),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass2, 1, false),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass3, relaxationIterations, false),
			!conserveMass   ? null : new FluidOperationPass(FluidOperation.ConserveMass4, 1, true)
			};
		}

		void RenderFluid(Texture displayTexture)
		{
			if (outputRenderer != null && displayTexture != null)
			{
				// TODO Should render scale be world space, or should mouse velocity be local (FluidDisplay) space?
				outputRenderer.material.SetFloat("_RenderScale", 1 / outputRenderer.transform.localScale.x);
				RenderTexture outputTexture = (RenderTexture)outputRenderer.material.mainTexture;
				outputRenderer.material.mainTexture = displayTexture;
				//Graphics.Blit(displayTexture, outputTexture, outputRenderer.material, 0);
				//outputRenderer.material.mainTexture = outputTexture;
				//outputRenderer.material.mainTexture = displayTexture;
			}
		}

		void SwapBuffers(RenderTexture buffer1, RenderTexture buffer2, out RenderTexture inputBuffer, out RenderTexture outputBuffer, int operationPassNumber)
		{
			if (operationPassNumber % 2 == 0)
			{
				inputBuffer = buffer1;
				outputBuffer = buffer2;
			}
			else
			{
				inputBuffer = buffer2;
				outputBuffer = buffer1;
			}
		}

		public void SetExternalAdditionsTexture(Texture externalAdditions)
		{
			externalAdditionsTexture = externalAdditions;
			fluidOperationMaterial.SetTexture("_AdditionTex", externalAdditionsTexture);
		}

		void ResetFluid()
		{
			fluidOperationMaterial.SetColor("_ClearColor", clearColor);
			Graphics.Blit(fluidTexture2, fluidTexture1, fluidOperationMaterial, 0);
			Graphics.Blit(fluidTexture1, fluidTexture2);
		}
	}
}
