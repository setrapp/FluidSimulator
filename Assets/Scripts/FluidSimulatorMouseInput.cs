﻿using UnityEngine;
using System.Collections;

public class FluidSimulatorMouseInput : MonoBehaviour
{

    public FluidSimulator SelectedFluid { get; private set; }
    public GameObject depthTarget;

    [Header("External Additions")]
    public float densityChangeRate = 20;
    public float densityChangeRadius = 1;
    public float forceRate = 100;
    public float forceRadius = 1;
    //public bool alwaysAddVelocity = true;

    private Vector3 oldMousePos;
    private Vector3 mouseMove;

    void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + new Vector3(0, 0, depthTarget.transform.position.z));
        mouseMove = mousePosition - oldMousePos;
        oldMousePos = mousePosition;

        SelectedFluid = null;
        FluidCollider selectedCollider = null;

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            selectedCollider = hit.collider.GetComponent<FluidCollider>();
            if (selectedCollider != null)
            {
                SelectedFluid = selectedCollider.Simulator;
                //FluidCellIndex selectedIndex = new FluidCellIndex(selected.XIndex, selected.YIndex, 0);
                //SelectedFluid.SelectedCellIndex = selectedIndex;
                selectedCollider.SelectCell(hit.point);
            }
        }

        if (SelectedFluid != null)
        {
            bool leftMouse = Input.GetMouseButton(0);
            bool rightMouse = Input.GetMouseButton(1);
            bool middleMouse = Input.GetMouseButton(2);
            if (leftMouse || rightMouse || middleMouse)
            {
                float densityChange = 0;
                Vector3 force = Vector3.zero;

                if (leftMouse)
                {
                    densityChange += densityChangeRate;
                    force += mouseMove * forceRate;
                }
                if (rightMouse)
                {
                    densityChange -= densityChangeRate;
                    force += mouseMove * forceRate;
                }
                if (middleMouse && !leftMouse && !rightMouse)
                {
                    force += mouseMove * forceRate;
                }

                if (densityChange != 0 || force.sqrMagnitude > 0)
                {
                    //SelectedFluid.AddExternal(SelectedFluid.SelectedCellIndex, densityChange, densityChangeRadius, force, forceRadius);
                    //selectedCollider.AddExternal(hit.point, densityChange, densityChangeRadius, force, forceRadius);
                    FluidInputSustainerFactory.Instance.Create(selectedCollider, hit.point, densityChange, densityChangeRadius, force, forceRadius, 0.3f, 0f, 2f, 4f);
                }
            }
        }

    }
}
