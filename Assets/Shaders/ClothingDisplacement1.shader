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

        _DisplacementScale("Displacement Scale", Float) = 1.0

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
        float _DisplacementScale;
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

                // --- Apply displacement
                float3 displacement = _Displacements[vertexID] * _DisplacementScale;
                float3 displacedPosOS = IN.positionOS.xyz + displacement;

                // Transform position to world space
                float4 posWS = float4(TransformObjectToWorld(displacedPosOS), 1.0);
                OUT.positionWS = posWS.xyz;

                // Transform to clip space for SV_POSITION
                OUT.positionCS = TransformWorldToHClip(posWS.xyz);

                // Pass UVs
                OUT.uv = IN.uv;
                #if LIGHTMAP_ON
                OUT.uvLightmap = IN.uvLightmap.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                #endif

                // --- Compute approximate normal using screen-space derivatives
                // This will give a normal that matches displaced geometry for lighting
                float3 displacedPos = IN.positionOS + _Displacements[vertexID] * _DisplacementScale;

                // Original vertex-space tangent/normal
                float3 N = IN.normalOS;
                float3 T = IN.tangentOS.xyz;
                float3 B = cross(N, T) * IN.tangentOS.w;

                // Tiny offsets along tangent space
                float eps = 0.001;
                float3 dp1 = displacedPos + T * eps;
                float3 dp2 = displacedPos + B * eps;

                // Approximate normal in object space
                float3 approxNormal = normalize(cross(dp2 - displacedPos, dp1 - displacedPos));

                // Transform to world space
                OUT.normalWS = normalize(TransformObjectToWorldDir(approxNormal));



                #ifdef _NORMALMAP
                // Keep tangent if you still want normal maps
                OUT.tangentWS = float4(IN.tangentOS.xyz, IN.tangentOS.w);
                #endif

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