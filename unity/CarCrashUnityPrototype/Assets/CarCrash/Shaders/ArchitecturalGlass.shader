Shader "CarCrash/ArchitecturalGlass"
{
    Properties
    {
        _MainTex ("Window coordinates",2D) = "white" {}
        _Color ("Glass tint", Color) = (.13,.17,.2,1)
        _Glossiness ("Smoothness", Range(0,1)) = .89
        _RoomLight ("Room light", Range(0,1)) = .1
        _Shopfront ("Shop interior", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250
        CGPROGRAM
        #pragma target 3.0
        #pragma surface surf StandardSpecular fullforwardshadows addshadow
        fixed4 _Color;
        half _Glossiness,_RoomLight,_Shopfront;
        struct Input { float2 uv_MainTex; float3 viewDir; float3 worldPos; };
        void surf(Input IN,inout SurfaceOutputStandardSpecular o)
        {
            float2 uv=saturate(IN.uv_MainTex);
            float3 ray=normalize(float3(-IN.viewDir.xy,-abs(IN.viewDir.z)));
            float3 origin=float3(uv*2-1,.999);
            float3 invRay=(step(0,ray)*2-1)/max(abs(ray),.001);
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
            float2 roomUv=saturate(room.xy*.5+.5);
            float2 floorUv=room.xz*.5+.5;
            float floorGrid=step(.965,frac(floorUv.x*5))+step(.965,frac(floorUv.y*6));
            interior*=1-saturate(floorGrid)*step(room.y,-.98)*.25;
            half3 shop=lerp(half3(.19,.16,.13),half3(.43,.36,.27),roomUv.y);
            float shelf=step(.945,frac(roomUv.y*4))*step(.15,roomUv.x)*step(roomUv.x,.87)*back;
            float uprights=step(.97,frac(roomUv.x*5))*step(.15,roomUv.y)*step(roomUv.y,.89)*back;
            float products=step(.24,frac(roomUv.x*18))*step(frac(roomUv.x*18),.78)*step(.30,frac(roomUv.y*4))*step(frac(roomUv.y*4),.7)*back;
            shop=lerp(shop,half3(.085,.065,.045),saturate(shelf+uprights));
            shop=lerp(shop,lerp(half3(.24,.28,.22),half3(.44,.26,.15),frac(roomUv.x*7)),products*.55);
            shop*=lerp(.56,1,back);shop*=lerp(1,.66,ceiling);
            interior=lerp(interior,shop,_Shopfront);
            float blind=step(.96,frac(uv.y*12))*step(.84,variation);
            interior*=1-blind*.47*(1-_Shopfront);
            float curtain=step(.77,abs(uv.x*2-1))*step(.68,variation);
            interior=lerp(interior,half3(.24,.23,.21),curtain*.4*(1-_Shopfront));
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
