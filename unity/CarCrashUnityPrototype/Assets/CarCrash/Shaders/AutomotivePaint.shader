Shader "CarCrash/AutomotivePaint"
{
    Properties
    {
        _Color ("Base coat", Color) = (.12,.2,.3,1)
        _MainTex ("Base texture", 2D) = "white" {}
        [Normal] _BumpMap ("Micro normal", 2D) = "bump" {}
        _BumpScale ("Micro normal strength", Range(0,1)) = .025
        _Glossiness ("Coat smoothness", Range(0,1)) = .76
        _Metallic ("Metal flake", Range(0,1)) = .38
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Range(0,1)) = .6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular fullforwardshadows addshadow
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex,_BumpMap,_OcclusionMap;
        fixed4 _Color;
        half _Glossiness,_Metallic,_BumpScale,_OcclusionStrength;
        struct Input { float2 uv_MainTex; float2 uv_BumpMap; };
        void surf(Input IN,inout SurfaceOutputStandardSpecular o)
        {
            half3 normal=UnpackScaleNormal(tex2D(_BumpMap,IN.uv_BumpMap),_BumpScale);
            half3 pigment=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
            // Neutral dielectric highlights keep the pigment beneath the coat
            // distinct from bare coloured metal. StandardSpecular supplies the
            // per-pixel Fresnel and energy-conserving light response.
            o.Albedo=pigment*.92;
            o.Specular=lerp(half3(.04,.04,.04),half3(.10,.10,.10),_Metallic);
            o.Smoothness=_Glossiness;
            o.Normal=normal;
            o.Occlusion=lerp(1,tex2D(_OcclusionMap,IN.uv_MainTex).g,_OcclusionStrength);
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
