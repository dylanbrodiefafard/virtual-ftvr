Shader "Biglab/SurfaceClipping"
{
	Properties{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}

		SubShader{
			Tags{ "RenderType" = "Opaque" "Queue" = "Geometry" }
			LOD 200
			Cull Off

			// ---- forward rendering base pass:
			Pass{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardBase" }

			CGPROGRAM
			// compile directives
			#pragma vertex vert_surf
			#pragma fragment frag_surf
			#pragma target 3.0
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase
			#include "HLSLSupport.cginc"
			#include "UnityShaderVariables.cginc"
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			#include "AutoLight.cginc"

			#define INTERNAL_DATA
			#define WorldReflectionVector(data,normal) data.worldRefl
			#define WorldNormalVector(data,normal) normal

			// Original surface shader snippet:
			#line 16 ""
			#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
			#endif
			/* UNITY: Original start of shader */
			//#pragma surface surf Standard fullforwardshadows exclude_path:deferred addshadow
			//#pragma target 3.0
			sampler2D _MainTex;

			struct Input {
				float2 uv_MainTex;
			};
			half _Glossiness;
			half _Metallic;
			fixed4 _Color;
			void surf(Input IN, inout SurfaceOutputStandard o) {
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				o.Albedo = c.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}


			// vertex-to-fragment interpolation data
			// no lightmaps:
		#ifndef LIGHTMAP_ON
		struct v2f_surf {
			float4 pos : SV_POSITION;
			float2 pack0 : TEXCOORD0; // _MainTex
			half3 worldNormal : TEXCOORD1;
			float3 worldPos : TEXCOORD2;
			#if UNITY_SHOULD_SAMPLE_SH
			half3 sh : TEXCOORD3; // SH
			#endif
			SHADOW_COORDS(4)
			UNITY_FOG_COORDS(5)
			#if SHADER_TARGET >= 30
			float4 lmap : TEXCOORD6;
			#endif
			float4 projPos : TEXCOORD7;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
		};
		#endif

		// with lightmaps:
			#ifdef LIGHTMAP_ON
			struct v2f_surf {
				float4 pos : SV_POSITION;
				float2 pack0 : TEXCOORD0; // _MainTex
				half3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				float4 lmap : TEXCOORD3;
				SHADOW_COORDS(4)
				UNITY_FOG_COORDS(5)
				#ifdef DIRLIGHTMAP_COMBINED
				fixed3 tSpace0 : TEXCOORD6;
				fixed3 tSpace1 : TEXCOORD7;
				fixed3 tSpace2 : TEXCOORD8;
				#endif
				float4 projPos : TEXCOORD9;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				};
			#endif

			float4 _MainTex_ST;

			// vertex shader
			v2f_surf vert_surf(appdata_full v) {
				UNITY_SETUP_INSTANCE_ID(v);
				v2f_surf o;
				UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
				UNITY_TRANSFER_INSTANCE_ID(v,o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
		#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
				fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
		#endif
		#if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
				o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
		#endif
				o.worldPos = worldPos;
				o.worldNormal = worldNormal;
		#ifdef DYNAMICLIGHTMAP_ON
				o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
		#endif
		#ifdef LIGHTMAP_ON
				o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
		#endif

				// SH/ambient and vertex lights
		#ifndef LIGHTMAP_ON
		#if UNITY_SHOULD_SAMPLE_SH
				o.sh = 0;
				// Approximated illumination from non-important point lights
		#ifdef VERTEXLIGHT_ON
				o.sh += Shade4PointLights(
					unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
					unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
					unity_4LightAtten0, worldPos, worldNormal);
		#endif
				o.sh = ShadeSHPerVertex(worldNormal, o.sh);
		#endif
		#endif // !LIGHTMAP_ON

				TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
				UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
				o.projPos = ComputeScreenPos(o.pos);
				return o;
			}

			uniform sampler2D _FrontDepthTex;
			uniform fixed4 _NearIntersectionColor;
			uniform float _NearIntersectionDistance;
			uniform int _NearIntersectionAnimationLines;
			uniform float _NearIntersectionAnimationSpeed;

			// fragment shader
			fixed4 frag_surf(v2f_surf IN) : SV_Target{
				UNITY_SETUP_INSTANCE_ID(IN);
			// prepare and unpack data
			Input surfIN;
			UNITY_INITIALIZE_OUTPUT(Input,surfIN);

			// Depth test
			float nearDepth = LinearEyeDepth(tex2Dproj(_FrontDepthTex, UNITY_PROJ_COORD(IN.projPos)).r);
			//Actual distance to the camera
			float depth = LinearEyeDepth(IN.projPos.z / IN.projPos.w);
			
			if (depth < nearDepth) discard;

			float t = 0;
			if(depth - nearDepth < _NearIntersectionDistance)
			{
				float y = (IN.projPos.y / IN.projPos.w);
				float s = sin(_Time.x);
				float d = sqrt((y - s) * (y - s));
				t = sin(d * _NearIntersectionAnimationLines + _NearIntersectionAnimationSpeed * _Time.x);
				t /= 2;
				t += 0.5;
				t *= _NearIntersectionColor.a;
			}

			surfIN.uv_MainTex.x = 1.0;
			surfIN.uv_MainTex = IN.pack0.xy;
			float3 worldPos = IN.worldPos;
		#ifndef USING_DIRECTIONAL_LIGHT
			fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
		#else
			fixed3 lightDir = _WorldSpaceLightPos0.xyz;
		#endif
			fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
		#ifdef UNITY_COMPILER_HLSL
			SurfaceOutputStandard o = (SurfaceOutputStandard)0;
		#else
			SurfaceOutputStandard o;
		#endif
			o.Albedo = 0.0;
			o.Emission = 0.0;
			o.Alpha = 0.0;
			o.Occlusion = 1.0;
			fixed3 normalWorldVertex = fixed3(0,0,1);
			o.Normal = IN.worldNormal;
			normalWorldVertex = IN.worldNormal;

			// call surface function
			surf(surfIN, o);

			// compute lighting & shadowing factor
			UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
				fixed4 c = 0;

			// Setup lighting environment
			UnityGI gi;
			UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
			gi.indirect.diffuse = 0;
			gi.indirect.specular = 0;
		#if !defined(LIGHTMAP_ON)
			gi.light.color = _LightColor0.rgb;
			gi.light.dir = lightDir;
		#endif
			// Call GI (lightmaps/SH/reflections) lighting function
			UnityGIInput giInput;
			UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
			giInput.light = gi.light;
			giInput.worldPos = worldPos;
			giInput.worldViewDir = worldViewDir;
			giInput.atten = atten;
		#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			giInput.lightmapUV = IN.lmap;
		#else
			giInput.lightmapUV = 0.0;
		#endif
		

		#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
			giInput.ambient.rgb = 0.0;
		#else
		#if UNITY_SHOULD_SAMPLE_SH
			giInput.ambient = IN.sh;
		#else
			giInput.ambient.rgb = 0.0;
#endif
		#endif
			giInput.probeHDR[0] = unity_SpecCube0_HDR;
			giInput.probeHDR[1] = unity_SpecCube1_HDR;
		#if UNITY_SPECCUBE_BLENDING || UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
		#endif
		#if UNITY_SPECCUBE_BOX_PROJECTION
			giInput.boxMax[0] = unity_SpecCube0_BoxMax;
			giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
			giInput.boxMax[1] = unity_SpecCube1_BoxMax;
			giInput.boxMin[1] = unity_SpecCube1_BoxMin;
			giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
		#endif
			LightingStandard_GI(o, giInput, gi);

			// realtime lighting: call lighting function
			c += LightingStandard(o, worldViewDir, gi);
			c = (1-t)*c + t*_NearIntersectionColor; // Linear blend between the colors.
			UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
			UNITY_OPAQUE_ALPHA(c.a);
			return c;
		}

		ENDCG

		}

			// ---- forward rendering additive lights pass:
			Pass{
			Name "FORWARD"
			Tags{ "LightMode" = "ForwardAdd" }
			ZWrite Off Blend One One

			CGPROGRAM
			// compile directives
	#pragma vertex vert_surf
	#pragma fragment frag_surf
	#pragma target 3.0
	#pragma multi_compile_fog
	#pragma multi_compile_fwdadd_fullshadows
	#pragma skip_variants INSTANCING_ON
	#include "HLSLSupport.cginc"
	#include "UnityShaderVariables.cginc"
			// Surface shader code generated based on:
			// writes to per-pixel normal: no
			// writes to emission: no
			// writes to occlusion: no
			// needs world space reflection vector: no
			// needs world space normal vector: no
			// needs screen space position: no
			// needs world space position: no
			// needs view direction: no
			// needs world space view direction: no
			// needs world space position for lighting: YES
			// needs world space view direction for lighting: YES
			// needs world space view direction for lightmaps: no
			// needs vertex color: no
			// needs VFACE: no
			// passes tangent-to-world matrix to pixel shader: no
			// reads from normal: no
			// 1 texcoords actually used
			//   float2 _MainTex
	#define UNITY_PASS_FORWARDADD
	#include "UnityCG.cginc"
	#include "Lighting.cginc"
	#include "UnityPBSLighting.cginc"
	#include "AutoLight.cginc"

	#define INTERNAL_DATA
	#define WorldReflectionVector(data,normal) data.worldRefl
	#define WorldNormalVector(data,normal) normal

			// Original surface shader snippet:
	#line 16 ""
	#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
	#endif
			/* UNITY: Original start of shader */
			//#pragma surface surf Standard fullforwardshadows exclude_path:deferred addshadow
			//#pragma target 3.0
			sampler2D _MainTex;
		struct Input {
			float2 uv_MainTex;
		};
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}


		// vertex-to-fragment interpolation data
		struct v2f_surf {
			float4 pos : SV_POSITION;
			float2 pack0 : TEXCOORD0; // _MainTex
			half3 worldNormal : TEXCOORD1;
			float3 worldPos : TEXCOORD2;
			SHADOW_COORDS(3)
				UNITY_FOG_COORDS(4)
				float4 projPos : TEXCOORD5;
		};
		float4 _MainTex_ST;

		// vertex shader
		v2f_surf vert_surf(appdata_full v) {
			v2f_surf o;
			UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
			o.pos = UnityObjectToClipPos(v.vertex);
			o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
			o.worldPos = worldPos;
			o.worldNormal = worldNormal;

			TRANSFER_SHADOW(o); // pass shadow coordinates to pixel shader
			UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
			UNITY_TRANSFER_DEPTH(o.depth);
			o.projPos = ComputeScreenPos(o.pos);
			return o;
		}

		uniform sampler2D _FrontDepthTex;
		// fragment shader
		fixed4 frag_surf(v2f_surf IN) : SV_Target{
			// prepare and unpack data
			Input surfIN;
		UNITY_INITIALIZE_OUTPUT(Input,surfIN);

		surfIN.uv_MainTex.x = 1.0;
		surfIN.uv_MainTex = IN.pack0.xy;
		float3 worldPos = IN.worldPos;

		// Depth test
		float nearDepth = LinearEyeDepth(tex2Dproj(_FrontDepthTex, UNITY_PROJ_COORD(IN.projPos)).r);
		//Actual distance to the camera
		float depth = LinearEyeDepth(IN.projPos.z / IN.projPos.w);

		if (depth < nearDepth) discard;
	#ifndef USING_DIRECTIONAL_LIGHT
		fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
	#else
		fixed3 lightDir = _WorldSpaceLightPos0.xyz;
	#endif
		fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
	#ifdef UNITY_COMPILER_HLSL
		SurfaceOutputStandard o = (SurfaceOutputStandard)0;
	#else
		SurfaceOutputStandard o;
	#endif
		o.Albedo = 0.0;
		o.Emission = 0.0;
		o.Alpha = 0.0;
		o.Occlusion = 1.0;
		fixed3 normalWorldVertex = fixed3(0,0,1);
		o.Normal = IN.worldNormal;
		normalWorldVertex = IN.worldNormal;

		// call surface function
		surf(surfIN, o);
		UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
			fixed4 c = 0;

		// Setup lighting environment
		UnityGI gi;
		UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
		gi.indirect.diffuse = 0;
		gi.indirect.specular = 0;
	#if !defined(LIGHTMAP_ON)
		gi.light.color = _LightColor0.rgb;
		gi.light.dir = lightDir;
	#endif
		gi.light.color *= atten;
		c += LightingStandard(o, worldViewDir, gi);
		c.a = 0.0;
		UNITY_APPLY_FOG(IN.fogCoord, c); // apply fog
		UNITY_OPAQUE_ALPHA(c.a);
		return c;
		}

			ENDCG

		}

			// ---- shadow caster pass:
			Pass{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On ZTest LEqual

			CGPROGRAM
			// compile directives
	#pragma vertex vert_surf
	#pragma fragment frag_surf
	#pragma target 3.0
	#pragma multi_compile_shadowcaster
	#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
	#include "HLSLSupport.cginc"
	#include "UnityShaderVariables.cginc"
			// Surface shader code generated based on:
			// writes to per-pixel normal: no
			// writes to emission: no
			// writes to occlusion: no
			// needs world space reflection vector: no
			// needs world space normal vector: no
			// needs screen space position: no
			// needs world space position: no
			// needs view direction: no
			// needs world space view direction: no
			// needs world space position for lighting: YES
			// needs world space view direction for lighting: YES
			// needs world space view direction for lightmaps: no
			// needs vertex color: no
			// needs VFACE: no
			// passes tangent-to-world matrix to pixel shader: no
			// reads from normal: no
			// 0 texcoords actually used
	#define UNITY_PASS_SHADOWCASTER
	#include "UnityCG.cginc"
	#include "Lighting.cginc"
	#include "UnityPBSLighting.cginc"

	#define INTERNAL_DATA
	#define WorldReflectionVector(data,normal) data.worldRefl
	#define WorldNormalVector(data,normal) normal

			// Original surface shader snippet:
	#line 16 ""
	#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
	#endif
			/* UNITY: Original start of shader */
			//#pragma surface surf Standard fullforwardshadows exclude_path:deferred addshadow
			//#pragma target 3.0
			sampler2D _MainTex;
		struct Input {
			float2 uv_MainTex;
		};
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}


		// vertex-to-fragment interpolation data
		struct v2f_surf {
			V2F_SHADOW_CASTER;
			float3 worldPos : TEXCOORD1;
			UNITY_VERTEX_INPUT_INSTANCE_ID
			UNITY_VERTEX_OUTPUT_STEREO
			float4 projPos : TEXCOORD4;
		};

		uniform float4x4 _CaptureWorldToClipMono;
		// vertex shader
		v2f_surf vert_surf(appdata_full v) {
			UNITY_SETUP_INSTANCE_ID(v);
			v2f_surf o;
			UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
			UNITY_TRANSFER_INSTANCE_ID(v,o);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
			o.worldPos = worldPos;
			TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)

			float4 clipPos = mul(_CaptureWorldToClipMono, float4(worldPos, 1));
			//float4 clipPos = UnityObjectToClipPos(v.vertex);
			o.projPos = ComputeScreenPos(clipPos);
			return o;
		}

		uniform sampler2D _FrontDepthTex;
		// fragment shader
		fixed4 frag_surf(v2f_surf IN) : SV_Target{
			UNITY_SETUP_INSTANCE_ID(IN);
		// prepare and unpack data
		Input surfIN;
		UNITY_INITIALIZE_OUTPUT(Input,surfIN);
		surfIN.uv_MainTex.x = 1.0;
		float3 worldPos = IN.worldPos;

		// Depth test
		float nearDepth = LinearEyeDepth(tex2Dproj(_FrontDepthTex, UNITY_PROJ_COORD(IN.projPos)).r);
		//Actual distance to the camera
		float depth = LinearEyeDepth(IN.projPos.z / IN.projPos.w);

		if (depth < nearDepth) discard;
	#ifndef USING_DIRECTIONAL_LIGHT
		fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
	#else
		fixed3 lightDir = _WorldSpaceLightPos0.xyz;
	#endif
	#ifdef UNITY_COMPILER_HLSL
		SurfaceOutputStandard o = (SurfaceOutputStandard)0;
	#else
		SurfaceOutputStandard o;
	#endif
		o.Albedo = 0.0;
		o.Emission = 0.0;
		o.Alpha = 0.0;
		o.Occlusion = 1.0;
		fixed3 normalWorldVertex = fixed3(0,0,1);

		// call surface function
		surf(surfIN, o);
		SHADOW_CASTER_FRAGMENT(IN)
		}

			ENDCG

		}

			// ---- meta information extraction pass:
			Pass{
			Name "Meta"
			Tags{ "LightMode" = "Meta" }
			Cull Off

			CGPROGRAM
			// compile directives
	#pragma vertex vert_surf
	#pragma fragment frag_surf
	#pragma target 3.0
	#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
	#pragma skip_variants INSTANCING_ON
	#include "HLSLSupport.cginc"
	#include "UnityShaderVariables.cginc"
			// Surface shader code generated based on:
			// writes to per-pixel normal: no
			// writes to emission: no
			// writes to occlusion: no
			// needs world space reflection vector: no
			// needs world space normal vector: no
			// needs screen space position: no
			// needs world space position: no
			// needs view direction: no
			// needs world space view direction: no
			// needs world space position for lighting: YES
			// needs world space view direction for lighting: YES
			// needs world space view direction for lightmaps: no
			// needs vertex color: no
			// needs VFACE: no
			// passes tangent-to-world matrix to pixel shader: no
			// reads from normal: no
			// 1 texcoords actually used
			//   float2 _MainTex
	#define UNITY_PASS_META
	#include "UnityCG.cginc"
	#include "Lighting.cginc"
	#include "UnityPBSLighting.cginc"

	#define INTERNAL_DATA
	#define WorldReflectionVector(data,normal) data.worldRefl
	#define WorldNormalVector(data,normal) normal

			// Original surface shader snippet:
	#line 16 ""
	#ifdef DUMMY_PREPROCESSOR_TO_WORK_AROUND_HLSL_COMPILER_LINE_HANDLING
	#endif
			/* UNITY: Original start of shader */
			//#pragma surface surf Standard fullforwardshadows exclude_path:deferred addshadow
			//#pragma target 3.0
			sampler2D _MainTex;
		struct Input {
			float2 uv_MainTex;
		};
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}

	#include "UnityMetaPass.cginc"

		// vertex-to-fragment interpolation data
		struct v2f_surf {
			float4 pos : SV_POSITION;
			float2 pack0 : TEXCOORD0; // _MainTex
			float3 worldPos : TEXCOORD1;
			float4 projPos : TEXCOORD2;
		};
		float4 _MainTex_ST;

		// vertex shader
		v2f_surf vert_surf(appdata_full v) {
			v2f_surf o;
			UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
			o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
			o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			fixed3 worldNormal = UnityObjectToWorldNormal(v.normal);
			o.worldPos = worldPos;
			o.projPos = ComputeScreenPos(o.pos);
			return o;
		}

		uniform sampler2D _FrontDepthTex;
		// fragment shader
		fixed4 frag_surf(v2f_surf IN) : SV_Target{
			// prepare and unpack data
			Input surfIN;
		UNITY_INITIALIZE_OUTPUT(Input,surfIN);
		surfIN.uv_MainTex.x = 1.0;
		surfIN.uv_MainTex = IN.pack0.xy;
		float3 worldPos = IN.worldPos;

		// Depth test
		float nearDepth = LinearEyeDepth(tex2Dproj(_FrontDepthTex, UNITY_PROJ_COORD(IN.projPos)).r);
		//Actual distance to the camera
		float depth = LinearEyeDepth(IN.projPos.z / IN.projPos.w);

		if (depth < nearDepth) discard;
	#ifndef USING_DIRECTIONAL_LIGHT
		fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
	#else
		fixed3 lightDir = _WorldSpaceLightPos0.xyz;
	#endif
	#ifdef UNITY_COMPILER_HLSL
		SurfaceOutputStandard o = (SurfaceOutputStandard)0;
	#else
		SurfaceOutputStandard o;
	#endif
		o.Albedo = 0.0;
		o.Emission = 0.0;
		o.Alpha = 0.0;
		o.Occlusion = 1.0;
		fixed3 normalWorldVertex = fixed3(0,0,1);

		// call surface function
		surf(surfIN, o);
		UnityMetaInput metaIN;
		UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
		metaIN.Albedo = o.Albedo;
		metaIN.Emission = o.Emission;
		return UnityMetaFragment(metaIN);
		}

			ENDCG

		}

			// ---- end of surface shader generated code

			#LINE 35

		}

			SubShader{
				Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
				Blend SrcAlpha One
				ColorMask RGB
				Cull Off Lighting Off ZWrite Off
				Pass{

					CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma target 2.0
					#pragma multi_compile_particles
					#pragma multi_compile_fog

					#include "UnityCG.cginc"

					sampler2D _MainTex;
					fixed4 _TintColor;

					struct appdata_t {
						float4 vertex : POSITION;
						fixed4 color : COLOR;
						float2 texcoord : TEXCOORD0;
						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					struct v2f {
						float4 vertex : SV_POSITION;
						fixed4 color : COLOR;
						float2 texcoord : TEXCOORD0;
						UNITY_FOG_COORDS(1)
						#ifdef SOFTPARTICLES_ON
							float4 projPos : TEXCOORD2;
						#endif
						UNITY_VERTEX_OUTPUT_STEREO
					};

				float4 _MainTex_ST;

				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					o.vertex = UnityObjectToClipPos(v.vertex);
			#ifdef SOFTPARTICLES_ON
					o.projPos = ComputeScreenPos(o.vertex);
					COMPUTE_EYEDEPTH(o.projPos.z);
			#endif
					o.color = v.color;
					o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
					UNITY_TRANSFER_FOG(o,o.vertex);
					return o;
				}

				sampler2D_float _CameraDepthTexture;
				float _InvFade;

				fixed4 frag(v2f i) : SV_Target
				{
			#ifdef SOFTPARTICLES_ON
					float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
				float partZ = i.projPos.z;
				float fade = saturate(_InvFade * (sceneZ - partZ));
				i.color.a *= fade;
			#endif

				fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
				UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0)); // fog towards black due to our blend mode
				return col;
				}
					ENDCG
				}
		}
			FallBack "Diffuse"
}