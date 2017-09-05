﻿using UnityEngine;
using System.Collections;

public class FluidDispatcher : MonoBehaviour {

	public SimulatorMode simulatorMode = SimulatorMode.CPU;
	FluidSimulator simulator;
	public FluidSimulator Simulator { get { return simulator; } }
	[SerializeField]
	FluidSimulator cpuSimulator;
	[SerializeField]
	FluidSimulator gpgpuSimulator;
	public RendererMode rendererMode = RendererMode.OBJECTS;
	new FluidRenderer renderer;
	public FluidRenderer Renderer { get { return renderer; } }
	[SerializeField]
	FluidRendererObjects objectsRenderer;
	[SerializeField]
	FluidRendererTexture textureRenderer;

	public bool initializeOnStart = true;
	public string FamilyName { get; private set; }

	public enum SimulatorMode
	{
		CPU = 0,
		GPGPU
	}

	public enum RendererMode
	{
		OBJECTS = 0,
		TEXTURE
	}

	void Awake()
	{
		FamilyName = name.Substring(0, name.IndexOf("Dispatcher")).TrimEnd();
		gameObject.name = string.Format("{0} Dispatcher", FamilyName);

		// TODO genericify the enabling and destroying of mode options and move to arrays

		string nullReference = "Attempting to use {0} that does not exist.";
		switch (simulatorMode)
		{
			case SimulatorMode.CPU:
				simulator = cpuSimulator;
				if (cpuSimulator != null) { cpuSimulator.gameObject.SetActive(true); }
				else { throw new System.NullReferenceException(string.Format(nullReference, "CPU FluidSimulator")); }
				if (gpgpuSimulator != null) { Destroy(gpgpuSimulator.gameObject); }
				break;
			case SimulatorMode.GPGPU:
				simulator = gpgpuSimulator;
				if (gpgpuSimulator != null) { gpgpuSimulator.gameObject.SetActive(true); }
				else { throw new System.NullReferenceException(string.Format(nullReference, "GPGPU FluidSimulator")); }
				if (cpuSimulator != null) { Destroy(cpuSimulator.gameObject); }
				break;
		}

		switch (rendererMode)
		{
			case RendererMode.OBJECTS:
				renderer = objectsRenderer;
				if (objectsRenderer != null) { objectsRenderer.gameObject.SetActive(true); }
				else { throw new System.NullReferenceException(string.Format(nullReference, "Objects Renderer")); }
				if (textureRenderer != null) { Destroy(textureRenderer.gameObject); }
				break;
			case RendererMode.TEXTURE:
				renderer = textureRenderer;
				if (textureRenderer != null) { textureRenderer.gameObject.SetActive(true); }
				else { throw new System.NullReferenceException(string.Format(nullReference, "Texture Renderer")); }
				if (objectsRenderer != null) { Destroy(objectsRenderer.gameObject); }
				break;
		}
	}

	void Start()
	{
		if (initializeOnStart)
		{
			simulator.Initialize(FamilyName, renderer);
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
