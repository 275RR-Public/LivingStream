Shader "Custom/WaterRippleShader"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0.5, 1, 1)  // Default Blue
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.3
        _RippleSpeed ("Ripple Speed", Range(0, 5)) = 1.4
        _ScrollSpeed ("Scroll Speed", Range(0, 5)) = 0.03  // New Scroll Speed property
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            // Remove blending setup for full opacity
            Tags { "Queue"="Overlay" }
            ZWrite On  // Ensure depth writing is enabled (opaque objects are written to depth buffer)
            
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
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _RippleStrength;
            float _RippleSpeed;
            float _ScrollSpeed;   // Scroll Speed variable

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Apply ripple effect
                float ripple = sin(v.vertex.x * 5.0 + _Time.y * _RippleSpeed) * _RippleStrength;
                o.pos.y += ripple;

                // Move the UV coordinates for scrolling effect (moving from left to right)
                o.uv.x += _Time.y * _ScrollSpeed; // Move UV in the x direction over time

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Return color without transparency
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
}
