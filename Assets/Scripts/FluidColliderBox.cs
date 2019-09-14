using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
public class FluidColliderBox : FluidCollider
{
    Vector3 recentFocalPoint = Vector3.zero;
    FluidCellIndex recentFoundIndex;

    public override void SelectCell(Vector3 focalPoint)
    {

        Simulator.SelectedCellIndex = FindIndex(focalPoint);
    }

    public override void AddExternal(Vector3 focalPoint, float densityChange, float densityChangeRadius, Vector3 force, float forceRadius)
    {
        Simulator.AddExternal(FindIndex(focalPoint), densityChange, densityChangeRadius, force, forceRadius);
    }


    FluidCellIndex FindIndex(Vector3 focalPoint)
    {
        if (focalPoint == recentFocalPoint)
        {
            return recentFoundIndex;
        }

        Vector3 localFocus = transform.InverseTransformPoint(focalPoint);
        int gridSize = Simulator.fluidParameters.gridSize;
        recentFoundIndex = new FluidCellIndex((int)((localFocus.x + 0.5f) * gridSize), (int)((localFocus.y + 0.5f) * gridSize), 0);
        return recentFoundIndex;
    }
}
