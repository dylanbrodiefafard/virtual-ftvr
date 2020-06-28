Shader "Unlit/CubemapProjector"
{
	Properties {
      //_MainTex ("Texture", 2D) = "white" {}
	  _Cube ("Cubemap", CUBE) = "" {}
    }
    SubShader {
      Tags { "RenderType" = "Opaque" }
      CGPROGRAM
      #pragma surface surf Lambert
      struct Input {
          float2 uv_MainTex;
          float3 worldRefl;
		  float3 worldNormal;
      };

      sampler2D _MainTex;
      samplerCUBE _Cube;
      void surf (Input IN, inout SurfaceOutput o) {
		  float3 cubeUV = float3(IN.worldNormal.x, IN.worldNormal.y, IN.worldNormal.z);
          // The cube maps are rendered horizontally flipped from what we need,
          // so we need to reflect the normal horizontally to properly sample the texture
          if(abs(cubeUV.x) > abs(cubeUV.z)) cubeUV = reflect(cubeUV, float3(1, 0, 0));
          else cubeUV = reflect(cubeUV, float3(0, 0, 1));
          o.Emission = texCUBE (_Cube, cubeUV).rgb;
      }
      ENDCG
    } 
    Fallback "Diffuse"
}
