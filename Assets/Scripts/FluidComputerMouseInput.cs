using UnityEngine;
using System.Collections;

/*[RequireComponent(typeof(FluidComputer))]
public class FluidComputerMouseInput : MonoBehaviour
{

	FluidComputer fluidComputer;
	public float densityChangeRate;
	public float densityChangeRadius;
	public float forceRate;
	public float forceRadius;
	public bool alwaysAddDensity = false;
	public bool alwaysAddVelocity = true;

	private Vector3 oldMousePos;
	private Vector3 mouseMove;

	void Awake()
	{
		fluidComputer = GetComponent<FluidComputer>();
	}

	void Update()
	{
		Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, fluidComputer.transform.position.z));
		mouseMove = mousePosition - oldMousePos;
		oldMousePos = mousePosition;

		bool leftMouse = Input.GetMouseButton(0);
		bool rightMouse = Input.GetMouseButton(1);
		bool middleMouse = Input.GetMouseButton(2);
		bool anyMouse = leftMouse || rightMouse || middleMouse;

		RaycastHit hit;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
		{
			float densityChange = 0;
			Vector3 force = Vector3.zero;

			if (anyMouse)
			{
				if (leftMouse)
				{
					densityChange += densityChangeRate * Time.deltaTime;
					force += mouseMove * forceRate * Time.deltaTime;
				}
				if (rightMouse)
				{
					densityChange -= densityChangeRate * Time.deltaTime;
					force += mouseMove * forceRate * Time.deltaTime;
				}
				if (middleMouse && !leftMouse && !rightMouse)
				{
					force += mouseMove * forceRate * Time.deltaTime;
				}
			}
			else
			{
				if (alwaysAddDensity)
				{
					densityChange += densityChangeRate * Time.deltaTime;
				}
				if (alwaysAddVelocity)
				{
					force += mouseMove * forceRate * Time.deltaTime;
				}
			}

			if (densityChange != 0 || force.sqrMagnitude > 0)
			{

				fluidComputer.AddExternal(hit.collider.GetComponent<ComputeFluidCellRenderer>(), densityChange, densityChangeRadius, force, forceRadius);
			}
		}
	}
}*/