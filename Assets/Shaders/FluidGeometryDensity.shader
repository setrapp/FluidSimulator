Shader "Fluid/GeometryDensity"
{
    Properties
    {
        _EmptyColor ("Empty Color", Color) = (0, 0, 0, 0)
        _FullColor ("Full Color", Color) = (1, 1, 1, 1)
        _GridSize ("Grid Size", Vector) = (0, 0, 0, 0)
        _MaxDensity ("Max Density", Float) = 10
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100
        Cull Off //TODO only need this for velocity mesh

        Pass
        {
            CGPROGRAM
            //TODO These seem backwards (X should have the smallest step)
            //TODO It is backwards... advection is going in the wrong direction
            #define X_STEP 1
            #define Y_STEP (X_STEP * _GridSize.x)
            #define Z_STEP (Y_STEP * _GridSize.y)
            #define INDEX(id) ((id.x * X_STEP) + (id.y * Y_STEP))// + (id.z * Z_STEP

            #pragma vertex vert
            //#pragma geometry geom
            #pragma fragment frag
            
            #pragma target 4.5
            
            #include "UnityCG.cginc"

            struct FluidCell
            {
                float density;
                float3 velocity;
                float rawDivergence;
                float relaxedDivergence;
                float2 padding;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float density : POINT; //TODO Should these structures be 16byte aligned?
            };

            float4 _EmptyColor;
            float4 _FullColor;
            float4 _GridSize;
            //float _CellSize;
            StructuredBuffer<FluidCell> _FluidCells;
            float _MaxDensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Find cells that surround this vertex, disregarding border cells.
                float2 rawCellIndex = float2(o.uv.x, o.uv.y) * _GridSize.xy;
                int2 minCell = rawCellIndex - float2(0.5, 0.5);
                int2 maxCell = min(minCell + float2(1, 1), float2(_GridSize.x - 2, _GridSize.y - 2));
                minCell = max(minCell, float2(1, 1));
                
                FluidCell leftBottomCell = _FluidCells[INDEX(float3(minCell.x, minCell.y, 0))];
                FluidCell rightBottomCell = _FluidCells[INDEX(float3(maxCell.x, minCell.y, 0))];
                FluidCell leftTopCell = _FluidCells[INDEX(float3(minCell.x, maxCell.y, 0))];
                FluidCell rightTopCell = _FluidCells[INDEX(float3(maxCell.x, maxCell.y, 0))];
                float density = (leftBottomCell.density + rightBottomCell.density + leftTopCell.density + rightTopCell.density) / 4;

                o.density = density / _MaxDensity;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = lerp(_EmptyColor, _FullColor, i.density);
                return col;
            }
            ENDCG
        }
    }
}
