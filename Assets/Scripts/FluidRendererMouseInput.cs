using UnityEngine;
using System.Collections;

namespace junk
{
	[RequireComponent(typeof(FluidRenderer))]
	public class FluidRendererMouseInput : MonoBehaviour
	{

		private FluidRenderer fluidRenderer;
		public Color clearColor = new Color(0, 0, 0, 1);
		public float densityChangeRate;
		public float densityChangeRadius;
		public float forceRate;
		public float forceRadius;
		public bool alwaysAddVelocity = true;
		public Material storageControllingMaterial;
		public RenderTexture storageTexture1;
		public RenderTexture storageTexture2;

		private Vector3 oldMousePos;
		private Vector3 mouseMove;

		void Awake()
		{
			fluidRenderer = GetComponent<FluidRenderer>();
			oldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, fluidRenderer.transform.position.z));

			// Reset storage textures to default value.
			ClearAuxiliaryBuffers();
		}

		bool flip = false;
		void Update()
		{
			fluidRenderer.fluidOperationMaterial.SetColor("_ExternalClearColor", clearColor);

			Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, fluidRenderer.transform.position.z));
			mouseMove = mousePosition - oldMousePos;
			oldMousePos = mousePosition;

			bool leftMouse = Input.GetMouseButton(0);
			bool rightMouse = Input.GetMouseButton(1);
			bool middleMouse = Input.GetMouseButton(2);
			if (true)// leftMouse || rightMouse || middleMouse)
			{
				RaycastHit hit;
				if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
				{
					if (hit.collider == fluidRenderer.inputCollider)
					{
						float densityChange = 0;
						Vector3 force = Vector3.zero;

						if (true)//leftMouse)
						{
							densityChange += densityChangeRate;
							force += mouseMove * forceRate;
						}
						if (rightMouse)
						{
							densityChange -= densityChangeRate;
							force += mouseMove * forceRate;
						}
						if (middleMouse && !leftMouse && !rightMouse)
						{
							force += mouseMove * forceRate;
						}

						if (densityChange != 0 || force.sqrMagnitude > 0)
						{
							Vector4 location = (Vector4)fluidRenderer.outputRenderer.transform.InverseTransformPoint(hit.point);
							Vector4 densityChangeRadiusVector = new Vector4(0, 0, 0, densityChangeRadius);
							storageControllingMaterial.SetVector("_Location", location + densityChangeRadiusVector);
							storageControllingMaterial.SetVector("_Addition", new Vector4(force.x, force.y, force.z, densityChange * Time.deltaTime));

							// TODO could I just pass in old location and calculate change inside the shader?
							// nope because force could be any number
							//Debug.Log(storageControllingMaterial.GetVector("_Addition") + " " + densityChangeRate);
							RenderTexture inTexture = (flip) ? storageTexture1 : storageTexture2;
							RenderTexture outTexture = (flip) ? storageTexture2 : storageTexture1;

							// Allow tracking of storage material in editor.
							if (Application.isEditor)
							{
								storageControllingMaterial.SetTexture("_MainTex", outTexture);
							}

							Graphics.Blit(inTexture, outTexture, storageControllingMaterial, 1);
							flip = !flip;
							storageControllingMaterial.SetFloat("_Addition", 0);
							fluidRenderer.SetExternalAdditionsTexture(outTexture);
							//Graphics.Blit(outTexture, fluidRenderer.externalAdditionsTexture);
							//Debug.Log("hit" + Time.time);
						}
					}
				}
			}
		}

		void ClearAuxiliaryBuffers()
		{
			storageControllingMaterial.SetColor("_ClearColor", clearColor);
			Graphics.Blit(storageTexture2, storageTexture1, storageControllingMaterial, 0);
			Graphics.Blit(storageTexture1, storageTexture2);
		}
	}
}
