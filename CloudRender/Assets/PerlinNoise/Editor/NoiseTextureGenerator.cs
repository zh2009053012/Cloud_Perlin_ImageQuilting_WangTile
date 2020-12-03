using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using CustomMath;

public class NoiseTextureGenerator : ScriptableWizard {
	public int width = 512;
	public int height = 512;
	public float amplitude = 1;
	public float frequence = 8;
	public int seed = 888;

	public enum Equation{Normal, NormalSum, TurbulenceSum, SinTurbulenceSum, Wood, Seamless};
	public Equation equation = Equation.Normal;

	public delegate float NoiseEquationCallback(Vector2 p);
	NoiseEquationCallback m_noiseEquationCallback;

	void OnWizardUpdate () {
		helpString = "Generate perlin noise textures.";
		isValid = (width > 0) && (height > 0) && (amplitude > 0) && (frequence > 0);
	}
	void OnWizardCreate () {
		switch(equation)
		{
		case Equation.Normal:
			m_noiseEquationCallback = NormalPerlinNoise2D;
			break;
		case Equation.NormalSum:
			m_noiseEquationCallback = NormalPerlinNoise2D_Sum;
			break;
		case Equation.TurbulenceSum:
			m_noiseEquationCallback = Turbulence_Sum;
			break;
		case Equation.SinTurbulenceSum:
			m_noiseEquationCallback = SinTurbulence_Sum;
			break;
		case Equation.Wood:
			m_noiseEquationCallback = Wood;
			break;
			case Equation.Seamless:
				m_noiseEquationCallback = SeamlessPerlinNoise2D;
				break;
		}
		Texture2D tex = CreatePerlinNoiseTexture(512, 512, seed, m_noiseEquationCallback);
		SaveTextureAsPNG(tex, Application.dataPath+"/PerlinNoise/Textures/perlinnoise.png");
		AssetDatabase.Refresh();
	}
	[MenuItem("PerlinNoise/Generate Perlin Noise Textures")]
	static void GenerateNoiseOctaves () {
		ScriptableWizard.DisplayWizard<NoiseTextureGenerator>("Generate Perlin Noise Tex", "Generate");
	}

	[MenuItem("PerlinNoise/Generate Permutation Texture")]
	static void GeneratePermutationTexture()
	{
		Texture2D tex = PerlinNoise.CreatePermutationSamplerTexture(TextureFormat.Alpha8);
		SaveTextureAsPNG(tex, Application.dataPath+"/PerlinNoise/Textures/permutation.png");
		AssetDatabase.Refresh();
	}

	[MenuItem("PerlinNoise/Generate Gradient Texture")]
	static void GenerateGradientTexture()
	{
		Texture2D tex = PerlinNoise.CreateGradientSamplerTexture(TextureFormat.RGB24);
		SaveTextureAsPNG(tex, Application.dataPath+"/PerlinNoise/Textures/gradient.png");
		AssetDatabase.Refresh();
	}

	static bool SaveTextureAsPNG(Texture2D tex, string path, bool isOverWrite=true)
	{
		if (!isOverWrite && File.Exists (path)) {
			Debug.LogWarning (path+" is already exit");
			return false;
		}
		if (tex == null) {
			Debug.LogWarning ("texture is null.");
			return false;
		}
		
		byte[] texData = tex.EncodeToPNG ();
		
		int index = path.LastIndexOf ('/');
		string dir = path.Remove (index);
		if (!Directory.Exists (dir)) {
			Directory.CreateDirectory (dir);
		}
		
		File.WriteAllBytes (path, texData);
		return true;
	}

	Texture2D CreatePerlinNoiseTexture(int width, int height, int seed, NoiseEquationCallback callback)
	{
		if(callback == null)
		{
			Debug.LogWarning("CreatePerlinNoiseTexture fail. callback is null.");
			return null;
		}
		Texture2D tex = new Texture2D (width, height, TextureFormat.RGB24, false);

		Color[] col = new Color[width*height];
		for (float i = 0; i < height; i++) {
			for (float j = 0; j < width; j++) {
				float u = i / height;
				float v = j / width;
				float sample = callback(new Vector2(u, v));
				col [(int)(i * width + j)] = new Color (sample, sample, sample);
			}
		}
		tex.SetPixels (col);
		tex.Apply ();
		return tex;
	}

	float SeamlessPerlinNoise2D(Vector2 p) 
	{
		Vector2 move = (new Vector2(1, 1) + CMath.Hash21(seed)) * 100;
		p = (p + move) * frequence;
		return SimplexNoise.SeamlessNoise(p.x, p.y, 10, 10, seed);
	}

	float NormalPerlinNoise2D(Vector2 p)
	{
		Vector2 move = (new Vector2(1, 1)+CMath.Hash21(seed))*100;
		p = (p+move)*frequence;
		float sample = PerlinNoise.PerlinNoise_2D(p);	
		return (sample*0.5f+0.5f)*amplitude;
	}

	float NormalPerlinNoise2D_Sum(Vector2 p)
	{
		Vector2 move = (new Vector2(1, 1)+CMath.Hash21(seed))*100;
		p = (p+move)*frequence;
		float sample = PerlinNoise.PerlinNoise_2D(p)+
			0.5f*PerlinNoise.PerlinNoise_2D(2*p)+
				0.25f*PerlinNoise.PerlinNoise_2D(4*p)+
				0.125f*PerlinNoise.PerlinNoise_2D(8*p);	
		//return (sample*0.5f+0.5f)*amplitude;
		float x = (sample*0.5f+0.5f)*amplitude;
		x = x * 0.8f + 0.2f;
		x = (1-(1-x)*(1-x));
		return x;
	}

	float Turbulence_Sum(Vector2 p)
	{
		Vector2 move = (new Vector2(1, 1)+CMath.Hash21(seed))*100;
		p = (p+move)*frequence;
		float sample = Mathf.Abs (PerlinNoise.PerlinNoise_2D(p))+
			0.5f*Mathf.Abs (PerlinNoise.PerlinNoise_2D(2*p))+
				0.25f*Mathf.Abs (PerlinNoise.PerlinNoise_2D(4*p))+
				0.125f*Mathf.Abs (PerlinNoise.PerlinNoise_2D(8*p));	
		return (sample*0.5f+0.5f)*amplitude;
	}

	float SinTurbulence_Sum(Vector2 p)
	{
		Vector2 move = (new Vector2(1, 1)+CMath.Hash21(seed))*100;
		p = (p+move)*frequence;
		float sample = Mathf.Abs (PerlinNoise.PerlinNoise_2D(p))+
			0.5f*Mathf.Abs (PerlinNoise.PerlinNoise_2D(2*p))+
				0.25f*Mathf.Abs (PerlinNoise.PerlinNoise_2D(4*p))+
				0.125f*Mathf.Abs (PerlinNoise.PerlinNoise_2D(8*p));	
		sample = Mathf.Sin (p.x + sample);
		return (sample*0.5f+0.5f)*amplitude;
	}

	float Wood(Vector2 p)
	{
		Vector2 move = (new Vector2(1, 1)+CMath.Hash21(seed))*100;
		p = (p+move)*frequence;
		float sample = PerlinNoise.PerlinNoise_2D(p)*20;
		sample = sample-(int)sample;
		return (sample*0.5f+0.5f)*amplitude;
	}
}
