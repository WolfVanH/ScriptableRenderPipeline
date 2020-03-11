#ifndef UNITY_PATH_TRACING_VOLUME_INCLUDED
#define UNITY_PATH_TRACING_VOLUME_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

void ApplyFogAttenuation(float3 origin, float3 direction, float t, inout float3 value)
{
    if (_FogEnabled)
    {
        float absFogBaseHeight = GetAbsolutePositionWS(float3(0.0, _HeightFogBaseHeight, 0.0)).y;
        float fogTransmittance = TransmittanceHeightFog(_HeightFogBaseExtinction, absFogBaseHeight, _HeightFogExponents, direction.y, origin.y, t);
        value = lerp(GetFogColor(-direction, t), value, fogTransmittance);
    }
}

void ApplyFogAttenuation(float3 origin, float3 direction, inout float3 value)
{
    if (_FogEnabled)
    {
        float absFogBaseHeight = GetAbsolutePositionWS(float3(0.0, _HeightFogBaseHeight, 0.0)).y;
        float fogTransmittance = TransmittanceHeightFog(_HeightFogBaseExtinction, absFogBaseHeight, _HeightFogExponents, direction.y, origin.y);
        value = lerp(GetFogColor(-direction, _MipFogFar), value, fogTransmittance);
    }
}

#endif // UNITY_PATH_TRACING_VOLUME_INCLUDED
