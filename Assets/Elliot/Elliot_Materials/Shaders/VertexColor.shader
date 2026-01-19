Shader "Common/VertexColor"
{
    Properties
    {
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        HLSLPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float _Glossiness;
        float _Metallic;

        struct Input
        {
            float4 color : COLOR;   // <-- use float4
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = IN.color.a;
        }
        ENDHLSL
    }

    FallBack "Standard"
}