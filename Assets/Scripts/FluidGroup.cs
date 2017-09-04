﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class FluidGroup : MonoBehaviour
{
	[Serializable]
	public class ShareInfoFlags
	{
		public bool fluidParameters = true;
		public bool cellParameters = true;
		public bool operationParameters = true;
		public bool operationFlags = true;
		public bool visualizationFlags = true;
	}

	[SerializeField]
	List<FluidDispatcher> members = null;
	//[SerializeField]
	//ShareInfoFlags shareInfoFlags = null;
	[SerializeField]
	FluidSimulator.FluidSimulatorInfo sharedInfo = null;
	[SerializeField]
	FluidSimulatorMouseInput mouseInput = null;

	FluidCell[,] sharedExternals;
	
	void Awake()
	{
		foreach(var member in members)
		{
			member.initializeOnStart = false;
		}
	}

	void Start()
	{
		sharedExternals = new FluidCell[sharedInfo.fluidParameters.gridSize, sharedInfo.fluidParameters.gridSize];

		foreach(var member in members)
		{
			ShareInfo(member.Simulator);
		}

		foreach(var member in members)
		{
			member.Simulator.Initialize(member.FamilyName, member.Renderer);
			member.Simulator.autoSimulate = false;
		}
	}

	void Update()
	{
		if (mouseInput != null && mouseInput.SelectedFluid != null)
		{
			FluidCellIndex selectedIndex = mouseInput.SelectedFluid.SelectedCellIndex;
			foreach (var member in members)
			{
				if (member.Simulator != mouseInput.SelectedFluid)
				{
					member.Simulator.SelectedCellIndex = selectedIndex;
				}
			}
		}

		for(int i = 0; i < sharedInfo.fluidParameters.gridSize; i++)
		{
			for(int j = 0; j < sharedInfo.fluidParameters.gridSize; j++)
			{
				sharedExternals[i, j].density = 0;
				sharedExternals[i, j].velocity = Vector3.zero;
			}
		}

		// Sum up external additions of all member simulators.
		FluidCellIndex index = new FluidCellIndex();
		foreach (var member in members)
		{
			for (int i = 0; i < sharedInfo.fluidParameters.gridSize; i++)
			{
				for (int j = 0; j < sharedInfo.fluidParameters.gridSize; j++)
				{
					index.x = i;
					index.y = j;
					index.z = 0;
					FluidCell memberCell = member.Simulator.GetExternal(index);
					sharedExternals[i, j].density += memberCell.density;
					sharedExternals[i, j].velocity += memberCell.velocity;
				}
			}
		}

		// Apply summed externals to member simulators to ensure identical input.
		foreach (var member in members)
		{
			for (int i = 0; i < sharedInfo.fluidParameters.gridSize; i++)
			{
				for (int j = 0; j < sharedInfo.fluidParameters.gridSize; j++)
				{
					index.x = i;
					index.y = j;
					index.z = 0;
					member.Simulator.SetExternal(index, sharedExternals[i,j]);
				}
			}
		}

		foreach (var member in members)
		{
			member.Simulator.Simulate();
		}
	}

	// TODO Is there a more programatic way of doing this (Type.GetMembers)?
	void ShareInfo(FluidSimulator simulator)
	{
		simulator.info = sharedInfo;
		/*
		if (shareInfoFlags.fluidParameters)			{ simulator.info.fluidParameters = sharedInfo.fluidParameters; }
		if (shareInfoFlags.cellParameters)			{ simulator.info.cellParameters = sharedInfo.cellParameters; }
		if (shareInfoFlags.operationParameters)		{ simulator.info.operationParameters = sharedInfo.operationParameters; }
		if (shareInfoFlags.operationFlags)			{ simulator.info.operationFlags = sharedInfo.operationFlags; }
		if (shareInfoFlags.visualizationFlags)		{ simulator.info.visualizationFlags = sharedInfo.visualizationFlags; }
		*/
	}
}
