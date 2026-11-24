Shader "CarCrash/RoadTurnPaint"
{
    Properties { _Color ("Color", Color) = (1,.7,.11,1) }
    SubShader
    {
        Tags { "Queue"="Geometry+12" "RenderType"="Opaque" }
        Cull Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            float4 vert(float4 vertex : POSITION) : SV_POSITION { return UnityObjectToClipPos(vertex); }
            fixed4 frag() : SV_Target { return _Color; }
            ENDCG
        }
    }
}
