Shader "Custom/Underwater Shader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_ClippingRadius ("Clipping Radius", Float) = 6
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		Cull[_Cull]
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Test fullforwardshadows
		#include "UnityPBSLighting.cginc"
		#include "Noise.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 worldNormal;
			float3 worldPos;
			float3 viewDir;
			float Face : VFACE;
			INTERNAL_DATA
		};

		struct SurfaceOutputCustom
		{
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;
			fixed3 WorldPosition;
			fixed Alpha;
			fixed Metallic;
			fixed Smoothness;
		};

		half _ClippingRadius;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;
		 
		/* WATER EFFECT */

		float ComputeWaterEffect( float2 uv, float time )
		{
			// 
			float n1 = noise( float3( uv, time ), 2, 0.7 );
			float n2 = noise( float3( uv + float2( 0.1, 0.0 ), time * 2.0 ), 3, 0.5 );

			float n = abs( n1 ) + ( 1.0 - abs( n2 ) ) - 0.8;

			float waterEffect = max( 0.0, n );
			waterEffect = pow( waterEffect * 1.5, 1.5 );

			return waterEffect;
		}

		/* CUSTOM LIGHTING MODEL */

		inline SurfaceOutputStandard ToStandard( in SurfaceOutputCustom s )
		{
			SurfaceOutputStandard o;
			o.Albedo = s.Albedo;
			o.Alpha = s.Alpha;
			o.Normal = s.Normal;
			o.Emission = s.Emission;
			o.Metallic = s.Metallic;
			o.Smoothness = s.Smoothness;
			o.Occlusion = 1.0;

			return o;
		}

		inline half4 LightingTest( SurfaceOutputCustom s, half3 viewDir, UnityGI gi )
		{ 
			SurfaceOutputStandard o = ToStandard(s);

			half4 L = LightingStandard( o, viewDir, gi );
			 
			half ndotl = max( 0.0, dot( s.Normal, gi.light.dir ) );
			float effect1 = 0.2 * ComputeWaterEffect(s.WorldPosition.xz / 1.5, _Time.x * 25.0);
			float effect2 = 0.5 * ComputeWaterEffect(s.WorldPosition.xz / 3.5, -_Time.x * 15.0);
			float effect = ( effect1 + effect2 ) / 2.0;
			L.rgb += effect * gi.light.color * ndotl;

			return L;
		}

		inline void LightingTest_GI(
			SurfaceOutputCustom s,
			UnityGIInput data,
			inout UnityGI gi)
		{
			SurfaceOutputStandard o = ToStandard(s);
			LightingStandard_GI(o, data, gi);
		}

		/* SURFACE SHADER */

		void surf( Input IN, inout SurfaceOutputCustom o ) 
		{
			if( IN.Face == 0.0 )
				IN.worldNormal *= -1.0;

			//
			o.WorldPosition = IN.worldPos;
			if( length( o.WorldPosition ) > _ClippingRadius )
				discard;

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

			// Surface color
			o.Albedo = c.rgb;
			// o.Albedo = IN.worldNormal * 0.5 + 0.5;
			o.Alpha = c.a;

			// Metallic and smoothness
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

			if (c.a < 0.35) discard;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
