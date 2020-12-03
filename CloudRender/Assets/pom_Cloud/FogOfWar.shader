Shader "Unlit/FogOfWar"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaskTex("Mask Texture", 2D) = "white"{}
        _NoiseTex("Noise Texture", 2D) = "white"{}
        _Level("Lod Level", int) = 0
        _Test("Test", Range(0, 1)) = 0.5
        _TileOffset("Tile & Offset", vector) = (1, 1, 1, 1)
        _MoveDirection("Move Direction", vector) = (1, 1, 0, 0)
        _MapSize("MapSize", float) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite off

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
                float4 uv : TEXCOORD0;
                float4 noiseUV : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            int _Level;
            float4 _TileOffset;
            float4 _MoveDirection;
            float _Test;
            float _MapSize;

            v2f vert (appdata v)
            {
                v2f o;
                half3 worldPos = mul(unity_ObjectToWorld, v.vertex);
                half2 move = _Time.y * _MoveDirection.xy;
                o.uv.xy = worldPos.xz * _TileOffset.x - move;
                o.uv.zw = worldPos.xz * _TileOffset.y + _TileOffset.zw - move;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.noiseUV.xy = TRANSFORM_TEX(v.uv, _NoiseTex) - move;
                o.noiseUV.zw = worldPos.xz / _MapSize; //mask uv

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 col = tex2D(_MainTex, i.uv.xy);
                half4 col2 = tex2D(_MainTex, i.uv.zw);
                fixed4 mask = tex2Dlod(_MaskTex, float4(i.noiseUV.zw, 0, _Level));
                float noise = tex2D(_NoiseTex, i.noiseUV.xy).r;

                col.xyz = lerp(col.xyz, col2.xyz, noise);

                col.a = step(_Test, mask.a + noise);
                col.a *= step(1, mask.a) + mask.a * (noise+mask.a);
                col.a = saturate(col.a);

                return col;
            }
            ENDCG
        }
    }
}
