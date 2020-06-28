#ifndef _CG_EXTRA_INC
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
#define _CG_EXTRA_INC

static const float4 BACK_COLOR = float4(0, 0, 0, 1);
static const float4 CLIP_COLOR = float4(0, 0, 0, 1);

// Virtual Display specific uniforms
uniform float4x4 _PhysicalToWorld;
uniform float4x4 _PhysicalToWorldNormal;

// Display specific uniforms
uniform float4x4 _VolumeToWorld;
uniform float4x4 _VolumeToWorldNormal;
			
// Viewer specific uniforms
sampler2D _MainTex; // TODO: Rename to ViewerTex ? 
float2 _MainTex_TexelSize;
float4 _MainTex_ST;
uniform float4 _ViewerPosition;
uniform float4x4 _ViewerMatrix;

// (Projector) Geometry specific uniforms
sampler2D _ProjectorTex; // TODO: Rename this?
sampler2D _NormalMap; // TODO: rename this?
float2 _ProjectorTex_TexelSize;
float4 _ProjectorTex_ST;

// No viewer specific uniforms
//samplerCUBE _FlatTex;
uniform float4x4 _FlatWorldToClips[6];
UNITY_DECLARE_TEX2DARRAY(_FlatTex);

// Overlay specific uniforms
// samplerCUBE _OverlayCubemap; // TODO: implement

float2 TRANSFORM_GEOMETRY_TEX(float2 uv) {
	return TRANSFORM_TEX(uv, _ProjectorTex);
}

float4 PhysicalToWorldPosition(float3 v_position) {
	return mul(_PhysicalToWorld, float4(v_position, 1));
}

float3 PhysicalToWorldNormal(float3 v_normal) {
	return mul(_PhysicalToWorldNormal, v_normal);
}

float4 VolumeToWorldPosition(float3 v_position) {
	return mul(_VolumeToWorld, float4(v_position, 1));
}

float3 VolumeToWorldNormal(float3 v_normal) {
	return mul(_VolumeToWorldNormal, v_normal);
}


//fixed4 GetOverlayColor(float3 w_normal) {
	// TODO: implement this
//	return texCUBE(_OverlayCubemap, w_normal);
//}

float4 GetGeometryData(float2 uv) {
	return tex2D(_ProjectorTex, uv);
}

float3 GetVolumeNormal(float2 uv) {
	return tex2D(_NormalMap, uv);
}

// +x -x +y -y +z -z
int getFaceIndex(float3 w_normal) {
	float xx = abs(w_normal.x);
	float yy = abs(w_normal.y);
	float zz = abs(w_normal.z);
	if (xx > yy && xx > zz) return w_normal.x < 0 ? 0 : 1;
	if (yy > xx && yy > zz) return w_normal.y < 0 ? 2 : 3;
	if (zz > xx && zz > yy) return w_normal.z < 0 ? 4 : 5;
	return 0; // FAILED?
}

//fixed4 GetFlatColor(float3 w_normal) {
	// The cube map faces are rendered horizontally flipped from what we need
    // so we need to reflect the normal horizontally to properly sample the texture
//	float3 uvw = w_normal;
//    if(abs(w_normal.x) > abs(w_normal.z)) uvw = reflect(uvw, float3(1, 0, 0));
//    else uvw = reflect(uvw, float3(0, 0, 1));

	// Sample the flat cubemap
//	return texCUBE(_FlatTex, uvw);
//}

fixed4 GetFlatColor(float3 w_position, float3 w_normal)
{ 
	int faceIndex = getFaceIndex(w_normal); 
	// Transform vertex from W ( world ) to C ( clip ) to UV
	float4 c_position = mul(_FlatWorldToClips[faceIndex], float4(w_position, 1));
	c_position.xyz /= c_position.w;
	c_position.xy = c_position.xy * 0.5 + 0.5;
	c_position.z = faceIndex;

	return UNITY_SAMPLE_TEX2DARRAY(_FlatTex, c_position.xyz);
}

// Determines the 'invalid zones' of a projection
bool isOutsideProjection(float4 proj) {
	if (proj.w < 0) return true;
	if (proj.x < 0 || proj.y < 0) return true;
	if (proj.x > 1 || proj.y > 1) return true;
	return false;
}

fixed4 GetViewerColor(float4 w_position, float3 w_normal) {
	// Transform vertex from W ( world ) to C ( clip ) to UV
	float4 c_position = mul(_ViewerMatrix, w_position);
	c_position.xyz /= c_position.w;
	c_position.xy = c_position.xy * 0.5 + 0.5;
	
	// Are we behind the viewpoint?
	if (isOutsideProjection(c_position)) return CLIP_COLOR;

	// Clip if the viewer can't see this fragment
	float3 w_view = normalize(_ViewerPosition - w_position);
	if (dot(w_view, w_normal) < 0) return BACK_COLOR;

	// Sample Viewpoint ( projection )
	return tex2D(_MainTex, c_position.xy);
}

// Flips the vertical UV
float2 flipUV(float2 uv) {
	uv.y = 1 - uv.y;
	return uv;
}

// Blends src into dst ( normal blending )
float4 alphaBlend(float4 src, float4 dst) {
	return src * src.a + dst * (1.0 - src.a);
}

#endif