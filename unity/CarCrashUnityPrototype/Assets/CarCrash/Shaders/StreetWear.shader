Shader "CarCrash/StreetWear"
{
    Properties { _Color("Wear strength",Color)=(.43,.43,.43,1) _Pattern("Pattern",Float)=0 }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend DstColor Zero
        ZWrite Off Cull Off Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;float _Pattern;
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float n=noise(i.uv*13)*.6+noise(i.uv*31)*.4;
                float mask=smoothstep(1,.35,length(p*float2(1,.9))+n*.24);
                if(_Pattern>.5)mask=smoothstep(.24,.10,min(abs(p.x-.47),abs(p.x+.47)))*smoothstep(1,.5,abs(p.y))*lerp(.35,.8,n);
                return float4(lerp(1,_Color.rgb,mask*.7),1);
            }
            ENDCG
        }
    }
}
