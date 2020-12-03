Shader "Unlit/SimpleParallax"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Height("Height", float) = 1
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

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 viewLight : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _NormalTex;
            float4 _MainTex_ST;
            float _Height;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) +frac(_Time.y * 0.1f);
                TANGENT_SPACE_ROTATION;
                o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex));
                o.viewLight =mul(rotation, ObjSpaceLightDir(v.vertex));
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                float3 view = normalize(-i.viewDir);
                float4 col = tex2D(_MainTex, i.uv);

                float h = 1 - col.r;
                view.z = abs(view.z) + 0.42f;
                view.xy *= _Height;
                float2 v = view.xy / view.z * h;
                col = tex2D(_MainTex, i.uv + v) * _Color;
                //
                /*float3 normal = UnpackNormal(tex2D(_NormalTex, i.uv + v));
                half diff = saturate(dot(normalize(normal), normalize(i.viewLight)));
                diff = diff * 0.7f + 0.3f;
                col.xyz = diff * col.xyz;*/
                //col.xyz = normal;
                return col;
            }
            ENDCG
        }
    }
}
