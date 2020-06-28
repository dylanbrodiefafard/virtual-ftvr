Shader "Biglab/Spheree/Projector" { // TODO: Move out of "Spheree"

	Properties {
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader{
		Tags { 
			"RenderType"="Opaque" 
		}

		LOD 100

		Pass{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile VIEWER _
			
			#include "UnityCG.cginc"
			#include "../Includes/Extra.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv     : TEXCOORD0;
			};

			struct v2f {
				float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
			};

			v2f vert (appdata v) {
				v2f o;
				o.vertex   = UnityObjectToClipPos(v.vertex);
				o.uv       = TRANSFORM_GEOMETRY_TEX(v.uv);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// Sample Projector Data
				float4 data = GetGeometryData(i.uv);
				float3 v_position = data.xyz;
				float  alphaMask = data.w;
                float3 v_normal = GetVolumeNormal(i.uv);

                // Transform normal from V ( volume ) to W ( world )
                float3 w_normal = VolumeToWorldNormal(v_normal);

                fixed4 color = CLIP_COLOR;

				// Compute the position of the surface of the sphere in world space ( V -> W )
				float4 w_position = VolumeToWorldPosition(v_position);
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