#ifndef VOXELCIRCUIT_SHOCKWAVE_COMMON_INCLUDED
#define VOXELCIRCUIT_SHOCKWAVE_COMMON_INCLUDED

// Shared by the shockwave glow and heat-haze shaders so both take exactly the same shape.
//
// The Shockwave_Band mesh is a flat strip: object X from -1 (end of the trail) to 0 (front), object Y from -0.5 to
// 0.5 across the wave's height. The object sits at the middle of the front, with +X pointing in the travel
// direction and its Y scale set to the wave's full height. This bends the strip into half of the ring:
//   front(y) = frontDistance - lag(y),  lag(y) = curveDepth * (1 - sqrt(1 - n^2)),  n = -1..1 across the height
// (curveDepth = frontDistance gives a circle, "o"/"O"; a smaller curveDepth gives the flatter "( )").
// The trail behind each point is kept shorter than that point's distance from the ring centre, so the two
// halves never cross in the middle.
float3 ShockwaveRingWS(float3 positionOS, float curveDepth, float frontDistance, float trailLength)
{
    float n = clamp(positionOS.y * 2.0, -1.0, 1.0);
    float lag = curveDepth * (1.0 - sqrt(saturate(1.0 - n * n)));
    float frontHere = max(frontDistance - lag, 0.0);
    float trail = min(trailLength, frontHere * 0.85);

    float3 travelAxisWS = normalize(float3(UNITY_MATRIX_M._m00, UNITY_MATRIX_M._m10, UNITY_MATRIX_M._m20));
    float3 centreLineWS = TransformObjectToWorld(float3(0.0, positionOS.y, 0.0));
    return centreLineWS + travelAxisWS * (-lag + positionOS.x * trail);
}

// Cuts the ring at the cylinder's bottom and top with a short soft edge (the ring can be taller than the wall).
half ShockwaveClip(float worldY, float bottom, float top)
{
    return smoothstep(bottom, bottom + 0.6, worldY) * smoothstep(top, top - 0.6, worldY);
}

float ShockwaveHash(float2 p)
{
    return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
}

// Soft fade at the very tips of the strip, so the ring's tips taper instead of ending in a hard line.
half ShockwaveEnds(float v)
{
    return smoothstep(0.0, 0.04, v) * smoothstep(1.0, 0.96, v);
}

#endif
