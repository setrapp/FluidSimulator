Shader "Fluid/Geometry"
{
	Properties
	{
		_GridSize ("Grid Size", Vector) = (0, 0, 0, 0)
		_CellSize ("Cell Size", Float) = 1
		_MaxDensity ("Max Density", Float) = 1
		_MaxSpeed ("Max Speed", Float) = 10
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM
			#define Z_STEP 1
			#define Y_STEP (Z_STEP * _GridSize.z)
			#define X_STEP (Y_STEP * _GridSize.y)
			#define INDEX(id) ((id.x * X_STEP) + (id.y * Y_STEP))// + (id.z * Z_STEP

			#pragma vertex vert
			#pragma geometry geom
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
				float4 directionAndSpeed : COLOR;
				float density : POINT; //TODO Should these structures be 16byte aligned.
			};

			float4 _GridSize;
			float _CellSize;
			StructuredBuffer<FluidCell> _FluidCells;
			float _MaxDensity;
			float _MaxSpeed;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = v.vertex;
				o.uv = v.uv;

				float2 cellIndex = float2(o.uv.x, o.uv.y) * _GridSize.xy;
				float epsilon = 0.001;

				FluidCell cell = _FluidCells[INDEX(float3(cellIndex, 0))];
				o.density = cell.density / _MaxDensity;

				// TODO Profile the sqrt cost
				float speed = sqrt((cell.velocity.x * cell.velocity.x) + (cell.velocity.y * cell.velocity.y));
				float normalizedSpeed = speed + epsilon;
				float3 direction = cell.velocity / normalizedSpeed;
				normalizedSpeed = min(normalizedSpeed / (_MaxSpeed), 1);

				// Clip speed to zero when small enough.
				normalizedSpeed = normalizedSpeed - (normalizedSpeed * (speed < epsilon));

				o.directionAndSpeed = float4(direction, normalizedSpeed);

				return o;
			}

			[maxvertexcount(4)]
			void geom(point v2f input[1], inout TriangleStream<v2f> triStream)
			{
				v2f center = input[0];
				float offset = _CellSize;

				v2f leftDown, rightDown, leftUp, rightUp;

				// TODO May want to interpolate with adjacent vertices to smooth out cells

				leftDown.vertex = center.vertex + float4(-offset, -offset, 0, 1);
				leftDown.vertex = UnityObjectToClipPos(leftDown.vertex);
				leftDown.uv = center.vertex;
				leftDown.directionAndSpeed = center.directionAndSpeed;
				leftDown.density = center.density;

				leftUp.vertex = center.vertex + float4(-offset, offset, 0, 1);
				leftUp.vertex = UnityObjectToClipPos(leftUp.vertex);
				leftUp.uv = center.vertex;
				leftUp.directionAndSpeed = center.directionAndSpeed;
				leftUp.density = center.density;

				rightDown.vertex = center.vertex + float4(offset, -offset, 0, 1);
				rightDown.vertex = UnityObjectToClipPos(rightDown.vertex);
				rightDown.uv = center.vertex;
				rightDown.directionAndSpeed = center.directionAndSpeed;
				rightDown.density = center.density;

				rightUp.vertex = center.vertex + float4(offset, offset, 0, 1);
				rightUp.vertex = UnityObjectToClipPos(rightUp.vertex);
				rightUp.uv = center.vertex;
				rightUp.directionAndSpeed = center.directionAndSpeed;
				rightUp.density = center.density;

				triStream.Append(leftDown);
				triStream.Append(leftUp);
				triStream.Append(rightDown);
				triStream.Append(rightUp);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 velocityColor = i.directionAndSpeed.rgb * i.directionAndSpeed.a;
				float4 col = float4(velocityColor, i.density);
				return col;
			}
			ENDCG
		}
	}
}
