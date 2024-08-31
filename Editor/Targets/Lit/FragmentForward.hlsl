#pragma fragment frag

#include "Functions.hlsl"

#ifdef _CBIRP
    #include "Packages/z3y.clusteredbirp/Shaders/CBIRP.hlsl"
#endif
#ifdef _LTCGI
    #include "Assets/_pi_/_LTCGI/Shaders/LTCGI.cginc"
#endif

#ifndef QUALITY_LOW
    #define BAKERY_SHNONLINEAR
#endif

half4 frag(Varyings varyings) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(varyings);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
    
    SurfaceDescription surf = SurfaceDescriptionFunction(varyings);
    half roughness2 = surf.Roughness * surf.Roughness;

    #if !defined(_ALPHATEST_ON) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_ALPHAMODULATE_ON) && !defined(_ALPHAFADE_ON)
        surf.Alpha = 1.0;
    #endif

    #if defined(_ALPHATEST_ON)
        if (surf.Alpha < surf.Cutoff) discard;
    #endif

    FragmentData fragData = FragmentData::Create(varyings);

    GIInput giInput = GIInput::New();

    #if defined(_NORMAL_DROPOFF_OFF)
        giInput.normalWS = fragData.normalWS;
    #elif defined(_NORMAL_DROPOFF_WS)
        giInput.normalWS = surf.Normal;
    #elif defined(_NORMAL_DROPOFF_OS)
        giInput.normalWS = TransformObjectToWorldNormal(surf.Normal);
    #else // _NORMAL_DROPOFF_TS
        giInput.normalWS = SafeNormalize(mul(surf.Normal, fragData.tangentSpaceTransform));
    #endif
    
    giInput.NoV = abs(dot(giInput.normalWS, fragData.viewDirectionWS)) + 1e-5f;
    giInput.reflectVector = reflect(-fragData.viewDirectionWS, giInput.normalWS);
    #if !defined(QUALITY_LOW)
        giInput.reflectVector = lerp(giInput.reflectVector, giInput.normalWS, roughness2);
    #endif
    giInput.f0 = 0.16 * surf.Reflectance * surf.Reflectance * (1.0 - surf.Metallic) + surf.Albedo * surf.Metallic;
    Filament::EnvironmentBRDF(giInput.NoV, surf.Roughness, giInput.f0, giInput.brdf, giInput.energyCompensation);

    Light unityLight = Light::GetUnityLight(varyings);
    unityLight.ComputeData(fragData, giInput);

    GIOutput giOutput = GIOutput::New();

    #if defined(LIGHTMAP_ON)
        float2 lightmapUV = varyings.lightmapUV;
        #if defined(_BICUBIC_LIGHTMAP) && !defined(QUALITY_LOW)
            float4 texelSize = TexelSizeFromTexture2D(unity_Lightmap);
            half3 illuminance = SampleTexture2DBicubic(unity_Lightmap, custom_bilinear_clamp_sampler, lightmapUV, texelSize, 1.0).rgb;
        #else
            half3 illuminance = DecodeLightmap(unity_Lightmap.SampleLevel(custom_bilinear_clamp_sampler, lightmapUV, 0));
        #endif

        #if defined(DIRLIGHTMAP_COMBINED) || defined(_BAKERY_MONOSH)
            #if defined(_BICUBIC_LIGHTMAP) && !defined(QUALITY_LOW)
                half4 directionalLightmap = SampleTexture2DBicubic(unity_LightmapInd, custom_bilinear_clamp_sampler, lightmapUV, texelSize, 1.0);
            #else
                half4 directionalLightmap = unity_LightmapInd.SampleLevel(custom_bilinear_clamp_sampler, lightmapUV, 0);
            #endif
            #ifdef _BAKERY_MONOSH
                half3 L0 = illuminance;
                half3 nL1 = directionalLightmap * 2.0 - 1.0;
                half3 L1x = nL1.x * L0 * 2.0;
                half3 L1y = nL1.y * L0 * 2.0;
                half3 L1z = nL1.z * L0 * 2.0;
                #ifdef BAKERY_SHNONLINEAR
                    float lumaL0 = dot(L0, 1);
                    float lumaL1x = dot(L1x, 1);
                    float lumaL1y = dot(L1y, 1);
                    float lumaL1z = dot(L1z, 1);
                    float lumaSH = shEvaluateDiffuseL1Geomerics(lumaL0, float3(lumaL1x, lumaL1y, lumaL1z), giInput.normalWS);

                    half3 sh = L0 + giInput.normalWS.x * L1x + giInput.normalWS.y * L1y + giInput.normalWS.z * L1z;
                    float regularLumaSH = dot(sh, 1);
                    sh *= lerp(1, lumaSH / regularLumaSH, saturate(regularLumaSH * 16));
                #else
                    half3 sh = L0 + giInput.normalWS.x * L1x + giInput.normalWS.y * L1y + giInput.normalWS.z * L1z;
                #endif

                illuminance = sh;
                #ifdef _LIGHTMAPPED_SPECULAR
                {
                    half smoothnessLm = 1.0f - max(roughness2, 0.002);
                    smoothnessLm *= sqrt(saturate(length(nL1)));
                    half roughnessLm = 1.0f - smoothnessLm;
                    half3 dominantDir = nL1;
                    half3 halfDir = Unity_SafeNormalize(normalize(dominantDir) + fragData.viewDirectionWS);
                    half nh = saturate(dot(giInput.normalWS, halfDir));
                    half spec = Filament::D_GGX(nh, roughnessLm);
                    sh = L0 + dominantDir.x * L1x + dominantDir.y * L1y + dominantDir.z * L1z;
                    
                    #ifdef _ANISOTROPY
                        // half at = max(roughnessLm * (1.0 + surf.Anisotropy), 0.001);
                        // half ab = max(roughnessLm * (1.0 - surf.Anisotropy), 0.001);
                        // giOutput.indirectSpecular += max(Filament::D_GGX_Anisotropic(nh, halfDir, sd.tangentWS, sd.bitangentWS, at, ab) * sh, 0.0);
                    #else
                        giOutput.indirectSpecular += max(spec * sh, 0.0);
                    #endif
                }
                #endif
            #else
                half halfLambert = dot(giInput.normalWS, directionalLightmap.xyz - 0.5) + 0.5;
                illuminance = illuminance * halfLambert / max(1e-4, directionalLightmap.w);
            #endif
        #endif
        #if defined(LIGHTMAP_SHADOW_MIXING) && !defined(SHADOWS_SHADOWMASK) && defined(SHADOWS_SCREEN)
            illuminance = SubtractMainLightWithRealtimeAttenuationFromLightmap(illuminance, unityLight.attenuation, float4(0,0,0,0), giInput.normalWS);
            unityLight.color = 0;
        #endif

        giOutput.indirectDiffuse = illuminance;

        #if defined(_BAKERY_MONOSH)
            giOutput.indirectOcclusion = (dot(nL1, giInput.reflectVector) + 1.0) * L0 * 2.0;
        #else
            giOutput.indirectOcclusion = illuminance;
        #endif

    #elif defined(UNITY_PASS_FORWARDBASE)
        #if defined(_FLATSHADING)
        {
            float3 sh9Dir = (unity_SHAr.xyz + unity_SHAg.xyz + unity_SHAb.xyz);
            float3 sh9DirAbs = float3(sh9Dir.x, abs(sh9Dir.y), sh9Dir.z);
            half3 N = normalize(sh9DirAbs);
            UNITY_FLATTEN
            if (!any(unity_SHC.xyz))
            {
                N = 0;
            }
            half3 l0l1 = SHEvalLinearL0L1(float4(N, 1));
            half3 l2 = SHEvalLinearL2(float4(N, 1));
            giOutput.indirectDiffuse = l0l1 + l2;
        }
        #else
            #if UNITY_SAMPLE_FULL_SH_PER_PIXEL
                giOutput.indirectDiffuse = ShadeSHPerPixel(giInput.normalWS, 0.0, fragData.positionWS);
            #else
                giOutput.indirectDiffuse = ShadeSHPerPixel(giInput.normalWS, varyings.sh, fragData.positionWS);
            #endif
            giOutput.indirectOcclusion = giOutput.indirectDiffuse;
        #endif
    #endif
    giOutput.indirectDiffuse = max(0.0, giOutput.indirectDiffuse);


    // unity lights
    ShadeLightDefault(unityLight, fragData, giInput, surf, giOutput);

    // reflection probes
    #if !defined(_GLOSSYREFLECTIONS_OFF)
        Unity_GlossyEnvironmentData envData;
        envData.roughness = surf.Roughness;
        envData.reflUVW = BoxProjectedCubemapDirection(giInput.reflectVector, fragData.positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);

        half3 probe0 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData);
        half3 reflectionSpecular = probe0;

        #if defined(UNITY_SPECCUBE_BLENDING)
            UNITY_BRANCH
            if (unity_SpecCube0_BoxMin.w < 0.99999)
            {
                envData.reflUVW = BoxProjectedCubemapDirection(giInput.reflectVector, fragData.positionWS, unity_SpecCube1_ProbePosition, unity_SpecCube1_BoxMin, unity_SpecCube1_BoxMax);
                float3 probe1 = Unity_GlossyEnvironment(UNITY_PASS_TEXCUBE_SAMPLER(unity_SpecCube1, unity_SpecCube0), unity_SpecCube1_HDR, envData);
                reflectionSpecular = lerp(probe1, probe0, unity_SpecCube0_BoxMin.w);
            }
        #endif
        giOutput.indirectSpecular += reflectionSpecular;
    #endif

                    
    #ifdef _CBIRP
            #ifdef LIGHTMAP_ON
            half4 shadowmask = _Udon_CBIRP_ShadowMask.SampleLevel(custom_bilinear_clamp_sampler, lightmapUV, 0);
            // half4 shadowmask = 1;
        #else
            half4 shadowmask = 1;
    #endif
        giOutput.directDiffuse = 0;
        giOutput.directSpecular = 0;
        uint3 cluster = CBIRP::GetCluster(fragData.positionWS);
        CBIRP::ComputeLights(cluster, fragData.positionWS, giInput.normalWS, fragData.viewDirectionWS, giInput.f0, giInput.NoV, surf.Roughness, shadowmask, giOutput.directDiffuse, giOutput.directSpecular);
        giOutput.directSpecular *= giInput.energyCompensation;
        giOutput.indirectSpecular = CBIRP::SampleProbes(cluster, giInput.reflectVector, fragData.positionWS, surf.Roughness).xyz;
    #endif

    #if !defined(QUALITY_LOW)
        float horizon = min(1.0 + dot(giInput.reflectVector, giInput.normalWS), 1.0);
        giOutput.indirectSpecular *= horizon * horizon;
    #endif


    #ifdef _LTCGI
        float2 untransformedLightmapUV = 0;
        #ifdef LIGHTMAP_ON
        untransformedLightmapUV = (lightmapUV - unity_LightmapST.zw) / unity_LightmapST.xy;
        #endif
        float3 ltcgiSpecular = 0;
        float3 ltcgiDiffuse = 0;
        LTCGI_Contribution(fragData.positionWS.xyz, giInput.normalWS, fragData.viewDirectionWS, surf.Roughness, untransformedLightmapUV, ltcgiDiffuse, ltcgiSpecular);
        #ifndef LTCGI_DIFFUSE_DISABLED
            giOutput.directDiffuse += ltcgiDiffuse;
        #endif
        giOutput.indirectSpecular += ltcgiSpecular;
    #endif

    half3 fr;
    fr = giInput.energyCompensation * giInput.brdf;
    giOutput.indirectSpecular *= fr;

    half specularAO;
    #if defined(QUALITY_LOW)
        specularAO = surf.Occlusion;
    #else
        specularAO = Filament::ComputeSpecularAO(giInput.NoV, surf.Occlusion, roughness2);
    #endif
    giOutput.directSpecular *= specularAO;

    half indirectOcclusionIntensity = 1.0;
    specularAO *= lerp(1.0, saturate(sqrt(dot(giOutput.indirectOcclusion + giOutput.directDiffuse, 1.0))), indirectOcclusionIntensity);
    giOutput.indirectSpecular *= specularAO;


    #ifdef _FLATSHADING
        giOutput.indirectDiffuse = saturate(max(giOutput.indirectDiffuse, giOutput.directDiffuse));
        giOutput.directDiffuse = 0.0;
        #if !(!defined(_ALPHATEST_ON) && !defined(_ALPHAPREMULTIPLY_ON) && !defined(_ALPHAMODULATE_ON) && !defined(_ALPHAFADE_ON))
            #ifdef UNITY_PASS_FORWARDADD
                surf.Albedo *= surf.Alpha; // theres probably a better way
            #endif
        #endif
    #endif

    AlphaTransparentBlend(surf.Alpha, surf.Albedo, surf.Metallic);

    half4 color = FinalColorDefault(surf, fragData, giInput, giOutput);

    UNITY_APPLY_FOG(varyings.fogCoord, color);

    return color;
}