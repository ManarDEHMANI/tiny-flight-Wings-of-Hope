Shader "Custom/SkyboxGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.5, 0.8, 1, 1)
        _BottomColor ("Bottom Color", Color) = (0.1, 0.2, 0.4, 1)
        _Exponent ("Gradient Exponent", Range(1, 10)) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Background" }
        Pass
        {
            ZWrite Off
            Cull Off
            Fog { Mode Off }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _BottomColor;
            float _Exponent;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float height = normalize(i.worldPos).y * 0.5 + 0.5; // Convertir Y en [0,1]
                height = pow(height, _Exponent); // Appliquer l'exposant
                return lerp(_BottomColor, _TopColor, height);
            }
            ENDCG
        }
    }
}
