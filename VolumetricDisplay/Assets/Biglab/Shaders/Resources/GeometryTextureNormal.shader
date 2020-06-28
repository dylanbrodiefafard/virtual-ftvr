Shader "Biglab/GeometryTexture/Normal" {
	SubShader {
		Tags { 
			"Queue"="Geometry" 
			"RenderType"="Opaque" 
			"IgnoreProjector"="True"
		}

		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 position : SV_POSITION;
				float3 vNormal  : TEXCOORD0;
			};

			uniform float4x4 _WorldToVolumeNormal;

			v2f vert(appdata v) {
				float3 w_normal = UnityObjectToWorldNormal(v.normal);

				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.vNormal = mul(_WorldToVolumeNormal, float4(w_normal, 0));
				return o;
			}

			float4 frag(v2f i) : SV_Target {
				float3 vNormal = normalize(i.vNormal);
				return float4(vNormal, 1.0f);
			}

			ENDCG
		}
	}
}
