Shader "CarCrash/ArchitecturalGlass"
{
    Properties
    {
        _MainTex ("Window coordinates",2D) = "white" {}
        _Color ("Glass tint", Color) = (.13,.17,.2,1)
        _Glossiness ("Smoothness", Range(0,1)) = .89
        _RoomLight ("Room light", Range(0,1)) = .1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular fullforwardshadows addshadow
        fixed4 _Color;
        half _Glossiness,_RoomLight;
        struct Input { float2 uv_MainTex; float3 viewDir; float3 worldPos; };
        void surf(Input IN,inout SurfaceOutputStandardSpecular o)
        {
            float2 uv=saturate(IN.uv_MainTex);
            float3 ray=normalize(float3(IN.viewDir.xy,-abs(IN.viewDir.z)));
            float3 origin=float3(uv*2-1,1);
            float3 invRay=sign(ray)/max(abs(ray),.001);
            float3 hit=abs(invRay)-origin*invRay;
            float travel=min(min(hit.x,hit.y),hit.z);
            float3 room=origin+ray*travel;
            float variation=frac(sin(dot(floor(IN.worldPos.xz*.32)+floor(IN.worldPos.y*.34),float2(12.9898,78.233)))*43758.54);
            float floorShade=lerp(.22,.65,saturate(room.y*.5+.5));
            float ceiling=step(.98,room.y);
            float wall=step(.98,abs(room.x));
            float back=step(room.z,-.98);
            half3 interior=lerp(half3(.09,.13,.17),half3(.19,.22,.23),variation)*floorShade;
            interior*=lerp(.63,1,back);interior*=lerp(1,.72,wall);interior*=lerp(1,.64,ceiling);
            float blind=step(.96,frac(uv.y*12))*step(.84,variation);
            interior*=1-blind*.47;
            float curtain=step(.77,abs(uv.x*2-1))*step(.68,variation);
            interior=lerp(interior,half3(.24,.23,.21),curtain*.4);
            o.Albedo=lerp(interior,_Color.rgb,.36);
            o.Specular=half3(.055,.057,.06);
            o.Smoothness=_Glossiness;
            o.Emission=interior*_RoomLight;
            o.Occlusion=.74;
            o.Alpha=1;
        }
        ENDCG
    }
    FallBack "Standard"
}
