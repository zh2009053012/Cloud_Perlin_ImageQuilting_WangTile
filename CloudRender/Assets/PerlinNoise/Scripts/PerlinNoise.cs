using UnityEngine;
using System.Collections;
using CustomMath;

public class PerlinNoise {

	public static int[] permutation = {
		151,160,137,91,90,15, 131,13,201,95,96,53,194,233,7,225,140,
		36,103,30,69,142,8,99,37,240,21,10,23, 190, 6,148,247,120,234,
		75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33, 88,237,149,
		56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166, 
		77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,
		245,40,244, 102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,
		208, 89,18,169,200,196, 135,130,116,188,159,86,164,100,109,198,173,
		186, 3,64,52,217,226,250,124,123, 5,202,38,147,118,126,255,82,85,212,
		207,206,59,227,47,16,58,17,182,189,28,42, 223,183,170,213,119,248,152, 
		2,44,154,163, 70,221,153,101,155,167, 43,172,9, 129,22,39,253, 19,98,108,
		110,79,113,224,232,178,185, 112,104,218,246,97,228, 251,34,242,193,238,210,
		144,12,191,179,162,241, 81,51,145,235,249,14,239,107, 49,192,214, 31,181,199,
		106,157,184, 84,204,176,115,121,50,45,127, 4,150,254, 138,236,205,93,222,114,
		67,29,24,72,243,141,128,195,78,66,215,61,156,180};
	public static Vector3[] gradient_3D = { 
		new Vector3(1,1,0), new Vector3(-1,1,0), new Vector3(1,-1,0), new Vector3(-1,-1,0),
		new Vector3(1,0,1), new Vector3(-1,0,1), new Vector3(1,0,-1), new Vector3(-1,0,-1),
		new Vector3(0,1,1), new Vector3(0,-1,1), new Vector3(0,1,-1), new Vector3(0,-1,-1),
		new Vector3(1,1,0), new Vector3(0,-1,1), new Vector3(-1,1,0), new Vector3(0,-1,-1)};
	static Vector2[] gradient_2D = new Vector2[256];
	static float[] gradient_1D = new float[256];
	static bool m_isInit = false;
	

	static void InitGradient()
	{
		if (m_isInit)
			return;
		for (int i = 0; i < gradient_1D.Length; i++) {
			gradient_1D [i] = CMath.Hash11(i);
		}
		for (int i = 0; i < gradient_2D.Length; i++) {
			gradient_2D[i] = CMath.HashOld22(new Vector2(i, gradient_2D.Length+i));
			gradient_2D[i].Normalize();
		}
		for (int i = 0; i < gradient_3D.Length; i++) {
			gradient_3D [i].Normalize();
		}
		m_isInit = true;
	}

	public static Vector3 Fade(Vector3 t)
	{
		return new Vector3(Fade(t.x), Fade(t.y), Fade(t.z));
	}
	public static Vector2 Fade(Vector2 t)
	{
		return new Vector2 (Fade(t.x), Fade(t.y));
	}
	public static float Fade(float t)
	{
		return t*t*t*(t*(t*6-15)+10);
		//return 3*t*t-2*t*t*t;
	}
	
	public static float Gradient_3D(int x, Vector3 y)
	{
		return Vector3.Dot (gradient_3D[x%gradient_3D.Length], y);
	}

	public static float Gradient_2D(int x, Vector2 y)
	{
		return Vector2.Dot (gradient_2D[x%gradient_2D.Length], y);
	}

	public static float Gradient_1D(int x, float y)
	{
		return gradient_1D [x % gradient_1D.Length] * y;
	}

	
	public static int Perm(Vector2 p)
	{
		int p1 = permutation [((int)p.x) % 256];
		return p1+(int)p.y;
	}
	public static int Perm(Vector3 p)
	{
		int p1 = permutation[((int)p.x) % permutation.Length];
		int p2 = permutation[(p1 + (int)p.y) % permutation.Length];
		return permutation[(p2 + (int)p.z) % permutation.Length];
	}
	
	public static float PerlinNoise_1D(float p)
	{
		InitGradient();
		int ip = Mathf.FloorToInt (p);
		float np = p - ip;
		
		float t = Fade (np);
		
		return Mathf.Lerp (Gradient_1D(ip, np), Gradient_1D(ip+1, np-1), t);
	}
	//classic method
	public static float PerlinNoise_2D(Vector2 p)
	{
		InitGradient();
		Vector2 ip = CMath.Floor(p);
		Vector2 np = p - ip;
		
		Vector2 t = Fade(np);
		
		float corner1 = Gradient_2D ( Perm(ip), np );
		float corner2 = Gradient_2D ( Perm(ip+new Vector2(1, 0)), np-new Vector2(1, 0) );
		float corner3 = Gradient_2D ( Perm(ip+new Vector2(0, 1)), np-new Vector2(0, 1) );
		float corner4 = Gradient_2D ( Perm(ip+new Vector2(1, 1)), np-new Vector2(1, 1) );
		
		return Mathf.Lerp (
			Mathf.Lerp(corner1, corner2, t.x),
			Mathf.Lerp(corner3, corner4, t.x),
			t.y);
	}
	//this is improved method, 
	//using 16 gradients instead of 256 gradients
	//so the method to get the gradient is g[ p[p[p[x]+y]+z] ], not g[ p[p[x]+y]+z ]
	public static float PerlinNoise_3D(Vector3 p)
	{
		InitGradient();
		Vector3 ip = CMath.Floor (p);
		Vector3 np = p - ip;
		
		Vector3 t = Fade (np);
		
		float corner1 = Gradient_3D(Perm(ip), np);
		float corner2 = Gradient_3D(Perm(ip+new Vector3(1, 0, 0)), np-new Vector3(1, 0, 0));
		float corner3 = Gradient_3D(Perm(ip+new Vector3(0, 1, 0)), np-new Vector3(0, 1, 0));
		float corner4 = Gradient_3D(Perm(ip+new Vector3(1, 1, 0)), np-new Vector3(1, 1, 0));
		float corner5 = Gradient_3D(Perm(ip+new Vector3(0, 0, 1)), np-new Vector3(0, 0, 1));
		float corner6 = Gradient_3D(Perm(ip+new Vector3(1, 0, 1)), np-new Vector3(1, 0, 1));
		float corner7 = Gradient_3D(Perm(ip+new Vector3(0, 1, 1)), np-new Vector3(0, 1, 1));
		float corner8 = Gradient_3D(Perm(ip+new Vector3(1, 1, 1)), np-new Vector3(1, 1, 1));
		
		return Mathf.Lerp( 	Mathf.Lerp( Mathf.Lerp( corner1, corner2, t.x ), Mathf.Lerp( corner3, corner4, t.x ), t.y ),
		                  Mathf.Lerp( Mathf.Lerp( corner5, corner6, t.x ), Mathf.Lerp( corner7, corner8, t.x ), t.y ),
		                  t.z );
	}

	public static Texture2D CreatePermutationSamplerTexture(TextureFormat format)
	{
		Texture2D m_tex = new Texture2D (permutation.Length, 1, format, false);
		m_tex.wrapMode = TextureWrapMode.Repeat;
		m_tex.filterMode = FilterMode.Point;
		m_tex.anisoLevel = 0;
		m_tex.mipMapBias = 0;
		
		Color col = new Color ();
		for (int i = 0; i < permutation.Length; i++) {
			col.a = permutation [i] / 255.0f;
			//col.r = col.a;
			//col.g = col.a;
			//col.b = col.a;
			m_tex.SetPixel (i, 0, col);
		}
		m_tex.Apply();
		
		return m_tex;
	}
	
	public static Texture2D CreateGradientSamplerTexture(TextureFormat format)
	{
		Texture2D tex = new Texture2D (gradient_3D.Length, 1, format, false);
		tex.wrapMode = TextureWrapMode.Repeat;
		tex.filterMode = FilterMode.Point;
		tex.anisoLevel = 0;
		tex.mipMapBias = 0;
		Color col = new Color ();
		for (int i = 0; i < gradient_3D.Length; i++) {
			col.r = gradient_3D [i].x*0.5f+0.5f;
			col.g = gradient_3D [i].y*0.5f+0.5f;
			col.b = gradient_3D [i].z*0.5f+0.5f;
			
			tex.SetPixel (i, 0, col);
		}
		
		tex.Apply();
		
		return tex;
	}
}
