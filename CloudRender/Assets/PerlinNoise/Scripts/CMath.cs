using UnityEngine;
using System.Collections;

namespace CustomMath
{
	public class CMath {

		// Use this for integer stepped ranges, ie Value-Noise/Perlin noise functions.
		static float HASHSCALE1 = 0.1031f;
		static Vector3 HASHSCALE3 = new Vector3(0.1031f, 0.1030f, 0.0973f);
		static Vector4 HASHSCALE4 = new Vector4(1031f, .1030f, .0973f, .1099f);
		// For smaller input rangers like audio tick or 0-1 UVs use these...
		//static float HASHSCALE1 = 443.8975f;
		//static Vector3 HASHSCALE3 = new Vector3(443.897f, 441.423f, 437.195f);
		//static Vector4 HASHSCALE4 = new Vector4(443.897f, 441.423f, 437.195f, 444.129f);

		public static float Fmod(float x, float y)
		{
			float t = x / y;
			return x - Mathf.Floor (t)*y;
		}

		public static Vector3 Fmod(Vector3 x, float y)
		{
			Vector3 t = x / y;
			return x - new Vector3 (Mathf.Floor(t.x), Mathf.Floor(t.y), Mathf.Floor(t.z))*y;
		}

		public static float Fract(float x)
		{
			return x - Mathf.Floor(x);
		}

		public static Vector2 Fract(Vector2 p)
		{
			return p - Floor (p);
		}

		public static Vector3 Fract(Vector3 p)
		{
			return p - Floor (p);
		}

		public static Vector2 Sin(Vector2 p)
		{
			return new Vector2(Mathf.Sin(p.x), Mathf.Sin(p.y));
		}

		public static Vector3 Sin(Vector3 p)
		{
			return new Vector3(Mathf.Sin (p.x), Mathf.Sin (p.y), Mathf.Sin (p.z));
		}

		public static Vector3 Floor(Vector3 x)
		{
			return new Vector3 (Mathf.Floor(x.x), Mathf.Floor(x.y), Mathf.Floor(x.z));
		}
		
		public static Vector2 Floor(Vector2 f)
		{
			return new Vector2 (Mathf.Floor(f.x), Mathf.Floor(f.y));
		}
		//1 out; 1 in
		public static float Hash11(float p)
		{
			Vector3 p3  = Fract(new Vector3(p, p, p) * HASHSCALE1);
			Vector3 pTem = new Vector3(p3.y+ 19.19f, p3.z+ 19.19f, p3.x+ 19.19f);
			float d = Vector3.Dot(p3, pTem);
			p3 += new Vector3(d, d, d);
			return Fract((p3.x + p3.y) * p3.z);
		}
		//1 out; 1 in
		public static float HashOld11( float n ) 
		{ 
			return Fract(Mathf.Sin(n)*753.5453123f); 
		}
		//2 out; 1 in
		public static Vector2 Hash21(float p)
		{
			Vector3 p3 = Fract(p * HASHSCALE3);
			Vector3 pTem = new Vector3(p3.y+ 19.19f, p3.z+ 19.19f, p3.x+ 19.19f);
			float d = Vector3.Dot(p3, pTem);
			p3 += new Vector3(d, d, d);
			return Fract(new Vector2((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y));
		}
		public static Vector2 Hash22(Vector2 p)
		{
			Vector3 tp = new Vector3(p.x, p.y, p.x);
			Vector3 p3 = Fract(new Vector3(tp.x * HASHSCALE3.x, tp.y * HASHSCALE3.y, tp.z * HASHSCALE3.z));
			Vector3 tp3 = new Vector3(p3.y, p3.z, p3.x);
			float d = Vector3.Dot(p3, tp3+new Vector3(19.19f, 19.19f, 19.19f));
			p3 += new Vector3(d, d, d);
			return Fract(new Vector2((p3.x + p3.y)*p3.z, (p3.x+p3.z)*p3.y));
		}
		//2 out; 2 in
		public static Vector2 HashOld22(Vector2 p)
		{
			p = new Vector2( Vector2.Dot(p,new Vector2(127.1f, 311.7f)),
			                Vector2.Dot(p,new Vector2(269.5f,183.3f)));
			
			return new Vector2( -1, -1) + 2.0f * Fract(Sin(p)*43758.5453123f);
		}

	}
}
