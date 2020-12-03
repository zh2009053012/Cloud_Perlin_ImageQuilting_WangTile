// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Noise"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PermutationTex("permutation Texture", 2D) = ""{}
		_GradientTex("Gradient Texture", 2D) = ""{}
		_E("e", float) = 0.01
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" "LightMode"="ForwardBase"}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwb
			//#pragma target 2.0
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal:NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normal:TEXCOORD1;
				float3 lightDir:TEXCOORD2;
				float3 pos:TEXCOORD3;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler1D _PermutationTex;
			sampler1D _GradientTex;
			
			float _E;

			float3 Fade(float3 t)
			{
				return t*t*t*(t*(t*6-15)+10);
				//return t*t*t*(3-2*t);
			}

			float Permutation(float x)
			{
				return tex1D(_PermutationTex, x/255.0).a*255;
			}

			float Gradient(float x, float3 p)
			{
				return dot(tex1D(_GradientTex, x).xyz*2-1, p);
			}

			float Noise3D(float3 np)
			{
				float3 p = fmod(floor(np), 256.0);
			
				np -= floor(np);
				
				float3 f = Fade(np);

				float A = Permutation(p.x) + p.y;
				float AA = Permutation(A) + p.z;
				float AB = Permutation(A + 1) + p.z;
				float B = Permutation(p.x+1) + p.y;
				float BA = Permutation(B) + p.z;
				float BB = Permutation(B + 1) + p.z;

				float corner1 = Gradient(Permutation(AA), np);
				float corner2 = Gradient(Permutation(BA), np+float3(-1, 0, 0));
				float corner3 = Gradient(Permutation(AB), np+float3(0, -1, 0));
				float corner4 = Gradient(Permutation(BB), np+float3(-1, -1, 0));
				float corner5 = Gradient(Permutation(AA+1), np+float3(0, 0, -1));
				float corner6 = Gradient(Permutation(BA+1), np+float3(-1, 0, -1));
				float corner7 = Gradient(Permutation(AB+1), np+float3(0, -1, -1));
				float corner8 = Gradient(Permutation(BB+1), np+float3(-1, -1, -1));

				return lerp( lerp( lerp( corner1, corner2, f.x ), lerp( corner3, corner4, f.x ), f.y ),
							 lerp( lerp( corner5, corner6, f.x ), lerp( corner7, corner8, f.x ), f.y ),
							 f.z );
				
			}

			

			float F(float x, float y, float z)
			{
				return Noise3D(float3(x, y, z));
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.pos = mul(unity_ObjectToWorld, v.vertex);
				o.normal = v.normal;
				o.lightDir = ObjSpaceLightDir(v.vertex);

				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float4 col = tex2D(_MainTex, i.uv);
				//noise
				float e = _E;
				float f0 = F(i.pos.x, i.pos.y, i.pos.z);
				float fx = F(i.pos.x+e, i.pos.y, i.pos.z);
				float fy = F(i.pos.x, i.pos.y+e, i.pos.z);
				float fz = F(i.pos.x, i.pos.y, i.pos.z+e);
				float3 df = float3((fx-f0)/e, (fy-f0)/e, (fz-f0)/e);
				float3 nor = normalize(i.normal - df);
				//col.xyz = max(0, dot(nor, i.lightDir));
				//col.xyz = Noise3D(i.pos.xyz)*0.5+0.5;
				float3 p = i.pos.xyz + float3((_Time.y), 0, (_Time.y));
				col.xyz = Noise3D(p) * 0.5f + 0.5f;
				return col;
			}
			ENDCG
		}
	}
}
