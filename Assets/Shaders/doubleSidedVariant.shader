Shader "Custom/DoubleSidedVariant"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        // Adjust queue and render type if most of the object uses significant transparency
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        Cull Off
        ZWrite On // Keep ZWrite On for opaque parts, might need to dynamically adjust for fully transparent objects
        Blend SrcAlpha OneMinusSrcAlpha // Enable blending

        Pass
        {
            Name "DepthPrePass"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ColorMask 0
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
            #pragma target 3.0
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
            fixed4 _Color;

            StructuredBuffer<float3> _Positions;

            // Vertex shader remains the same
            v2f vert (appdata v, uint id : SV_InstanceID)
            {
                v2f o;
                float3 position = _Positions[id];
                v.vertex.xyz += position;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 fragDepth() : SV_Target {
                return fixed4(1, 0, 0, 0);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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
            fixed4 _Color;

            StructuredBuffer<float3> _Positions;

            v2f vert (appdata v, uint id : SV_InstanceID)
            {
                v2f o;
                float3 position = _Positions[id];
                v.vertex.xyz += position;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv); // Sample the texture
                col *= _Color; // Apply color tint
                // Ensure the alpha is respected by not forcing it to 1.0
                return col; // Output color with texture alpha
            }
            ENDCG
        }
    }
    FallBack "Transparent"
}