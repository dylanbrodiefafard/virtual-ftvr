Shader "Biglab/HosohedralProjection" {
	Properties {
		_MainTex("Texture", 2D) = "white" {}
	}

	SubShader {
		Cull Off
		ZTest Always
		ZWrite Off

		Pass {
			CGPROGRAM

			#include "UnityCG.cginc"

			#pragma vertex vert
			#pragma fragment frag

			sampler2D _MainTex;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			uniform float4x4 _Volume2World;
			uniform float4x4 _ViewerMatrix;
			uniform float3 _ViewerPosition;
			uniform sampler2D _ViewerTex;
			uniform int _Lunes;

			inline float3 spherical_to_cartesian(float r, float theta, float phi) {
				return r * float3(cos(theta) * sin(phi), 
					              cos(phi),
					              sin(theta) * sin(phi));
			}

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			half4 frag(v2f i) : SV_Target {

				const float PI = 3.1415926535897932384626433832795;

				float u = i.uv.x;
				float v = i.uv.y;

				float psi = (2 * PI) / _Lunes; // Radians per segment
				float phi = (1 - v) * PI; // Angle of inclination

				// Compute the XZ cartesian coordinate to derive azimuthal angle
				// float x = rescale(0, 1.0f / _Lunes, -tan(psi / 2), tan(psi / 2), fmod(u, 1.0f / _Lunes)) // Simplified below:
				float x = tan(PI / _Lunes) * (2 * _Lunes * fmod(u, 1.0f / _Lunes) - 1); // X is the dimension that goes across the paper
				float z = sin(phi); // Z is the dimension that comes in/out of the paper

				float theta = atan2(x, z); // Angle of azimuth

				if (abs(theta) > psi / 2) // It's not in the segment
				{
					if (abs(theta) - psi / 2 < 0.1f *(1 - sin(phi))) // Make a line
					{
						return half4(1, 1, 1, 1) * 0.8f;
					}
					return half4(1, 1, 1, 1);
				}

				int lune = u * _Lunes; // Which segment is this
				float beta = lune * psi; // Panel theta offset

				// Transform fragment from spherical coordinates to cartesian coordinates ( volumetric camera )
				float3 v_position = spherical_to_cartesian(0.5f, theta + beta, phi);

				if (dot(normalize(_ViewerPosition), normalize(v_position)) > 0) // Backside of the display
				{
					return half4(1, 1, 1, 1);
				}

				// Transform fragment from V ( volumetric camera ) to W ( world )
				float3 w_position = mul(_Volume2World, float4(v_position, 1));
				// Transform vertex from W ( world ) to C ( clip ) to UV
				float4 c_position = mul(_ViewerMatrix, float4(w_position, 1));
				// Transform to UV coordinates
				c_position.xyz /= c_position.w;
				c_position.xy = c_position.xy * 0.5 + 0.5;

				// Sample Viewpoint ( projection )
				return tex2D(_ViewerTex, c_position.xy);
			}

			ENDCG
		}
	}
}
