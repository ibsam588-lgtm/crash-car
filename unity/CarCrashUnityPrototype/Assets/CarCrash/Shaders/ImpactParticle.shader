Shader "CarCrash/ImpactParticle"
{
    Properties { _Color ("Tint", Color) = (1,1,1,1) _Ring ("Ring", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct input { float4 vertex: POSITION; fixed4 color: COLOR; float2 uv: TEXCOORD0; };
            struct output { float4 position: SV_POSITION; fixed4 color: COLOR; float2 uv: TEXCOORD0; };
            fixed4 _Color;
            float _Ring;
            output vert(input v) { output o; o.position=UnityObjectToClipPos(v.vertex); o.color=v.color*_Color; o.uv=v.uv; return o; }
            fixed4 frag(output i): SV_Target
            {
                float r=length(i.uv*2-1);
                float disc=pow(saturate(1-r*r), 1.6);
                float ring=smoothstep(0.45,0.65,r)*(1-smoothstep(0.78,1,r));
                i.color.a*=lerp(disc,ring,_Ring);
                return i.color;
            }
            ENDCG
        }
    }
}
