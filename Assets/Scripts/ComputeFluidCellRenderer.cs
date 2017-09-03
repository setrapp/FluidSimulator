using UnityEngine;
using System.Collections;

/*[System.Serializable]
public class FluidCell
{
	public float density;
	public Vector2 velocity;
}*/

/*public class ComputeFluidCellRenderer : MonoBehaviour {

	public ComputeFluidCell cell = new ComputeFluidCell();
	public Renderer solidRenderer;
	public Renderer outlineRenderer;
	public Renderer centerRenderer;
	//public float maxDensity = 1;
	//public float maxSpeed = 3;
	//public float mass = 1;
	public FluidComputer pool;
	public int poolI = -1;
	public int poolJ = -1;

	public DiffuseData diffuseData;
	public AdvectData advectData;

	// TEMPORARY
	public Vector4 Data;
	public TextMesh DataDisplay;

	void Start()
	{
		SetRenderedColor();
	}

	void Update()
	{
		if (solidRenderer.enabled != pool.showSolid)
		{
			solidRenderer.enabled = pool.showSolid;
		}
		if (outlineRenderer.enabled != pool.showOutline)
		{
			outlineRenderer.enabled = pool.showOutline;
		}
		if (centerRenderer.enabled != pool.showCenter)
		{
			centerRenderer.enabled = pool.showCenter;
		}

		SetRenderedColor();

		//DataDisplay.text = string.Format("{0},{1},{2},{3} ", Data.x, Data.y, Data.z, Data.w);
	}

	void SetRenderedColor()
	{
		if (pool.showDensity)
		{
			Color densityColor = new Color(0, 0, Mathf.Min(cell.color.a, pool.cellMaxDensity) / pool.cellMaxDensity, 1);
			if (densityColor != solidRenderer.material.color)
			{
				solidRenderer.material.color = densityColor;
				outlineRenderer.material.color = densityColor * 0.75f;
				centerRenderer.material.color = densityColor * 0.75f;
			}
		}
		else
		{
			solidRenderer.material.color = Color.black;
			outlineRenderer.material.color = Color.white;
			centerRenderer.material.color = Color.white;
		}
	}

	void OnDrawGizmos()
	{
		if (pool.showVelocity && cell.velocity.sqrMagnitude > 0)
		{
			float speed = cell.velocity.magnitude;
			Gizmos.color = Color.red;
			Vector3 linePos = transform.position - new Vector3(0, 0, pool.cellSize);
			Vector3 direction = cell.velocity / speed;
			float halfCellSize = pool.cellSize * 0.5f;
			Gizmos.DrawLine(linePos, linePos + (direction * halfCellSize * (speed / pool.cellMaxSpeed)));
		}
	}

	void OnMouseOver()
	{
		if (Input.GetMouseButton(0))
		{
			cell.density += pool.densityAddRate * Time.deltaTime;
		}

		if (Input.GetMouseButton(1))
		{
			cell.velocity = mouseMove;
		}


		//particle.velocity = new Vector3(Random.Range(-maxSpeed, maxSpeed), Random.Range(-maxSpeed, maxSpeed), 0);

		//Debug.Log("Velocity " + particle.velocity);
		//pool.InterpolateNearbyParticles((Vector2)transform.localPosition + particle.velocity);
	}
} */
