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
	[Range(0, 10)]
	public float dragFactor = 0.5f;
	new public Collider collider;

	// TODO update this based on which FluidCollider is hit (OnCollisionEnter will not work if it requires a non-kinematic rigidbody)
	public FluidSampler sampler;

	Vector3 cellVelocity;

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

		//Vector3 cellVelocity;
		if (false)//collider as SphereCollider != null)
		{
			cellVelocity = sampler.SampleCircle(transform.position, ((SphereCollider)collider).radius).velocity;
		}
		else
		{
			cellVelocity = sampler.SamplePoint(transform.position).velocity;
		}

		CellParameters cellParameters = sampler.Simulator.cellParameters;
		float maxSpeed = ((cellParameters.cellMaxSpeed / cellParameters.cellSize) * maxSpeedUtilization) / Time.deltaTime;

		/*if (mimicFluidVelocity)
		{
			velocity = cellVelocity / mass;
		}
		else
		{
			velocity += ((cellVelocity / mass));// * Time.deltaTime);
		}
		velocity.z = is2D ? 0 : velocity.z;
		if(maxSpeed >= 0 && velocity.sqrMagnitude > (maxSpeed * maxSpeed))
		{
			velocity = velocity.normalized * maxSpeed;
		}

		// Time.deltaTime is not used here because it not used in the fluid simulation.
		//transform.position += velocity;

		Vector3 drag = velocity * dragFactor;
		velocity -= drag * Time.deltaTime;*/

		velocity = cellVelocity / Time.deltaTime;
		
		var body = GetComponent<Rigidbody>();
		// TODO Not even close..
		// TODO Do not need mass or drag if using rigidbody
		var fluidContribution = 1;// velocity.magnitude / maxSpeed;
		var bodyContribution = Mathf.Clamp(body.velocity.magnitude / Mathf.Max(velocity.magnitude, 0.001f), 0, 1);//Mathf.Max(body.velocity.magnitude / maxSpeed, 0.001f);
		Debug.Log(bodyContribution);
		body.AddForceAtPosition(((velocity - body.velocity) * (1 - bodyContribution)) / Time.deltaTime, transform.position);

		//sampler.Simulator.GetComponent<FluidCollider>().AddExternal(transform.position, 0, 0, velocity / Time.deltaTime, 2);



		//body.velocity = velocity / Time.deltaTime;// cellVelocity / Time.deltaTime;

		if (maxSpeed >= 0 && body.velocity.sqrMagnitude > (maxSpeed * maxSpeed))
		{
			body.velocity = body.velocity.normalized * maxSpeed;
		}

	}

	/*void OnCollisionEnter(Collision collision)
	{
		var body = GetComponent<Rigidbody>();
		body.velocity = collision.impulse.normalized * velocity.magnitude;
	}*/

	/*void OnDrawGizmos()
	{
		Vector3 linePos = transform.position + new Vector3(0, 0, -1);
		Gizmos.color = Color.white;
		Gizmos.DrawLine(linePos, linePos + cellVelocity / Time.deltaTime);
		Gizmos.color = Color.red;
		Gizmos.DrawLine(linePos, linePos + GetComponent<Rigidbody>().velocity);
	}*/
}
