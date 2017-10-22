using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidRider : MonoBehaviour
{
	public float mass = 1;
	public bool mimicFluidVelocity = false;
	public Vector3 velocity = Vector3.zero;
	[Range(0, 1)]
	public float maxSpeedUtilization = 1;
	new public Collider collider;

	// TODO update this based on which FluidCollider is hit (OnCollisionEnter will not work if it requires a non-kinematic rigidbody)
	public FluidSampler sampler;

	[HideInInspector]
	public bool is2D = true;

	private void Start()
	{
		if (collider == null)
		{
			collider = GetComponent<Collider>();
		}
	}

	private void Update()
	{
		mass = Mathf.Max(mass, 0.001f);
		if (sampler == null)
		{
			return;
		}

		Vector3 cellVelocity;
		if (collider as SphereCollider != null)
		{
			cellVelocity = sampler.SampleCircle(transform.position, ((SphereCollider)collider).radius).velocity;
		}
		else
		{
			cellVelocity = sampler.SamplePoint(transform.position).velocity;
		}

		CellParameters cellParameters = sampler.Simulator.cellParameters;
		float maxSpeed = (cellParameters.cellMaxSpeed / cellParameters.cellSize) * maxSpeedUtilization;
		Debug.Log(maxSpeed);
		if (mimicFluidVelocity)
		{
			velocity = cellVelocity / mass;
		}
		else
		{
			velocity += ((cellVelocity / mass) * Time.deltaTime);
		}
		velocity.z = is2D ? 0 : velocity.z;
		if(maxSpeed >= 0 && velocity.sqrMagnitude > (maxSpeed * maxSpeed))
		{
			velocity = velocity.normalized * maxSpeed;
		}

		// Time.deltaTime is not used here because it not used in the fluid simulation.
		transform.position += velocity;
	}
}
