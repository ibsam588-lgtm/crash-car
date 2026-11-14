Shader "Hidden/CarCrash/CinematicGrade"
{
    Properties { _MainTex ("Scene", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex,_AOTexture,_BloomTexture;
        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
        float4 _MainTex_TexelSize,_AOTexture_TexelSize,_BlurDirection;
        float _OcclusionStrength,_OcclusionRadius,_BloomStrength;
        float depthAt(float2 uv)
        {
            #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0)uv.y=1-uv.y;
            #endif
            return LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv));
        }
        float3 positionAt(float2 uv,float depth)
        {
            float2 p=uv*2-1;
            p.y*=_ProjectionParams.x;
            return float3(p.x/unity_CameraProjection._m00,p.y/unity_CameraProjection._m11,-1)*depth;
        }
        float4 aoPass(v2f_img i):SV_Target
        {
            float depth=depthAt(i.uv);
            if(depth>100 || depth<.15)return 1;
            float3 p=positionAt(i.uv,depth);
            float3 n=normalize(cross(ddx(p),ddy(p)));
            if(dot(n,-p)<0)n=-n;
            float2 projected=float2(unity_CameraProjection._m00,abs(unity_CameraProjection._m11))*_OcclusionRadius/max(depth,1)*.5;
            projected=min(projected,float2(.08,.08));
            float rotation=frac(dot(floor(i.uv/abs(_MainTex_TexelSize.xy)),float2(.06711056,.00583715)))*6.283185;
            float occlusion=0;
            [unroll]for(int tap=0;tap<8;tap++)
            {
                float angle=rotation+tap*2.399963;
                float radius=.25+tap*.105;
                float2 uv=saturate(i.uv+float2(cos(angle),sin(angle))*projected*radius);
                float3 delta=positionAt(uv,depthAt(uv))-p;
                float distance=length(delta);
                occlusion+=max(0,dot(n,delta)/max(distance,.001)-.12)*saturate(1-distance/_OcclusionRadius);
            }
            float result=saturate(1-occlusion*.42*_OcclusionStrength);
            return float4(result,result,result,1);
        }
        float4 bloomPass(v2f_img i):SV_Target
        {
            float3 c=tex2D(_MainTex,i.uv).rgb;
            float peak=max(c.r,max(c.g,c.b));
            return float4(c*saturate((peak-1.05)/max(peak,.001)),1);
        }
        float4 blurPass(v2f_img i):SV_Target
        {
            float3 c=tex2D(_MainTex,i.uv).rgb*.227027;
            c+=(tex2D(_MainTex,i.uv+_BlurDirection.xy*1.384615).rgb+tex2D(_MainTex,i.uv-_BlurDirection.xy*1.384615).rgb)*.316216;
            c+=(tex2D(_MainTex,i.uv+_BlurDirection.xy*3.230769).rgb+tex2D(_MainTex,i.uv-_BlurDirection.xy*3.230769).rgb)*.070270;
            return float4(c,1);
        }
        float3 aces(float3 x){return saturate((x*(2.51*x+.03))/(x*(2.43*x+.59)+.14));}
        float4 compositePass(v2f_img i):SV_Target
        {
            float depth=depthAt(i.uv);
            float ao=tex2D(_AOTexture,i.uv).r*2;float weight=2;
            [unroll]for(int tap=0;tap<4;tap++)
            {
                float2 direction=tap==0 ? float2(1,0) : tap==1 ? float2(-1,0) : tap==2 ? float2(0,1) : float2(0,-1);
                float2 uv=i.uv+direction*_AOTexture_TexelSize.xy;
                float w=saturate(1-abs(depthAt(uv)-depth)/max(.12,depth*.012));
                ao+=tex2D(_AOTexture,uv).r*w;weight+=w;
            }
            ao/=weight;
            float3 scene=tex2D(_MainTex,i.uv).rgb*lerp(1,ao,.78);
            scene+=tex2D(_BloomTexture,i.uv).rgb*_BloomStrength;
            scene=aces(scene*1.08);
            float luma=dot(scene,float3(.2126,.7152,.0722));
            scene=lerp(luma.xxx,scene,.98);
            float2 edge=i.uv*(1-i.uv.yx);
            scene*=lerp(.94,1,pow(saturate(edge.x*edge.y*16),.25));
            return float4(scene,1);
        }
        ENDCG
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment aoPass
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment bloomPass
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment blurPass
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert_img
            #pragma fragment compositePass
            ENDCG
        }
    }
}
