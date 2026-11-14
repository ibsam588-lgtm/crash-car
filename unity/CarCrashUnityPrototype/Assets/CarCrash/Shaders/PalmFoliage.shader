Shader "CarCrash/PalmFoliage"
{
    Properties { _Color("Leaf colour",Color)=(.12,.24,.075,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf Standard fullforwardshadows addshadow vertex:wind nolightmap nodynlightmap noforwardadd
        #pragma instancing_options assumeuniformscaling
        fixed4 _Color;
        struct Input { fixed4 color:COLOR; float facing:VFACE; };
        void wind(inout appdata_full v)
        {
            float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
            float sway=sin(world.x*.45+world.z*.32+_Time.y*1.1)*.055*v.texcoord.y;
            v.vertex.xyz+=mul((float3x3)unity_WorldToObject,float3(sway,sway*.22,sway*.44));
        }
        void surf(Input IN,inout SurfaceOutputStandard o)
        {
            o.Albedo=_Color.rgb*IN.color.rgb;
            o.Normal=float3(0,0,IN.facing>=0 ? 1 : -1);
            o.Smoothness=.2;o.Occlusion=.8;
            o.Emission=o.Albedo*.045;o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
