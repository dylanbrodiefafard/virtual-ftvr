Shader "Projector/Biglab" {
	Properties{
		_MainTex("Main Texture", 2D) = "white" {}
		_AlphaTex("Alpha Texture", 2D) = "white" {}
		[Toggle(OCCLUSION)]
		[PerRendererData] _Occlusion("Occlusion", Float) = 0
		[Toggle(INTRINSICS)]
		[PerRendererData] _Intrinsics("Intrinsics", Float) = 0
		[PerRendererData] _Luminosity("Luminosity", Float) = 1
	}

	Subshader{
		Tags {"RenderType" = "Transparent" "Queue" = "Transparent"}

		// Project the image
		Pass {
			Name "Projection"
			Blend DstColor Zero // Additive
			Offset -1, -1 // avoid depth fighting

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma shader_feature OCCLUSION
			#pragma shader_feature INTRINSICS
			#include "UnityCG.cginc"

			struct v2f {
				float4 position : SV_POSITION;
				float3 normal: NORMAL; // In world space
				float4 clipProjPos : TEXCOORD0; // Clip coordinates in projector space
				float4 projPos : TEXCOORD1;
				float distance : TEXCOORD2;
				float3 worldPos : TEXCOORD3;
			};

			struct VertexInput {
				float4 vertex : POSITION;
				fixed3 normal : NORMAL;
			};

			uniform float3 _WorldSpaceProjPos;
			uniform float4x4 _WorldToProjectorClip;
			uniform float4x4 _ProjectorToWorld;

			v2f vert(VertexInput v)
			{
				float4 worldPos = mul(unity_ObjectToWorld, v.vertex); // Object to world
				float3 worldNormal = UnityObjectToWorldNormal(v.normal); // Object to world
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex); // Object to Clip (Projector)
				o.normal = worldNormal;
				o.worldPos = worldPos.xyz;
				o.clipProjPos = mul(_WorldToProjectorClip, worldPos);
				o.projPos = ComputeNonStereoScreenPos(o.clipProjPos);
				o.distance = distance(_WorldSpaceProjPos, worldPos);
				return o;
			}

			inline bool isUVOutOfRange(float2 uv) { return (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1); }
			
			static const float FOUR_PI = 12.566370614359172953850573533118f;
			inline float computeBrightness(float L, float d){ return L / (FOUR_PI * d * d); } // See: inverse square law
			
			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _AlphaTex;
			float4 _AlphaTex_ST;

			float _Luminosity;
			uniform half4 _MainTex_TexelSize;
#ifdef OCCLUSION
			uniform sampler2D _OcclusionTex;
			
			// This is adapted from AutoLight.cginc
			inline bool ProjectorSampleShadow (sampler2D shadowMapTexture, float4 shadowCoord)
			{
				float3 coord = shadowCoord.xyz / shadowCoord.w;
				float occDepth = UNITY_SAMPLE_DEPTH (tex2D( shadowMapTexture, coord ));
				float fragDepth = coord.z;
				return (fragDepth < occDepth - 0.01f); // Empirically derived
			}
#endif
#ifdef INTRINSICS
			uniform float3 _RadialDistortion;
			uniform float2 _TangentialDistortion;
#endif


			// For detail see: https://docs.opencv.org/2.4/modules/calib3d/doc/camera_calibration_and_3d_reconstruction.html
			inline float2 applyDistortion(float2 clip, float k1, float k2, float k3, float p1, float p2)
			{
				// Compute intermediate values
				float xy = clip.x*clip.y; float xx = clip.x * clip.x; float yy = clip.y * clip.y;
				// Compute radii powers
				float r2 = xx + yy; float r4 = r2 * r2; float r6 = r4 * r2;

				// Compute radial portion
				float2 radial = clip * (1 + k1 * r2 + k2 * r4 + k3 * r6);
				// 6 term function - float2 radial = clip * ((1 + k1 * r2 + k2 * r4 + k3 * r6) / (1 + k4 * r2 + k5 * r4 + k6 * r6));

				// Compute tangential portion
				float tangential = float2(2 * p1 * xy + p2 * (r2 + 2 * xx),
									      2 * p2 * xy + p1 * (r2 + 2 * yy));

				// Combine them and return
				return radial + tangential;
			}


			half4 frag(v2f i) : SV_Target
			{
				if(i.clipProjPos.w < 0) return half4(1, 1, 1, 1); // Clip if behind projector
				float4 clipPos = i.clipProjPos / i.clipProjPos.w; // Do perspective-divide in the fragment shader

				i.normal = normalize(i.normal);
				float3 view = normalize(i.worldPos - _WorldSpaceProjPos);
				float a = dot(i.normal, view);
				// Clip if fragment is facing away from projector
				if (a > -0.075f) return half4(1, 1, 1, 1); // Emperically derived
				
				
#ifdef INTRINSICS
				clipPos.xy = applyDistortion(clipPos.xy, _RadialDistortion.x, _RadialDistortion.y, _RadialDistortion.z, _TangentialDistortion.x, _TangentialDistortion.y);
#endif

				float2 uvProj = ComputeNonStereoScreenPos(clipPos); // TODO: make sure this works
				if ( isUVOutOfRange(uvProj) ) return half4(1, 1, 1, 1); // Clip if the uv is outside the image

#ifdef OCCLUSION
				bool shadow = ProjectorSampleShadow(_OcclusionTex, i.projPos);
				if(shadow) return half4(1, 1, 1, 1); // Clip if this fragment is occluded by another
				
#endif
				// we're rendering with upside-down flipped projection, so flip the vertical UV coordinate too
				if (_ProjectionParams.x < 0) uvProj.y = 1 - uvProj.y;

				half4 color = tex2D(_MainTex, TRANSFORM_TEX(uvProj, _MainTex)); // Sample the viewpoint texture
				float alpha = tex2D(_AlphaTex, TRANSFORM_TEX(uvProj, _AlphaTex)).a; // Get overlay alpha 
				float brightness = computeBrightness(_Luminosity, i.distance); // Get apparent brightness

				return color * brightness * alpha;
			}

			ENDCG
		}
	}
}
