Shader "Fluid/GeometryVelocity"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _GridSize ("Grid Size", Vector) = (0, 0, 0, 0)
        _CellSize ("Cell Size", Float) = 1
        _MaxSpeed ("Max Speed", Float) = 10
        _VelocityWidth ("Velocity Width", Float) = 0.125
        _VelocityStartOffset ("Velocity Start Offset", Float) = 0
        _VelocityEndOffset ("Velocity End Offset", Float) = 2
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
            #define X_STEP 1
            #define Y_STEP (X_STEP * _GridSize.x)
            #define Z_STEP (Y_STEP * _GridSize.y)
            #define INDEX(id) ((id.x * X_STEP) + (id.y * Y_STEP))// + (id.z * Z_STEP

            #pragma vertex vert
            //#pragma geometry geom
            #pragma fragment frag
            
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
                float4 directionAndSpeed : COLOR; //TODO Should these structures be 16byte aligned?
            };

            float4 _Color;
            float4 _GridSize;
            float _CellSize;
            StructuredBuffer<FluidCell> _FluidCells;
            float _MaxSpeed;
            float _VelocityWidth;
            float _VelocityStartOffset;
            float _VelocityEndOffset;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                float epsilon = 0.001;

                // Find cells that surround this vertex, disregarding border cells.
                float2 rawCellIndex = float2(o.uv.x, o.uv.y) * _GridSize.xy;
                int2 minCell = rawCellIndex - float2(0.5, 0.5);
                int2 maxCell = min(minCell + float2(1, 1), float2(_GridSize.x - 2, _GridSize.y - 2));
                minCell = max(minCell, float2(1, 1));
                
                FluidCell leftBottomCell = _FluidCells[INDEX(float3(minCell.x, minCell.y, 0))];
                FluidCell rightBottomCell = _FluidCells[INDEX(float3(maxCell.x, minCell.y, 0))];
                FluidCell leftTopCell = _FluidCells[INDEX(float3(minCell.x, maxCell.y, 0))];
                FluidCell rightTopCell = _FluidCells[INDEX(float3(maxCell.x, maxCell.y, 0))];
                float3 velocity = (leftBottomCell.velocity + rightBottomCell.velocity + leftTopCell.velocity + rightTopCell.velocity) / 4;

                // TODO Profile the sqrt cost
                float speed = sqrt((velocity.x * velocity.x) + (velocity.y * velocity.y));
                float normalizedSpeed = speed + epsilon;
                float3 direction = velocity / normalizedSpeed;
                normalizedSpeed = min(normalizedSpeed / _MaxSpeed, 1);

                // Clip speed to zero when small enough.
                normalizedSpeed = normalizedSpeed - (normalizedSpeed * (speed < epsilon));

                o.directionAndSpeed = float4(direction, normalizedSpeed);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
            	float3 direction = abs(i.directionAndSpeed.xyz - float3(0.5, 0.5, 0.5));
            	float speed = i.directionAndSpeed.w;
                return float4(direction, speed);
            }
            ENDCG
        }
    }
}
