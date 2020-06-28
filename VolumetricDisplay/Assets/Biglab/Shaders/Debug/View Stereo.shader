Shader "Biglab/Unlit/View Stereo" 
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

			// Because unity_StereoEyeIndex let us down
			uniform int biglab_StereoEyeIndex;
			
			struct vertexInput {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct vertexOutput {
				UNITY_POSITION(pos);
				float4 vertex : TEXCOORD0;
				float3 normal : NORMAL;
				UNITY_VERTEX_OUTPUT_STEREO
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			vertexOutput vert(vertexInput v) {
				UNITY_SETUP_INSTANCE_ID(v);
				vertexOutput o;
				UNITY_INITIALIZE_OUTPUT(vertexOutput, o);
				UNITY_TRANSFER_INSTANCE_ID(v,o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				
				// Calculate vertex position in camera space
				o.pos = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
				o.vertex = v.vertex;
				return o;
			}

			float4 frag(vertexOutput i) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				
				float2 screenUV = i.pos.xy / _ScreenParams.xy;
				
				screenUV.x *= 0.5;
				if (biglab_StereoEyeIndex == 1) {
					screenUV.x += 0.5; 
				}

				return float4(screenUV,0,1); 
			}

			ENDCG
		}
	}
}