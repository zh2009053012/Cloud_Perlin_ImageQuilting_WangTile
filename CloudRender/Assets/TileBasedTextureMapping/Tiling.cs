using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tiling : MonoBehaviour
{
    //默认边的颜色种类数量为2
    float[] m_colorArray = new float[256];//
    float[] m_atlasIndex1D = new float[4];
    // Start is called before the first frame update
    void Start()
    {
        UnityEngine.Random.InitState(1973);
        for (int i = 0; i < m_colorArray.Length; i++)
        {
            //四种：00 01 10 11。利用位移操作取2进制位，利用&操作取个位。n>>1. n&1.
            m_colorArray[i] = Random.Range(0, 4);   //随机Cn和Cw的值
        }
        Debug.Log(m_colorArray.Length);
        Shader.SetGlobalFloatArray("_ColorType", m_colorArray);

        for (int i = 0; i < 2; i++) 
        {
            for (int j = 0; j < 2; j++) 
            {
                int index = i * 2 + j;
                m_atlasIndex1D[index] = TileIndex1D(i, j);
                //Debug.Log(i+","+j+","+m_atlasIndex1D[index]);
            }
        }
        Shader.SetGlobalFloatArray("_AtlasIndex1D", m_atlasIndex1D);
    }

    float TileIndex1D(int e1, int e2) 
    {
        if (e1 == e2 && e2 == 0)
        {
            return 0;
        }
        else if (e1 > e2 && e2 > 0)
        {
            return e1 * e1 + 2 * e2 - 1;
        }
        else if (e2 > e1 && e1 >= 0)
        {
            return 2 * e1 + e2 * e2;
        }
        else if (e1 == e2 && e2 > 0)
        {
            return (e1 + 1) * (e1 + 1) - 2;
        }
        else if (e1 > e2 && e2 == 0)
        {
            return (e1 + 1) * (e1 + 1) - 1;
        }
        else 
        {
            Debug.LogError("TileIndex1D error.");
            return 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
