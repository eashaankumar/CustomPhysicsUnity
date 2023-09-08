Shader "Unlit/SpriteShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [KeywordEnum(Circle, Box)] _Type("Type", Int) = 0
        _Glow ("Glow", Float) = 0
        _Color ("Color", Color) = (1,1,1,1)
        _EdgeWidth("Edge Width", Float) = 0
        _EdgeColor("Edge Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Glow;
            float4 _Color;
            float4 _EdgeColor;
            float _EdgeWidth;
            int _Type;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                if (i.uv.y > 0.5 && i.uv.x > (0.5 - _EdgeWidth / 2) && i.uv.x < (0.5 + _EdgeWidth / 2)) return _EdgeColor;
                if (_Type == 0)
                {
                    float2 pointInCircle = i.uv - 0.5f;
                    float len = length(pointInCircle);
                    if (len > 0.5) discard;
                    else if (len > 0.5 - _EdgeWidth) return _EdgeColor;
                    return _Glow * _Color;
                }
                else
                {
                    if (i.uv.x < _EdgeWidth || i.uv.x > 1- _EdgeWidth ||
                        i.uv.y < _EdgeWidth || i.uv.y > 1 - _EdgeWidth) return _EdgeColor;
                    return _Glow * _Color;
                }
            }
            ENDCG
        }
    }
}
