Shader "Hidden/ChipChallengers/MenuBackgroundBlur"
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
            float2 _BlurDirection;

            fixed4 frag(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv) * 0.2270270270;
                color += tex2D(_MainTex, input.uv + _BlurDirection) * 0.1945945946;
                color += tex2D(_MainTex, input.uv - _BlurDirection) * 0.1945945946;
                color += tex2D(_MainTex, input.uv + _BlurDirection * 2.0) * 0.1216216216;
                color += tex2D(_MainTex, input.uv - _BlurDirection * 2.0) * 0.1216216216;
                color += tex2D(_MainTex, input.uv + _BlurDirection * 3.0) * 0.0540540541;
                color += tex2D(_MainTex, input.uv - _BlurDirection * 3.0) * 0.0540540541;
                color += tex2D(_MainTex, input.uv + _BlurDirection * 4.0) * 0.0162162162;
                color += tex2D(_MainTex, input.uv - _BlurDirection * 4.0) * 0.0162162162;
                return color;
            }
            ENDCG
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment downsample
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 downsample(v2f_img input) : SV_Target
            {
                float2 offset = abs(_MainTex_TexelSize.xy) * 0.5;
                fixed4 color = tex2D(_MainTex, input.uv + float2(-offset.x, -offset.y));
                color += tex2D(_MainTex, input.uv + float2(offset.x, -offset.y));
                color += tex2D(_MainTex, input.uv + float2(-offset.x, offset.y));
                color += tex2D(_MainTex, input.uv + offset);
                return color * 0.25;
            }
            ENDCG
        }
    }
    Fallback Off
}
