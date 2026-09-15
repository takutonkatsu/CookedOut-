Shader "CookedOut/CircularIngredientCard"
{
    Properties
    {
        _MainTex ("IngredientSourceCards Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ContentScale ("Ingredient Fill Scale", Range(1.0, 1.35)) = 1.0
        _BackgroundRadius ("Background Circle Radius", Range(0.30, 0.50)) = 0.485
        _SubjectOverflow ("Allow Ingredient Overflow", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _ContentScale;
            float _BackgroundRadius;
            float _SubjectOverflow;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 sampleUv = (input.uv - float2(0.5, 0.5)) / max(_ContentScale, 0.001) +
                                  float2(0.5, 0.5);
                fixed4 color = tex2D(_MainTex, saturate(sampleUv)) * _Color;

                // The generated source cards share a clean studio backdrop. Use
                // their four corners to distinguish that backdrop from the food,
                // so the food can sit tightly on and slightly beyond the circle.
                fixed3 backdrop = (
                    tex2D(_MainTex, float2(0.04, 0.04)).rgb +
                    tex2D(_MainTex, float2(0.96, 0.04)).rgb +
                    tex2D(_MainTex, float2(0.04, 0.96)).rgb +
                    tex2D(_MainTex, float2(0.96, 0.96)).rgb) * 0.25;
                float subjectDifference = length(color.rgb - backdrop);
                float subjectMask = smoothstep(0.065, 0.135, subjectDifference);
                float radius = distance(input.uv, float2(0.5, 0.5));
                float circleMask = 1.0 - smoothstep(
                    _BackgroundRadius - 0.015,
                    _BackgroundRadius,
                    radius);
                float quadEdgeMask = 1.0 - smoothstep(0.49, 0.505, radius);
                float overflowMask = subjectMask * quadEdgeMask * _SubjectOverflow;
                color.a *= max(circleMask, overflowMask);
                clip(color.a - 0.001);
                return color;
            }
            ENDCG
        }
    }
}
