Shader "Biglab/Spheree/Virtual" { // TODO: Move out of "Spheree"

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader {
		Tags { 
            "Queue"="Transparent-1" 
			"RenderType"="Transparent" 
			"IgnoreProjector"="True"
		}

		LOD 100

		Pass {
			Blend One OneMinusSrcAlpha
			ZTest LEqual
			ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile VIEWER _

			#include "UnityCG.cginc"
			#include "../Includes/Extra.cginc"

			struct appdata {
				float4 vertex : POSITION;
                float3 normal : NORMAL;
				float2 uv     : TEXCOORD0;
			};

			struct v2f {
				float4 vertex   : SV_POSITION;
				float4 worldPos : POSITION1; // World space position
				float3 worldNor : NORMAL; // World space normal
				float2 uv       : TEXCOORD0;
			};
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex   = UnityObjectToClipPos(v.vertex);
				o.worldNor = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex); // Transform from V -> W ( vcam volume to world )
				o.uv       = TRANSFORM_GEOMETRY_TEX(v.uv);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// Sample Projector Data
				float4 data = GetGeometryData(i.uv);
				//float3 v_position = data.xyz;
				float  alphaMask = data.w;
                //float3 v_normal = GetVolumeNormal(i.uv);

                // Get the world normal from the vertex shader
                float3 w_normal = normalize(i.worldNor);

                fixed4 color = CLIP_COLOR;

				// Get the world position from the vertex shader
				float4 w_position = i.worldPos;
#ifdef VIEWER
                color = GetViewerColor(w_position, w_normal);
#else
				color  = GetFlatColor(w_position, w_normal);
#endif
				// Sample Overlay ( 2D Projection )
				// float4 overlay = GetOverlayColor(w_normal);

				return color * alphaMask;
			}
			ENDCG
		}
	}
}
