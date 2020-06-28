// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Non Warp" 
{
	Properties 
	{
		_Color ( "Color", Color ) = ( 1,1,1,1 )
		_ApparentScale ( "Stage Scale", Float ) = 10
		_BlockThickness ( "Block Thickness", Float ) = 0.75
		_BlockCount ( "Number of Blocks Wrapping", Float ) = 8.0
		_Dither ( "50% Discard Transparency", Int ) = 0
	}

	SubShader 
	{
		Tags{ "RenderType" = "Opaque" }

		LOD 100
		// Cull Off

		Pass 
		{
			CGPROGRAM

			#pragma fragment frag
			#pragma vertex vert

			#include "UnityCG.cginc"
			
			// ===============================================
			// Structs
			// ===============================================

			struct Input {
				float2 uv_MainTex;
			};

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL0;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL0;
			};

			// ===============================================
			// Uniforms / Constants / Parameters
			// ===============================================

			half _ApparentScale;
			half _BlockThickness;
			half _BlockCount;

			int _Dither;
			
			fixed4 _Color;

			// ===============================================
			// Constants
			// ===============================================

			static const float Pi = 3.141592653;
			static const float PiHalf = Pi / 2.0;
			static const float TwoPi = Pi * 2.0;
			static const float InnerSphereRadius = 1.0;

			// ===============================================
			// Functions
			// ===============================================

			// Andrew Wagemakers 'Apparent Scale' formula.
			// ===============================================
			// r - radius of the sphere
			float WagemakersScaling( float r )
				{ return pow( 3.0, r - 1 ) / pow( 2.0, r - 2 ); }

			// Francois' 'Apparent Scale' formula.
			// ===============================================
			// r - radius of the sphere
			float FrancoisScaling( float r )
			{
				float x = InnerSphereRadius * pow( 1 + ( Pi / _BlockCount ), r );
				return x / pow( 1 - ( Pi / _BlockCount ), r );
			}

			// Adjusts the lower bound of x
			float LowerDomain( float x, float e )
				{ return e + x * ( 1.0 - e ); }

			// Adjusts the upper bound of x
			float CompressDomain( float x, float e )
				{ return e + x * ( 1.0 - e * 2.0 ); }

			// Computes a normal for the surface of a sphere.
			// ===============================================
			// xAngle - longitude angle
			// zAngle - latitude angle
			float3 CreatePolarNormal( float xAngle, float zAngle ) 
			{
				float cx = cos( xAngle );
				float sx = sin( xAngle );
				float cz = cos( zAngle + PiHalf );
				float sz = sin( zAngle + PiHalf );

				// return float3( cx * sz, cz, sx * sz );
				return float3( cx * cz, sz, sx * cz );
			}

			// Transforms the blocks from world space ( grid )
			// to a spherical space ( polar ).
			// ===============================================
			// pos - world space position
			float3 SphericalWarp( float3 pos )
			{
				const float _BlocksVertical = _BlockCount / 4.0;

				// Scaling to keep 'apparent scale' of wedge.
				// pos.y = WagemakersScaling( pos.y * _BlockThickness );
				pos.y = FrancoisScaling( pos.y * _BlockThickness );

				// Compute angles
				float xAngle = ( pos.x / _BlockCount ) * TwoPi;   // Angle due to X-Axis ( Horizontal wrap on sphere )
				float zAngle = ( pos.z / _BlocksVertical );       // Angle due to Z-Axis ( Vertical wrap on sphere )
				zAngle = CompressDomain( zAngle, 0.2 );
				zAngle = LowerDomain( zAngle, 0.3 );
				zAngle *= Pi;

				// Compute ploar normal
				return CreatePolarNormal( xAngle, zAngle ) * -pos.y;
			}

			// Transforms the blocks from world space ( grid )
			// to a cylindrical space ( ??? ).
			// ===============================================
			// pos - world space position
			float3 CylindricalWarp( float3 pos )
			{ 
				float xAngle = ( pos.x / _BlockCount ) * Pi * 2.0;  // Angle due to X-Axis ( Horizontal wrap on can )

				float cx = cos( xAngle ) * ( InnerSphereRadius + pos.y );
				float sx = sin( xAngle ) * ( InnerSphereRadius + pos.y );
				return float3( cx, pos.z, sx );
			}

			// Vertex Shader Program
			// Operates on every vertex independantly
			v2f vert( appdata v ) 
			{
				v2f o;
				o.vertex = UnityObjectToClipPos( v.vertex );
				o.normal = mul( UNITY_MATRIX_M, float4( v.normal, 0.0 ) ).xyz;
				return o;
			}

			// Fragment Shader Program
			// Operates on every pixel independantly
			fixed4 frag( v2f i ) : SV_Target
			{
				// Compute lambert term
				float l = dot( i.normal, normalize( float3( 1, 3, 2 ) ) );
				
				// Creates soft nintendo look
				l = ( l + 1.0 ) / 2.0;     // Wrap Lighting
				l = LowerDomain( l, 0.5 ); // Softens lighting

				if( _Dither > 0 )
				{
					int x = ( int(i.vertex.x + _Dither) & 1 ) == 0;
					int y = ( int(i.vertex.y) & 1 ) == 0;
					if( ( x ^ y ) > 0 ) discard; 
				}
				
				return _Color * l;
			}

			ENDCG
		}
	}
}
