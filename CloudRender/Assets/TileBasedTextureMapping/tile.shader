Shader "Unlit/tile"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Count("图块平铺数量", float) = 64

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Count;
            float _ColorType[256];
            float _AtlasIndex1D[4];

            float AtlasIndex1D(int e1, int e2) 
            {
                int index = e1 * 2 + e2;
                return _AtlasIndex1D[index];
            }

            int2 Hash(int e1, int e2)
            {
                float2 v = float2(e1 * 0.01f, e2 * 0.01f);
                v = frac(v);
                float noise = frac(sin(dot(v, float2(12.9898, 78.233))) * 43758.5453);
                int c = floor(noise * 3.99f);
                return int2(c >> 1, c & 1);
                //return _ColorType[index % 256];
            }

            int2 Hash2D(int e1, int e2) 
            {
                int c = _ColorType[e1 % 16 * 16 + e2 % 16];
                return int2(c >> 1, c & 1);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 TileIndex(float2 uv) 
            {
                return floor(uv * _Count % _Count);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                //1、计算i.uv所位于的图块坐标
                float2 O = TileIndex(i.uv);
                //2、使用O.xy来随机输出四个边的颜色
                /*int Cs = Hash(Hash(O.x) + O.y);
                int Ce = Hash((O.x + 1) % _Count + Hash(2 * O.y));
                int Cn = Hash(Hash(O.x) + (O.y + 1) % _Count);
                int Cw = Hash(O.x + Hash(2 * O.y));*/
                int2 CwCs = Hash(O.x, O.y);
                int Ce = Hash(O.x + 1, O.y).x;
                int Cn = Hash(O.x, O.y + 1).y;
                int Cw = CwCs.x;
                int Cs = CwCs.y;
                
                //3、计算图集索引
                int2 Index = int2(AtlasIndex1D(Cw, Ce), AtlasIndex1D(Cs, Cn));
                //4、知道了图集内的哪个图块，需要计算出i.uv落在这个图块上的相对uv
                float2 Theta = frac(i.uv * _Count);
                //5、从图集上取像素
                float4 ue = tex2D(_MainTex, (Index + Theta) / 4, ddx(1.0f/_Count), ddy(1.0f / _Count));
               
                col.xyz = ue.xyz;
               
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
