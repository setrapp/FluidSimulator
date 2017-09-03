using UnityEngine;
using System.Collections;

public class FluidDispatcher : MonoBehaviour {

	public FluidMode mode = FluidMode.CPU;
	private FluidSimulator simulator;
	public FluidSimulator Simulator { get { return simulator; } }
	[SerializeField]
	private FluidSimulator cpuSimulator;
	[SerializeField]
	private FluidSimulator gpgpuSimulator;
	public bool initializeOnStart = true;
	public string FamilyName { get; private set; }

	public enum FluidMode
	{
		CPU = 0,
		GPGPU
	}

	void Awake()
	{
		FamilyName = name.Substring(0, name.IndexOf("Dispatcher")).TrimEnd();
		gameObject.name = string.Format("{0} Dispatcher", FamilyName);

		string nullReference = "Attempting to use {0} fluid pool that does not exist.";
		switch (mode)
		{
			case FluidMode.CPU:
				simulator = cpuSimulator;
				if (cpuSimulator != null) { cpuSimulator.gameObject.SetActive(true); }
				else { throw new System.NullReferenceException(string.Format(nullReference, "CPU")); }
				if (gpgpuSimulator != null) { Destroy(gpgpuSimulator.gameObject); }
				break;
			case FluidMode.GPGPU:
				simulator = gpgpuSimulator;
				if (gpgpuSimulator != null) { gpgpuSimulator.gameObject.SetActive(true); }
				else { throw new System.NullReferenceException(string.Format(nullReference, "GPGPU")); }
				if (cpuSimulator != null) { Destroy(cpuSimulator.gameObject); }
				break;
		}
	}

	void Start()
	{
		if (initializeOnStart)
		{
			simulator.Initialize(FamilyName);
		}
	}

	void Update()
	{
		// TODO This only needs to happen in Editor.
		if (simulator != null)
		{
		}
	}
}
