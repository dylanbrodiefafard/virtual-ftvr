Shader "Biglab/GeometryTexture/Position" {
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
			};

			struct v2f {
				float4 position : SV_POSITION;
				float4 vPosition : TEXCOORD0;
			};

			uniform float4x4 _WorldToVolume;

			v2f vert(appdata v) {
				float4 w_position = mul(unity_ObjectToWorld, v.vertex);

				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);
				o.vPosition = mul(_WorldToVolume, w_position);
				return o;
			}

			float4 frag(v2f i) : SV_Target {
				return i.vPosition / i.vPosition.w;
			}

			ENDCG
		}
	}
}
