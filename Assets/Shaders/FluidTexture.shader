Shader "Fluid/Texture"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TextureSize ("Texture Size", Vector) = (0, 0, 0, 0)
		_MaxDensity ("Max Density", Float) = 1
		_MaxSpeed ("Max Speed", Float) = 10
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
		//Tags { "Queue"="Transparent" "RenderType"="Transparent" }
		//Blend SrcAlpha OneMinusSrcAlpha
		LOD 100

		Pass
		{
			CGPROGRAM
			#define Z_STEP 1
			#define Y_STEP (Z_STEP * _TextureSize.z)
			#define X_STEP (Y_STEP * _TextureSize.y)
			#define INDEX(id) ((id.x * X_STEP) + (id.y * Y_STEP))// + (id.z * Z_STEP)

			#pragma vertex vert
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
				float2 pixelIndex: TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _TextureSize;
			StructuredBuffer<FluidCell> _FluidCells;
			float _MaxDensity;
			float _MaxSpeed;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.pixelIndex = v.uv * _TextureSize.xy;

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				//TODO Why is the y coordinate exactly 0.5 off?
				float2 pixelIndex = float2(i.uv.x, i.uv.y - 0.5) * _TextureSize.xy;

				FluidCell cell = _FluidCells[INDEX(float3(pixelIndex, 0))];
				float density = cell.density / _MaxDensity;
				float epsilon = 0.001;

				// TODO Profile the sqrt cost
				float speed = sqrt((cell.velocity.x * cell.velocity.x) + (cell.velocity.y * cell.velocity.y));
				float normalizedSpeed = speed + epsilon;
				float3 direction = cell.velocity / normalizedSpeed;
				normalizedSpeed = min(normalizedSpeed / (_MaxSpeed), 1);

				// Clip speed to zero when small enough.
				normalizedSpeed = normalizedSpeed - (normalizedSpeed * (speed < epsilon));
				float3 velocityColor = abs(direction * normalizedSpeed);
				float4 color = float4(velocityColor, density);
				return color;
			}
			ENDCG
		}
	}
}
