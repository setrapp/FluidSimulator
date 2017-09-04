Shader "Fluid/Texture"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_TextureSize("Texture Size", Vector) = (0, 0, 0, 0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
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
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				o.pixelIndex = v.uv * _TextureSize.xy;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 color = float4(i.pixelIndex / _TextureSize.xy, 0, 1);
				return color;
			}
			ENDCG
		}
	}
}
