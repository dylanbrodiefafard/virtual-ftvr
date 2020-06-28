#ifndef _CG_NOISE_INC
#define _CG_NOISE_INC

// Author: Christopher Chamberlain - 2017
// Random functions based on:
//     https://stackoverflow.com/questions/4200224/random-noise-functions-for-glsl

/* ================ *\
 * RANDOM FUNCTIONS *
\* ================ */

/**
* Computes a psuedo-random value for the given input coordinate.
*
* x - input coordinate
*/
float random(float x) {
	return frac(sin(x) * 100000.0);
}

/**
 * Computes a psuedo-random value for the given input coordinate.
 *
 * xy - input coordinate
 */
float random(float2 xy) {
	return frac(sin(dot(xy, float2(12.9898, 78.233))) * 43758.5453123);
}

/**
 * Computes a psuedo-random value for the given input coordinate.
 *
 * xyz - input coordinate
 */
float random(float3 xyz) {
	return frac(sin(dot(xyz, float3(12.9898, 78.233, 35.221))) * 43758.5453123);
}

/* =============== *\
 * NOISE FUNCTIONS *
\* =============== */

/**
 * Procedural 2D noise function.
 *
 * x - input coordinate
 */
float noise(in float x)
{
	float2 i = floor(x);
	float2 f = frac(x);

	// Four corners in 2D of a tile
	float a = random(i);
	float b = random(i + 1.0);

	// Smooth Interpolation
	float u = smoothstep(0.0, 1.0, f);

	// Interpolate across tile
	return lerp(a, b, u) * 2.0 - 1.0;
}

/**
 * Procedural 2D noise function.
 *
 * xy - input coordinate
 */
float noise(in float2 xy)
{
	float2 i = floor(xy);
	float2 f = frac(xy);

	// Four corners in 2D of a tile
	float a = random(i);
	float b = random(i + float2(1.0, 0.0));
	float c = random(i + float2(0.0, 1.0));
	float d = random(i + float2(1.0, 1.0));

	// Smooth Interpolation
	float2 u = smoothstep(0.0, 1.0, f);

	// Interpolate across tile
	float x1 = lerp(a, b, u.x);
	float x2 = lerp(c, d, u.x);
	return lerp(x1, x2, u.y) * 2.0 - 1.0;
}

/**
 * Procedural 3D noise function.
 *
 * xyz - input coordinate
 */
float noise(in float3 xyz)
{
	float3 i = floor(xyz);
	float3 f = frac(xyz);

	// Eight corners in 3D of a tile
	float a1 = random(i + float3(0.0, 0.0, 0.0));
	float b1 = random(i + float3(1.0, 0.0, 0.0));
	float c1 = random(i + float3(0.0, 1.0, 0.0));
	float d1 = random(i + float3(1.0, 1.0, 0.0));

	float a2 = random(i + float3(0.0, 0.0, 1.0));
	float b2 = random(i + float3(1.0, 0.0, 1.0));
	float c2 = random(i + float3(0.0, 1.0, 1.0));
	float d2 = random(i + float3(1.0, 1.0, 1.0));

	// Smooth Interpolation
	float3 u = smoothstep(0.0, 1.0, f);

	// Interpolate Lower X Axii
	float x11 = lerp(a1, b1, u.x);
	float x21 = lerp(c1, d1, u.x);
	float x12 = lerp(a2, b2, u.x);
	float x22 = lerp(c2, d2, u.x);

	// Interpolate Across Y Axii
	float y1 = lerp(x11, x21, u.y);
	float y2 = lerp(x12, x22, u.y);

	// Interpolate Z Axis
	return lerp(y1, y2, u.z) * 2.0 - 1.0;
}

/* ====================== *\
 * OCTAVE NOISE FUNCTIONS *
\* ====================== */

/**
 * Procedural 1D noise function, with octaves.
 *
 * x - Input coordinates
 * octaves - How many 'layers', must be at least one.
 * persistence - How persistant each octave is as each layer is applied.
 */
float noise(in float x, in int octaves, in float persistence)
{
	float t = 0.0;
	float a = 1.0;
	float f = 1.0;
	float m = 0.0;

	for (int i = 0; i < octaves; i++)
	{
		t += noise(x * f) * a;
		m += a;

		a *= persistence;
		f *= 2.0;
	}

	return t / m;
}

/**
 * Procedural 2D noise function, with octaves.
 *
 * xy - Input coordinates
 * octaves - How many 'layers', must be at least one.
 * persistence - How persistant each octave is as each layer is applied.
 */
float noise(in float2 xy, in int octaves, in float persistence)
{
	float t = 0.0;
	float a = 1.0;
	float f = 1.0;
	float m = 0.0;

	for (int i = 0; i < octaves; i++)
	{
		t += noise(xy * f) * a;
		m += a;

		a *= persistence;
		f *= 2.0;
	}

	return t / m;
}

/**
 * Procedural 3D noise function, with octaves.
 *
 * xyz - Input coordinates
 * octaves - How many 'layers', must be at least one.
 * persistence - How persistant each octave is as each layer is applied.
 */
float noise(in float3 xyz, in int octaves, in float persistence)
{
	float t = 0.0;
	float a = 1.0;
	float f = 1.0;
	float m = 0.0;

	for (int i = 0; i < octaves; i++)
	{
		t += noise(xyz * f) * a;
		m += a;

		a *= persistence;
		f *= 2.0;
	}

	return t / m;
}

#endif