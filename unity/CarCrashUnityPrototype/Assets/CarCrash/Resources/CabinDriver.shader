Shader "CarCrash/CabinDriver"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        CGPROGRAM
        #pragma target 2.5
        #pragma surface surf Lambert nolightmap nodynlightmap noforwardadd noshadow

        struct Input { fixed4 color : COLOR; };
        void surf(Input input, inout SurfaceOutput output)
        {
            output.Albedo = input.color.rgb;
            output.Emission = input.color.rgb * .055;
            output.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
