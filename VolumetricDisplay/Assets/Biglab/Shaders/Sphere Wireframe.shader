Shader "Biglab/Sphere Wireframe" 
{
	Properties 
	{
		_NumLines( "Number of lines", Int ) = 20
		_Color( "Color of the lines", Color ) = (1,1,1,1)
	}

	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			// ZWrite On
			// ZTest Off
			Cull Off
			
			CGPROGRAM

			#include "UnityCG.cginc"
			#pragma fragment frag
			#pragma vertex vert
			
			// 
			float _Width;
			int _NumLines;
			float4 _Color;

			// 
			#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D10)
			#define FACING SV_IsFrontFace
			#define FACE_TYPE uint
			#else
			#define FACING FACE 
			#define FACE_TYPE float
			#endif 

			struct vertexInput {
				float4 vertex : POSITION;
			};

			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 vertex : TEXCOORD0;
			};

			float2 cartesianToSpherical(float3 pos) {
				return float2(acos(pos.y) / 3.14159, atan2(pos.z,pos.x) / 3.14159);
			}

			vertexOutput vert(vertexInput v) {
				vertexOutput o;
				// Calculate vertex position in camera space
				o.pos = UnityObjectToClipPos(v.vertex);
				o.vertex = v.vertex;
				return o;
			}

			float4 frag(vertexOutput i, FACE_TYPE face : FACING) : COLOR
			{
				float2 spherePos = cartesianToSpherical(normalize(i.vertex.xyz));
				
				// Scale the sphere to determine which fragments are coloured
				float2 scaledSpherePos = spherePos * _NumLines;
				
				// Computes the derivative
				float xdir = max(abs(ddx(scaledSpherePos.x)), abs(ddy(scaledSpherePos.x)));
				float ydir = max(abs(ddx(scaledSpherePos.y)), abs(ddy(scaledSpherePos.y)));
				float mdir = max(xdir, ydir);

				// Compute lines
				float xline = abs(scaledSpherePos.x - round(scaledSpherePos.x));
				float yline = abs(scaledSpherePos.y - round(scaledSpherePos.y));
				float mline = max(xline, yline);

				// Compute smooth line
				float m = saturate( max( xdir - xline, ydir - yline ) * 3.0 );
				m -= saturate(mdir - mline);
				if( m <= 0 ) discard;

				return float4( _Color.rgb, _Color.a );
			}

			ENDCG
		} 
	}
}