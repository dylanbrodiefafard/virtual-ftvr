Shader "Projector/Perspective" {

	Properties {
		_MainTex ("Main Texture", 2D) = "white" {}
	}

	SubShader {
		Tags {
			"RenderType"="Transparent" 
			"Queue"="Transparent"
		}

        LOD 100

		Pass {
			Name "Projection" // Is this needed?
			Blend One One // Additive blending mode
			Offset -1, -1 // To avoid depth fighting

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
            #pragma multi_compile VIEWER _

			#include "UnityCG.cginc"
            #include "../Includes/Extra.cginc"

            struct appdata {
				float4 vertex : POSITION;
				fixed3 normal : NORMAL;
			};

			struct v2f {
				float4 vertex   : SV_POSITION;
				float4 worldPos : POSITION1; // World space position
                float3 worldNor : NORMAL; // World space normal
			};

			v2f vert(appdata v) {
				v2f o;
				o.vertex   = UnityObjectToClipPos(v.vertex); // Object to Clip (Projector)
                o.worldNor = PhysicalToWorldNormal(v.normal); // Object to world
				o.worldPos = PhysicalToWorldPosition(v.vertex); // Object to world
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
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

				return color;
			}
			ENDCG
		}
	}
}