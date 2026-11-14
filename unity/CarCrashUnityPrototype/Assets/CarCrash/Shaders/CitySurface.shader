Shader "CarCrash/CitySurface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal strength", Range(0,2)) = 0.6
        _MetallicGlossMap ("Metal / smoothness", 2D) = "white" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion strength", Range(0,1)) = 0.55
        _GlossMapScale ("Smoothness", Range(0,1)) = 0.4
        _WorldScale ("Tiles per metre", Float) = 0.5
        _Weathering ("Weathering", Range(0,1)) = 0.3
        _VertexTint ("Use baked facade colours", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        #include "UnityStandardUtils.cginc"
        sampler2D _MainTex, _BumpMap, _MetallicGlossMap, _OcclusionMap;
        fixed4 _Color;
        half _BumpScale, _GlossMapScale, _WorldScale, _Weathering, _OcclusionStrength, _VertexTint;
        struct Input { float3 worldPos; float3 worldNormal; fixed4 color : COLOR; INTERNAL_DATA };
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 baseNormal=normalize(WorldNormalVector(IN,float3(0,0,1)));
            float3 axis=abs(baseNormal);
            float2 uv;
            if(axis.y>axis.x && axis.y>axis.z)uv=IN.worldPos.xz*_WorldScale;
            else if(axis.x>axis.z)uv=IN.worldPos.zy*_WorldScale;
            else uv=IN.worldPos.xy*_WorldScale;
            half3 detail=UnpackScaleNormal(tex2D(_BumpMap,uv),_BumpScale);
            float3 worldDetail;
            if(axis.y>axis.x && axis.y>axis.z)worldDetail=float3(detail.x,detail.z*sign(baseNormal.y),detail.y);
            else if(axis.x>axis.z)worldDetail=float3(detail.z*sign(baseNormal.x),detail.y,detail.x);
            else worldDetail=float3(detail.x,detail.y,detail.z*sign(baseNormal.z));
            float3 tangent=normalize(WorldNormalVector(IN,float3(1,0,0)));
            float3 bitangent=normalize(WorldNormalVector(IN,float3(0,1,0)));
            o.Normal=float3(dot(worldDetail,tangent),dot(worldDetail,bitangent),dot(worldDetail,baseNormal));
            float variation=sin(IN.worldPos.x*.17+sin(IN.worldPos.z*.093))*sin(IN.worldPos.z*.21+IN.worldPos.y*.13);
            float macro=1+variation*.12*_Weathering;
            float baseDirt=lerp(1,lerp(.69,1,smoothstep(.12,2.6,IN.worldPos.y)),_Weathering*(1-axis.y));
            o.Albedo=tex2D(_MainTex,uv).rgb*_Color.rgb*lerp(half3(1,1,1),IN.color.rgb,_VertexTint)*macro*baseDirt;
            half4 mask=tex2D(_MetallicGlossMap,uv);
            o.Metallic=mask.r;
            o.Smoothness=saturate(mask.a*_GlossMapScale*(1-variation*.18));
            o.Occlusion=lerp(1,tex2D(_OcclusionMap,uv).g,_OcclusionStrength);
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
