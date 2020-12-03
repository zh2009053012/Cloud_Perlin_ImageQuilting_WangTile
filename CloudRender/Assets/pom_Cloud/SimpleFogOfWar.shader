Shader "BC2/Env/SimpleFogOfWar"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _MaskTex("MaskTexture", 2D) = "white"{}
        _Color ("Color", Color) = (1,1,1,1)
        _Height("Height", float) = 0.32
        _TileSize("Tile Size", float) = 0.01
        _MoveDir("Move Direction", vector) = (1,1,1,0)
        _RimPower("RimPower", float) = 4
        _RimIntensity ("RimIntensity",float)=2
        _MapSize ("MapSize", float) = 3600
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "LightMode"="ForwardBase" "Queue"="Transparent"}
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.0
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float3 worldNor : TEXCOORD2;
                float3 worldView : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            sampler2D _MaskTex;
            float4 _MainTex_ST;
            half _Height;
            half _TileSize;
            half4 _Color;
            half4 _MoveDir;
            half4 _PlanetPos;
            half _RimPower;
            half _RimIntensity;
            half _MapSize;
            
            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                half3 worldPos = mul(unity_ObjectToWorld, v.vertex);

                half3 realWorldPos = mul(unity_ObjectToWorld, v.vertex);
                o.worldNor = normalize(realWorldPos - _PlanetPos.xyz);
                o.worldView = normalize(_WorldSpaceCameraPos.xyz - realWorldPos);
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.uv.xy = worldPos.xz * _TileSize +frac(_Time.y * normalize(_MoveDir.xy) * _MoveDir.z);
                o.uv.zw = (worldPos.xz + half2(_MapSize, _MapSize)*0.5f) / _MapSize;
                TANGENT_SPACE_ROTATION;
                o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex));
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 mask = tex2D(_MaskTex, i.uv.zw);
                if (mask.r <= 0) 
                {
                    return half4(1, 1, 1, 0);
                }

                // sample the texture
                half3 view = normalize(-i.viewDir);
                half4 col = tex2D(_MainTex, i.uv.xy);

                half h = 1 - col.r;
                view.z = abs(view.z) + 0.42f;
                view.xy *= _Height;
                half2 v = view.xy / view.z * h;
                col = tex2D(_MainTex, i.uv + v) * _Color;
                
                
                col.w = dot(col.xyz, half3(1,1,1)) * 0.33f + 0.1f;
                //
                fixed rim = 1 - saturate(dot(normalize(i.worldNor), normalize(i.worldView)));
                //col.w *= saturate((1-pow(rim, _RimPower))*_RimIntensity) * mask;

                //UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
