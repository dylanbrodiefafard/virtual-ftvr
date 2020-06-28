#ifndef _CG_NOISE_INC
#define _CG_NOISE_INC

/* RANDOM FUNCTIONS */

// 1D Procedural Random
float random(float x) {
	return frac(sin(x) * 100000.0);
}

// 2D Procedural Random
float random(float2 xy) {
	return frac(sin(dot(xy, float2(12.9898, 78.233))) * 43758.5453123);
}

// 3D Procedural Random
float random(float3 xyz) {
	return frac(sin(dot(xyz, float3(12.9898, 78.233, 35.221))) * 43758.5453123);
}

/* NOISE FUNCTIONS */

// 1D Procedural Noise
float noise(in float s)
{
	float2 i = floor(s);
	float2 f = frac(s);

	// Four corners in 2D of a tile
	float a = random(i);
	float b = random(i + 1.0);

	// Smooth Interpolation
	float u = smoothstep(0.0, 1.0, f);

	// Interpolate across tile
	return lerp(a, b, u) * 2.0 - 1.0;
}

// 2D Procedural Noise
float noise(in float2 st)
{
	float2 i = floor(st);
	float2 f = frac(st);

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

// 3D Procedural Noise
float noise(in float3 st)
{
	float3 i = floor(st);
	float3 f = frac(st);

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

/* OCTAVE NOISE FUNCTIONS */

// 1D Noise w/ Octaves
float noise(in float s, in int octaves, in float persistence)
{
	float t = 0.0;
	float a = 1.0;
	float f = 1.0;
	float m = 0.0;

	for (int i = 0; i < octaves; i++)
	{
		t += noise(s * f) * a;
		m += a;

		a *= persistence;
		f *= 2.0;
	}

	return t / m;
}

// 2D Noise w/ Octaves
float noise(in float2 st, in int octaves, in float persistence)
{
	float t = 0.0;
	float a = 1.0;
	float f = 1.0;
	float m = 0.0;

	for (int i = 0; i < octaves; i++)
	{
		t += noise(st * f) * a;
		m += a;

		a *= persistence;
		f *= 2.0;
	}

	return t / m;
}

// 3D Noise w/ Octaves
float noise(in float3 str, in int octaves, in float persistence)
{
	float t = 0.0;
	float a = 1.0;
	float f = 1.0;
	float m = 0.0;

	for (int i = 0; i < octaves; i++)
	{
		t += noise(str * f) * a;
		m += a;

		a *= persistence;
		f *= 2.0;
	}

	return t / m;
}

#endif