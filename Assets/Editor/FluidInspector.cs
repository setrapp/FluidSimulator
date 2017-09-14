using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(FluidSimulator), true)]
public class FluidInspector : Editor
{
	bool showSelected = true;
	bool showLeft = false;
	bool showRight = false;
	bool showDown = false;
	bool showUp = false;

	public override void OnInspectorGUI()
	{
		FluidSimulator simulator = (FluidSimulator)target;

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset"))
		{
			simulator.resetFluid = true;
		}
		if (GUILayout.Button(!simulator.pauseFluid ? "Pause" : "Resume"))
		{
			simulator.pauseFluid = !simulator.pauseFluid;
		}
		if (GUILayout.Button("Step"))
		{
			simulator.stepFluid = true;
		}
		GUILayout.EndHorizontal();

		EditorGUILayout.LabelField("Family", simulator.FamilyName);

		// Default Inspector Rendering.
		base.OnInspectorGUI();

		if (Application.isPlaying && simulator.isActiveAndEnabled)
		{
			EditorGUILayout.LabelField("Global Density", "" + simulator.GlobalDensity);

			// TODO Display cell and cell data in a layout
			//EditorGUILayout.LabelField("Selected Cell", simulator.SelectedCell.ToString());
			EditorGUILayout.Space();
			GUILayout.Label("Cell Data", EditorStyles.boldLabel);
			FluidCellIndex selectedIndex = simulator.SelectedCellIndex;
			FluidCellOperationData operationData = simulator.GetCellOperationData(selectedIndex);
			ShowCellData("Selected", simulator, selectedIndex, ref showSelected, true);
			ShowCellData("Left", simulator, operationData.leftId, ref showLeft, false);
			ShowCellData("Right", simulator, operationData.rightId, ref showRight, false);
			ShowCellData("Down", simulator, operationData.downId, ref showDown, false);
			ShowCellData("Up", simulator, operationData.upId, ref showUp, false);
		}
	}

	void ShowCellData(string cellName, FluidSimulator simulator, FluidCellIndex cellIndex, ref bool showCell, bool showOperationData)
	{
		return;
		int gridSize = simulator.info.fluidParameters.gridSize;
		if (cellIndex.x >= 0 && cellIndex.y >= 0 && cellIndex.z >= 0 &&
			cellIndex.x < gridSize && cellIndex.y < gridSize && cellIndex.z < gridSize)
		{
			FluidCell cell = simulator.GetCell(cellIndex);

			showCell = EditorGUILayout.Foldout(showCell, string.Format("{0} {1}", cellName, cellIndex.ToString()));
			if (showCell)
			{
				EditorGUILayout.LabelField("Density", "" + cell.density);
				EditorGUILayout.LabelField("Velocity", "" + cell.velocity);
				EditorGUILayout.LabelField("Raw Divergence", "" + cell.rawDivergence);
				EditorGUILayout.LabelField("Relaxed Divergence", "" + cell.relaxedDivergence);
				if (showOperationData)
				{
					FluidCellOperationData operationData = simulator.GetCellOperationData(cellIndex);
					EditorGUILayout.LabelField("Advect Index Velocity", "" + operationData.advectIdVelocity.ToString());
					EditorGUILayout.LabelField("Advect Past Index", "" + operationData.advectPastId.ToString());
					EditorGUILayout.LabelField("Advect SamplePercentages", "" + operationData.advectSamplePercentages);
				}
				EditorGUILayout.Space();
			}
		}
	}
}
