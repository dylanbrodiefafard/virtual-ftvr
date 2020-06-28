// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Glass Shader" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Fresnel("Fresnel Transparency", Range(0,10)) = 3.0
		[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 0
	}
	SubShader {
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Cull[_Cull]
		LOD 200
		
		Pass {
			ZWrite Off
			ColorMask 0
   
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			fixed4 _Color;
 
			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};
 
			v2f vert (appdata_base v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (v.vertex);
				o.uv = v.texcoord.xy;
				return o;
			}
 
			half4 frag (v2f i) : COLOR
			{
				fixed4 c = tex2D(_MainTex, i.uv) * _Color;
				// if( c.a < 0.5 ) discard;
				return half4(0,0,0,0);
			}
			ENDCG  
		}

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows alpha:fade
		#include "Noise.cginc"

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
			float3 viewDir;
			float3 worldNormal;
			INTERNAL_DATA
		};

		half _Fresnel;
		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o) {

			float incidence = 1.0 - max( 0.0, dot(IN.viewDir, IN.worldNormal) );

			// Algae effect on glass
			float algae = noise(IN.uv_MainTex * 100.0, 4, 0.7) * 0.5 + 0.5;
			float tilt = 1.0 - max( 0.0, IN.worldNormal.y * 0.5 + 0.5 );
			algae *= tilt;

			// if (algae > 0.3) algae = 1.0;
			algae = algae * 0.4 + smoothstep( 0.31, 0.35, algae ) * 0.6;

			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Alpha = saturate(algae) + min(0.0, IN.worldNormal.y);

			// Albedo comes from a texture tinted by color
			// o.Alpha *= c.a * pow(incidence, _Fresnel);
			o.Albedo = c.rgb;

			// Metallic and smoothness come from slider variables
			o.Smoothness = _Glossiness * (1.0 - algae);
			o.Metallic = _Metallic;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
