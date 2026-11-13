Shader "Hidden/CarCrash/CinematicGrade"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float3 aces(float3 x) { return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14)); }
            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 scene = tex2D(_MainTex, i.uv).rgb;
                float2 step = _MainTex_TexelSize.xy * 3.0;
                float3 glow = max(0, tex2D(_MainTex, i.uv + float2(step.x, 0)).rgb - 1.3);
                glow += max(0, tex2D(_MainTex, i.uv - float2(step.x, 0)).rgb - 1.3);
                glow += max(0, tex2D(_MainTex, i.uv + float2(0, step.y)).rgb - 1.3);
                glow += max(0, tex2D(_MainTex, i.uv - float2(0, step.y)).rgb - 1.3);
                scene = aces(scene * 0.93 + glow * 0.022);
                float luma = dot(scene, float3(0.2126, 0.7152, 0.0722));
                scene = lerp(luma.xxx, scene, 0.94);
                float2 edge = i.uv * (1.0 - i.uv.yx);
                scene *= lerp(0.90, 1.0, pow(saturate(edge.x * edge.y * 16.0), 0.25));
                return float4(scene, 1);
            }
            ENDCG
        }
    }
}
