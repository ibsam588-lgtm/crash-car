Shader "CarCrash/PalmBark"
{
    Properties { _Color("Bark colour",Color)=(.22,.16,.10,1) }
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow
        fixed4 _Color;
        struct Input {float3 worldPos;};
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float p=IN.worldPos.y*5.3+sin(IN.worldPos.x*21+IN.worldPos.z*17)*.075;
            float ring=smoothstep(.08,.22,frac(p));
            float fibre=.9+.1*sin(IN.worldPos.y*57+sin(IN.worldPos.z*27));
            o.Albedo=_Color.rgb*lerp(.62,1.08,ring)*fibre;
            o.Normal=normalize(float3(0,sin(p*6.283)*.15,1));
            o.Smoothness=.13;o.Occlusion=.88;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
