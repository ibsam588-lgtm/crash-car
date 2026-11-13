Shader "CarCrash/WorldSignText"
{
    Properties { _MainTex ("Font atlas", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct input { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct output { float4 position:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            output vert(input v) { output o; o.position=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
            fixed4 frag(output i):SV_Target { i.color.a*=tex2D(_MainTex,i.uv).a; return i.color; }
            ENDCG
        }
    }
}
