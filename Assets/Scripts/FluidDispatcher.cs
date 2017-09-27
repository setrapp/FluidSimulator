using System.Collections.Generic;
using UnityEngine;

public class FluidDispatcher : MonoBehaviour
{
	public SimulatorMode simulatorMode = SimulatorMode.CPU;
	FluidSimulator simulator;
	public FluidSimulator Simulator { get { return simulator; } }
	[SerializeField]
	List<SimulatorOption> simulatorOptions;
	public RendererMode rendererMode = RendererMode.Objects;
	new FluidRenderer renderer;
	public FluidRenderer Renderer { get { return renderer; } }
	[SerializeField]
	List<RendererOption> rendererOptions;

	public bool initializeOnStart = true;
	public string FamilyName { get; private set; }//TODO Is this redundant now?
	public FluidInfo info;

	public enum SimulatorMode
	{
		CPU = 0,
		GPGPU
	}

	public enum RendererMode
	{
		Objects = 0,
		Pixels,
		Geometry
	}

	[System.Serializable]
	private class SimulatorOption
	{
		public SimulatorMode mode;
		public FluidSimulator simulator;
	}

	[System.Serializable]
	private class RendererOption
	{
		public RendererMode mode;
		public FluidRenderer renderer;
	}

	void Awake()
	{
		FamilyName = name.Substring(0, name.IndexOf("Dispatcher")).TrimEnd();
		gameObject.name = string.Format("{0} Dispatcher", FamilyName);

		string nullReference = "Attempting to use {0} {1} that does not exist.";

		for (int i = 0; i < simulatorOptions.Count; i++)
		{
			if (simulator == null && simulatorOptions[i].mode == simulatorMode)
			{
				simulator = simulatorOptions[i].simulator;
				simulator.gameObject.SetActive(true);
			}
			else
			{
				Destroy(simulatorOptions[i].simulator.gameObject);
				simulatorOptions.RemoveAt(i);
				i--;
			}
		}
		if (simulator == null)
		{
			throw new System.NullReferenceException(string.Format(nullReference, simulatorMode, "Simulator"));
		}

		for (int i = 0; i < rendererOptions.Count; i++)
		{
			if (renderer == null && rendererOptions[i].mode == rendererMode)
			{
				renderer = rendererOptions[i].renderer;
				renderer.gameObject.SetActive(true);
			}
			else
			{
				Destroy(rendererOptions[i].renderer.gameObject);
				rendererOptions.RemoveAt(i);
				i--;
			}
		}
		if (renderer == null)
		{
			throw new System.NullReferenceException(string.Format(nullReference, rendererMode, "Renderer"));
		}
	}

	void Start()
	{
		if (initializeOnStart)
		{
			simulator.Initialize(FamilyName, info, renderer);
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
