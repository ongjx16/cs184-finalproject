Shader "Unlit/GrassPNG" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        Zwrite On
        Cull Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma target 4.5

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float saturationLevel : TEXCOORD1;
            };

            struct GrassData {
                float4 position;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<GrassData> positionBuffer;
            float _Rotation;
            
            float4 RotateAroundYInDegrees(float4 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
            
                float3 localPosition = RotateAroundYInDegrees(v.vertex, _Rotation).xyz;
                float4 grassPosition = positionBuffer[instanceID].position;
                localPosition.y *= v.uv.y * (0.5f + grassPosition.w);
                
                float4 worldPosition = float4(grassPosition.xyz + localPosition, 1.0f);

                o.vertex = UnityObjectToClipPos(worldPosition);

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.saturationLevel = 1.0 - ((positionBuffer[instanceID].position.w - 1.0f) / 1.5f);
                o.saturationLevel = max(o.saturationLevel, 0.5f);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(-(0.5 - col.a));

                float luminance = LinearRgbToLuminance(col);

                float saturation = lerp(1.0f, i.saturationLevel, i.uv.y * i.uv.y * i.uv.y);
                col.r /= saturation;
                
             
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float ndotl = DotClamped(lightDir, normalize(float3(0, 1, 0)));
                
                return col * ndotl;
            }

            ENDCG
        }
    }
}
