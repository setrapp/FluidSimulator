using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace junk
{
	public class FluidDebug : MonoBehaviour
	{
		[Header("Global Data")]
		public bool showGlobalData = true;
		public GameObject globalData = null;
		public Text globalDensity = null;

		[Header("Cell Data")]
		public bool showCellData = true;
		public GameObject cellData = null;
		public Text cellId = null;
		public Text cellVelocity = null;
		public Text cellDensity = null;

		[Header("Diffuse Data")]
		public bool showDiffuseData = true;
		public GameObject diffuseData = null;
		public Text leftId = null;
		public Text rightId = null;
		public Text downId = null;
		public Text upId = null;

		[Header("Advect Data")]
		public bool showAdvectData = true;
		public GameObject advectData = null;
		public Text idVelocity = null;
		public Text pastId = null;
		public Text leftPortion = null;
		public Text rightPortion = null;
		public Text downPortion = null;
		public Text upPortion = null;

		// TODO Move this into the inspector (time to learn Unity Editor)

		void Update()
		{
			// TODO Draw this debug Data
			// TODO Write this to inspector

			FluidCellRenderer selected = SelectFluidCell();

			if (globalData != null)
			{
				bool globalDataValid = showGlobalData && selected != null;
				if (globalData.activeSelf != globalDataValid)
				{
					globalData.SetActive(showGlobalData && selected != null);
				}

				if (globalDataValid)
				{
					//globalDensity.text = string.Format("Density ({0:0.##})", selected.Renderer.globalDensity);
				}
			}

			if (cellData != null)
			{
				bool cellDataValid = showCellData && selected != null;
				if (cellData.activeSelf != cellDataValid)
				{
					cellData.SetActive(showCellData && selected != null);
				}

				if (cellDataValid)
				{
					cellId.text = string.Format("Id ({0}, {1})", selected.XIndex, selected.YIndex);
					cellVelocity.text = string.Format("Velocity ({0:0.##}, {1:0.##})", selected.cell.velocity.x, selected.cell.velocity.y);
					cellDensity.text = string.Format("Density ({0:0.##})", selected.cell.density);
				}
			}

			if (diffuseData != null)
			{
				bool diffuseDataValid = showDiffuseData && selected != null;
				if (diffuseData.activeSelf != diffuseDataValid)
				{
					diffuseData.SetActive(showDiffuseData && selected != null && selected.Simulator.info.operationFlags.diffuse);
				}

				/*if (diffuseDataValid)
				{
					leftId.text = string.Format("Left Id ({0}, {1})", selected.diffuseData.leftId.x, selected.diffuseData.leftId.y);
					rightId.text = string.Format("Right Id ({0}, {1})", selected.diffuseData.rightId.x, selected.diffuseData.rightId.y);
					downId.text = string.Format("Down Id ({0}, {1})", selected.diffuseData.downId.x, selected.diffuseData.downId.y);
					upId.text = string.Format("Up Id ({0}, {1})", selected.diffuseData.upId.x, selected.diffuseData.upId.y);
				}*/
			}

			if (advectData != null)
			{
				bool advectDataValid = showAdvectData && selected != null;
				if (advectData.activeSelf != advectDataValid)
				{
					advectData.SetActive(showAdvectData && selected != null && selected.Simulator.info.operationFlags.advect);
				}

				/*if (advectDataValid)
				{
					idVelocity.text = string.Format("Id Velocity ({0:0.##}, {1:0.##})", selected.advectData.idVelocity.x, selected.advectData.idVelocity.y);
					pastId.text = string.Format("Old Id ({0:0.##}, {1:0.##})", selected.advectData.pastId.x, selected.advectData.pastId.y);
					leftPortion.text = string.Format("Left Portion {0:0.##}", 1 - selected.advectData.halfPortions.x);
					rightPortion.text = string.Format("Right Portion {0:0.##}", selected.advectData.halfPortions.x);
					downPortion.text = string.Format("Down Portion {0:0.##}", 1 - selected.advectData.halfPortions.y);
					upPortion.text = string.Format("Up Portion {0:0.##}", selected.advectData.halfPortions.y);
				} */
			}
		}

		FluidCellRenderer SelectFluidCell()
		{
			FluidCellRenderer selected = null;
			RaycastHit hit;
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
			{
				selected = hit.collider.GetComponent<FluidCellRenderer>();
			}
			return selected;
		}
	}
}
