Shader "Roots/IllustratedOutline"
{
    Properties { _Width("Width", Float)=0.009 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry-1" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Width;
            CBUFFER_END
            struct A { float4 p:POSITION; float3 n:NORMAL; };
            float4 vert(A v):SV_POSITION
            {
                float3 world=TransformObjectToWorld(v.p.xyz);
                world+=TransformObjectToWorldNormal(v.n)*_Width;
                return TransformWorldToHClip(world);
            }
            half4 frag():SV_Target { return half4(0.022,0.036,0.038,1); }
            ENDHLSL
        }
    }
}
