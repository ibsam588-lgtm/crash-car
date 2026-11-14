Shader "CarCrash/CoastalWater"
{
    Properties { _Color ("Water color",Color)=(0.04,0.22,0.28,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        struct Input { float3 worldPos; };
        fixed4 _Color;
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            float wave=sin(IN.worldPos.x*.73+IN.worldPos.z*.31+_Time.y*.8);
            float crossWave=cos(IN.worldPos.z*.91-IN.worldPos.x*.17-_Time.y*.65);
            o.Albedo=_Color.rgb*(.85+wave*.12+crossWave*.05);
            o.Normal=normalize(float3(wave*.14,crossWave*.09,1));
            o.Metallic=.18;o.Smoothness=.82;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
