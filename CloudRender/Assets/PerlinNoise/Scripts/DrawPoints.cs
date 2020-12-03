using UnityEngine;
using System.Collections;
using Vectrosity;

public class DrawPoints : MonoBehaviour {

	public Material m_mat;
	// Use this for initialization
	void Start () {
		DrawPerlinNoise_1D(1024, 256, 16);
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void DrawPerlinNoise_1D(int width, float amp, float fre)
	{
		Vector2[] points = new Vector2[width];
		for (float j = 0; j < width; j++) {
			float u = (j / width) * fre;
			float sample = PerlinNoise.PerlinNoise_1D(u);
//			if(j == 0)
//			{
//				points[0] = new Vector2(j, sample+Screen.height*0.25f);
//			}
//			else
//			{
//				points[(int)j] = new Vector2(1, sample*amp)+points[(int)(j-1)];
//			}
			points[(int)j] = new Vector2(j, (sample+1)*amp);
		}
		VectorPoints p = new VectorPoints("perlinnoise_1d", points, m_mat, 1.0f);
		p.Draw();
	}
}
