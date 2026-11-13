Shader "UI/ChipChallengers/FrostedImage"
{
    Properties
    {
        [PerRendererData] _MainTex ("Shape", 2D) = "white" {}
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screen : TEXCOORD1;
                float4 local : TEXCOORD2;
            };
            sampler2D _MainTex;
            sampler2D _PanelBlurTexture;
            float4 _ClipRect;
            float _TintOpacity;
            fixed4 _TextureSampleAdd;
            v2f vert(appdata input)
            {
                v2f output;
                output.local = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screen = ComputeScreenPos(output.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 scene = tex2Dproj(_PanelBlurTexture, UNITY_PROJ_COORD(input.screen));
                fixed shape = (tex2D(_MainTex, input.uv) + _TextureSampleAdd).a;
                fixed opacity = _TintOpacity > 0.0001 ? saturate(input.color.a / _TintOpacity) : 0;
                fixed4 color = fixed4(lerp(scene.rgb, input.color.rgb, _TintOpacity), shape * opacity);
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
