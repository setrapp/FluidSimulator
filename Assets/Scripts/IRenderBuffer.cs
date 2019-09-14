using UnityEngine;

public interface IRenderBuffer
{
    void RenderCells(ComputeBuffer fluidCells);
}
