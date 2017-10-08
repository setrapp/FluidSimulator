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
				o.vertex = v.vertex;
				o.uv = v.uv;

				float2 cellIndex = float2(o.uv.x, o.uv.y) * _GridSize.xy;
				float epsilon = 0.001;

				FluidCell cell = _FluidCells[INDEX(float3(cellIndex, 0))];

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
				//float velocityWidth = offset / 8; // TODO pass this in from script.

				v2f velStartDown, velStartUp, velEndDown, velEndUp;
				// TODO 3D will require some cross products to get orthonormal basis.
				float3 length = center.directionAndSpeed.xyz * offset;
				float3 width = center.directionAndSpeed.yxz * (offset * max(_VelocityWidth, 0));
				width.x = -width.x;
				float3 startOffset = -length * max(_VelocityStartOffset, 0);
				float3 endOffset = length * max(_VelocityEndOffset, 0);

				velStartDown.vertex = center.vertex + float4(startOffset - width, 0);
				velStartDown.vertex = UnityObjectToClipPos(velStartDown.vertex);
				velStartDown.uv = center.vertex;
				velStartDown.directionAndSpeed = center.directionAndSpeed;

				velStartUp.vertex = center.vertex + float4(startOffset + width, 0);
				velStartUp.vertex = UnityObjectToClipPos(velStartUp.vertex);
				velStartUp.uv = center.vertex;
				velStartUp.directionAndSpeed = center.directionAndSpeed;

				velEndDown.vertex = center.vertex + float4(endOffset - width, 0);
				velEndDown.vertex = UnityObjectToClipPos(velEndDown.vertex);
				velEndDown.uv = center.vertex;
				velEndDown.directionAndSpeed = center.directionAndSpeed;

				velEndUp.vertex = center.vertex + float4(endOffset + width, 0);
				velEndUp.vertex = UnityObjectToClipPos(velEndUp.vertex);
				velEndUp.uv = center.vertex;
				velEndUp.directionAndSpeed = center.directionAndSpeed;

				triStream.Append(velStartDown);
				triStream.Append(velStartUp);
				triStream.Append(velEndDown);
				triStream.Append(velEndUp);
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return _Color;
			}
			ENDCG
		}
	}
}
