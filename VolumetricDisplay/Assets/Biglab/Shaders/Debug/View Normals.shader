Shader "Biglab/Unlit/View Normals" 
{
	Properties 
	{
	}

	SubShader
	{
		Tags{ "Queue" = "Geometry" "RenderType" = "Opaque" }
		Pass
		{	
			CGPROGRAM

			#include "UnityCG.cginc"
			#pragma fragment frag
			#pragma vertex vert
			
			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 vertex : TEXCOORD0;
				float3 normal : NORMAL;
			};

			vertexOutput vert(vertexInput v) {
				vertexOutput o;
				// Calculate vertex position in camera space
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
				o.vertex = v.vertex;
				return o;
			}

			float4 frag(vertexOutput i) : COLOR
			{
				float nx = i.normal.x * 0.5 + 0.5;
				float ny = i.normal.y * 0.5 + 0.5;
				float nz = i.normal.z * 0.5 + 0.5;
				return float4( nx, ny, nz, 1.0 );
			}

			ENDCG
		} 
	}
}