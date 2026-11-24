Shader "CarCrash/ElysianNightSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct SkyVertex { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            SkyVertex vert(float4 vertexPosition : POSITION)
            {
                SkyVertex result;
                result.position = UnityObjectToClipPos(vertexPosition);
                result.direction = vertexPosition.xyz;
                return result;
            }

            fixed4 frag(SkyVertex input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float above = saturate(direction.y);
                float3 sky = lerp(float3(.057,.085,.148), float3(.004,.008,.025), smoothstep(0,.72,above));
                float2 field = float2(atan2(direction.z,direction.x) * 39.0, above * 108.0);
                float2 cell = floor(field);
                float salt = frac(sin(dot(cell,float2(18.42,64.13))) * 4271.65);
                float star = (1.0 - smoothstep(.042,.106,length(frac(field) - float2(.49,.52)))) * step(.982,salt);
                float moonAngle = dot(direction,normalize(float3(-.63,.42,.71)));
                float halo = smoothstep(.993,1.0,moonAngle) * .15;
                float moon = smoothstep(.99940,.99948,moonAngle);
                sky += float3(.66,.78,.98) * (star * smoothstep(.13,.41,above) * .62 + halo + moon * .8);
                return fixed4(sky,1);
            }
            ENDCG
        }
    }
}
