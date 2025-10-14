//lit shader from: https://github.com/phi-lira/UniversalShaderExamples/tree/master/Assets/_ExampleScenes/51_LitPhysicallyBased
Shader "Universal Render Pipeline/Custom/DisplacedLit"
{
    Properties
    {
        [Header(Surface)]
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 1.0
        [NoScaleOffset]_MetallicSmoothnessMap("MetallicMap", 2D) = "white" {}
        _AmbientOcclusion("Ambient Occlusion", Range(0, 1)) = 1.0
        [NoScaleOffset]_AmbientOcclusionMap("Ambient Occlusion Map", 2D) = "white" {}
        _Reflectance("Reflectance (dielectrics)", Range(0.0, 1.0)) = 0.5
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5

        [Toggle(_NORMALMAP)] _EnableNormalMap("Enable Normal Map", Float) = 0.0
        [Normal][NoScaleOffset]_NormalMap("Normal Map", 2D) = "bump" {}
        _NormalMapScale("Normal Map Scale", Float) = 1.0

        [Header(Emission)]
        [HDR]_Emission("Emission Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True" }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _Metallic;
        half _AmbientOcclusion;
        half _Reflectance;
        half _Smoothness;
        half4 _Emission;
        half _NormalMapScale;
        CBUFFER_END

        // Structured buffer of vertex displacements
        StructuredBuffer<float3> _Displacements;
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex SurfaceVertex
            #pragma fragment SurfaceFragment

            #pragma shader_feature _NORMALMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            #include "CustomShading.hlsl"

            // -------------------------------------
            // Textures
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_MetallicSmoothnessMap);
            TEXTURE2D(_AmbientOcclusionMap);

            // --- Vertex function with displacement
            Varyings SurfaceVertex(Attributes IN, uint vertexID : SV_VertexID)
            {
                Varyings OUT;

                // apply displacement
                float3 displacement = _Displacements[vertexID];
                float3 displacedPosOS = IN.positionOS.xyz + displacement;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(displacedPosOS);
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.uv = IN.uv;
                #if LIGHTMAP_ON
                OUT.uvLightmap = IN.uvLightmap.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif

                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = vertexNormalInput.normalWS;

                #ifdef _NORMALMAP
                OUT.tangentWS = float4(vertexNormalInput.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                #endif

                OUT.positionCS = vertexInput.positionCS;
                return OUT;
            }

            // --- Surface data generation
            void SurfaceFunction(Varyings IN, out CustomSurfaceData surfaceData)
            {
                surfaceData = (CustomSurfaceData)0;
                float2 uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                
                half3 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                half4 metallicSmoothness = SAMPLE_TEXTURE2D(_MetallicSmoothnessMap, sampler_BaseMap, uv);
                half metallic = _Metallic * metallicSmoothness.r;
                surfaceData.diffuse = ComputeDiffuseColor(baseColor.rgb, metallic);
                surfaceData.reflectance = ComputeFresnel0(baseColor.rgb, metallic, _Reflectance * _Reflectance * 0.16);
                surfaceData.ao = SAMPLE_TEXTURE2D(_AmbientOcclusionMap, sampler_BaseMap, uv).g * _AmbientOcclusion;
                surfaceData.perceptualRoughness = 1.0 - (_Smoothness * metallicSmoothness.a);

                #ifdef _NORMALMAP
                surfaceData.normalWS = GetPerPixelNormalScaled(TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), uv, IN.normalWS, IN.tangentWS, _NormalMapScale);
                #else
                surfaceData.normalWS = normalize(IN.normalWS);
                #endif

                surfaceData.emission = _Emission.rgb;
                surfaceData.alpha = 1.0;
            }
            ENDHLSL
        }

        // Use default shadow/depth/meta passes for URP Lit
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}