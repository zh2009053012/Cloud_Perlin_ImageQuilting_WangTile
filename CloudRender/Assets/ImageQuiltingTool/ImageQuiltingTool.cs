
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class ImageQuiltingTool
{
    public enum Direction { 
        Up,
        Right,
        Down,
        Left
    }
    public class ImageBlockData {
        public int Size;    //BlockSize
        public Color[] Cs;  //len=Size*Size

        public ImageBlockData(Color[] cs, int size) 
        {
            Cs = cs;
            Size = size;
        }

        public ImageBlockData(ImageBlockData data) 
        {
            Size = data.Size;
            Cs = new Color[data.Cs.Length];
            for (int i = 0; i < data.Cs.Length; i++) 
            {
                Cs[i] = data.Cs[i];
            }
        }

        /// <summary>
        /// 根据方向Direction获取重叠了overlap长度的区域颜色
        /// </summary>
        /// <returns></returns>
        public Color[] GetOverlapColors(Direction dir, int overlap) 
        {
            Color[] cs = new Color[overlap * Size];
            switch (dir) 
            {
                case Direction.Down:
                    for (int i = Size - overlap; i < Size; i++) 
                    {
                        for (int j = 0; j < Size; j++) 
                        {
                            int index = (i - Size + overlap) * Size + j;
                            int srcIndex = i * Size + j;
                            cs[index] = Cs[srcIndex];
                        }
                    }
                    break;
                case Direction.Left:
                    for (int i = 0; i < Size; i++) 
                    {
                        for (int j = 0; j < overlap; j++) 
                        {
                            int index = i * overlap + j;
                            int srcIndex = i * Size + j;
                            cs[index] = Cs[srcIndex];
                        }
                    }
                    break;
                case Direction.Right:
                    for (int i = 0; i < Size; i++) 
                    {
                        for (int j = Size - overlap; j < Size; j++) 
                        {
                            int index = i * overlap + j - Size + overlap;
                            int srcIndex = i * Size + j;
                            cs[index] = Cs[srcIndex];
                        }
                    }
                    break;
                case Direction.Up:
                    for (int i = 0; i < overlap; i++) 
                    {
                        for (int j = 0; j < Size; j++) 
                        {
                            int index = i * Size + j;
                            cs[index] = Cs[index];
                        }
                    }
                    break;
            }
            return cs;
        }

        public void SaveTexture(string path) 
        {
            Texture2D tex = new Texture2D(Size, Size);
            tex.SetPixels(Cs);
            tex.Apply();
            byte[] bs = tex.EncodeToTGA();
            File.WriteAllBytes(path, bs);
        }
    }
    
    /// <summary>
    /// 用于求最短累计误差路径的数据结构
    /// </summary>
    public class PointData 
    {
        public int2 Pos1;           //标识ImageBlockData1中的点
        public int2 Pos2;           //标识ImageBlockData2中的点
        public float Error;         //当前点的误差
        public float MinErrorSum;   //当前点的累计最小误差
        public PointData PrePoint;  //前一个路径点
        //public List<PointData> NeiList; //相接的邻居
        public PointData[] NeiArray;//邻居
        protected int m_neiIndex;
       
        public bool IsInS;
        public PointData(int2 pos1, int2 pos2, float error) 
        {
            Pos1 = pos1;
            Pos2 = pos2;
            Error = error;
            MinErrorSum = 0;
            PrePoint = null;
            //NeiList = new List<PointData>();
            NeiArray = new PointData[8];
            m_neiIndex = 0;
            IsInS = false;
        }
        public void AddNei(PointData data) 
        {
            NeiArray[m_neiIndex++] = data;
        }
        public void Clear() 
        {
            MinErrorSum = 0;
            PrePoint = null;
            IsInS = false;
        }

        public void UpdateNeiError() 
        {
            for (int i = 0; i < m_neiIndex; i++) 
            {
                if (null != NeiArray[i] && !NeiArray[i].IsInS) 
                {
                    NeiArray[i].UpdateError();
                }
            }
        }

        public void UpdateError() 
        {
            int minErrorIndex = -1;
            for (int i = 0; i < m_neiIndex; i++) 
            {
                if (null != NeiArray[i] && NeiArray[i].IsInS)//
                {
                    if (minErrorIndex == -1)
                    {
                        minErrorIndex = i;
                    }
                    else if (NeiArray[i].MinErrorSum < NeiArray[minErrorIndex].MinErrorSum) 
                    {
                        minErrorIndex = i;
                    }
                }
            }
            if (minErrorIndex >= 0)
            {
                MinErrorSum = NeiArray[minErrorIndex].MinErrorSum + Error;
                PrePoint = NeiArray[minErrorIndex];
            }
            else 
            {
                MinErrorSum = 10;
                PrePoint = null;
            }
            
        }
    }
    public static Texture2D DoQuiltingWangTileImage(Texture2D src, int tileSize) 
    {
        int overlap = tileSize / 6;
        int blockSize = tileSize + overlap * 2;
        //int count = 2;
        //int canvasSize = count * tileSize + overlap * 2;
        //Color[] canvas = new Color[canvasSize * canvasSize];

        //集合
        List<ImageBlockData> blocks = new List<ImageBlockData>();
        blocks.AddRange(GetBlockImages(src, 6, blockSize));
        //选取四个待缝合的块 A B C D
        ImageBlockData a = blocks[0];
        blocks.RemoveAt(0);
        //ImageBlockData b = FindFitBlock2(a, blocks, overlap);
        ImageBlockData b = blocks[UnityEngine.Random.Range(0, blocks.Count)];
        blocks.Remove(b);
        //ImageBlockData c = FindFitBlock3(a, blocks, overlap);
        ImageBlockData c = blocks[UnityEngine.Random.Range(0, blocks.Count)];
        blocks.Remove(c);
        //ImageBlockData d = FindFitBlock2(c, blocks, overlap);
        ImageBlockData d = blocks[UnityEngine.Random.Range(0, blocks.Count)];
        blocks.Remove(d);


        Texture2D tex0_0 = GenerateWangTile(b, a, a, b, tileSize, overlap);
        
        Texture2D tex0_1 = GenerateWangTile(b, c, a, b, tileSize, overlap);
        Texture2D tex0_2 = GenerateWangTile(b, c, c, b, tileSize, overlap);
        Texture2D tex0_3 = GenerateWangTile(b, a, c, b, tileSize, overlap);

        Texture2D tex1_0 = GenerateWangTile(b, a, a, d, tileSize, overlap);
        Texture2D tex1_1 = GenerateWangTile(b, c, a, d, tileSize, overlap);
        Texture2D tex1_2 = GenerateWangTile(b, c, c, d, tileSize, overlap);
        Texture2D tex1_3 = GenerateWangTile(b, a, c, d, tileSize, overlap);

        Texture2D tex2_0 = GenerateWangTile(d, a, a, d, tileSize, overlap);
        Texture2D tex2_1 = GenerateWangTile(d, c, a, d, tileSize, overlap);
        Texture2D tex2_2 = GenerateWangTile(d, c, c, d, tileSize, overlap);
        Texture2D tex2_3 = GenerateWangTile(d, a, c, d, tileSize, overlap);

        Texture2D tex3_0 = GenerateWangTile(d, a, a, b, tileSize, overlap);
        Texture2D tex3_1 = GenerateWangTile(d, c, a, b, tileSize, overlap);
        Texture2D tex3_2 = GenerateWangTile(d, c, c, b, tileSize, overlap);
        Texture2D tex3_3 = GenerateWangTile(d, a, c, b, tileSize, overlap);

        //
        //byte[] bs = tex0_0.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath+"/0_0.tga", bs);
        //bs = tex0_1.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/0_1.tga", bs);
        //bs = tex0_2.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/0_2.tga", bs);
        //bs = tex0_3.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/0_3.tga", bs);

        //bs = tex1_0.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/1_0.tga", bs);
        //bs = tex1_1.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/1_1.tga", bs);
        //bs = tex1_2.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/1_2.tga", bs);
        //bs = tex1_3.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/1_3.tga", bs);

        //bs = tex2_0.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/2_0.tga", bs);
        //bs = tex2_1.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/2_1.tga", bs);
        //bs = tex2_2.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/2_2.tga", bs);
        //bs = tex2_3.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/2_3.tga", bs);

        //bs = tex3_0.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/3_0.tga", bs);
        //bs = tex3_1.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/3_1.tga", bs);
        //bs = tex3_2.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/3_2.tga", bs);
        //bs = tex3_3.EncodeToTGA();
        //File.WriteAllBytes(Application.dataPath + "/3_3.tga", bs);

        float size = Mathf.Sqrt(2) * tileSize;
        int imageSize = (int)size * 4;
        Texture2D tex = new Texture2D(imageSize, imageSize);

        CopyWangTileToImage(tex, tex0_0, (int)size, 0, 0, tileSize, overlap);
        CopyWangTileToImage(tex, tex0_1, (int)size, 0, 1, tileSize, overlap);
        CopyWangTileToImage(tex, tex0_2, (int)size, 0, 2, tileSize, overlap);
        CopyWangTileToImage(tex, tex0_3, (int)size, 0, 3, tileSize, overlap);

        CopyWangTileToImage(tex, tex1_0, (int)size, 1, 0, tileSize, overlap);
        CopyWangTileToImage(tex, tex1_1, (int)size, 1, 1, tileSize, overlap);
        CopyWangTileToImage(tex, tex1_2, (int)size, 1, 2, tileSize, overlap);
        CopyWangTileToImage(tex, tex1_3, (int)size, 1, 3, tileSize, overlap);

        CopyWangTileToImage(tex, tex2_0, (int)size, 2, 0, tileSize, overlap);
        CopyWangTileToImage(tex, tex2_1, (int)size, 2, 1, tileSize, overlap);
        CopyWangTileToImage(tex, tex2_2, (int)size, 2, 2, tileSize, overlap);
        CopyWangTileToImage(tex, tex2_3, (int)size, 2, 3, tileSize, overlap);

        CopyWangTileToImage(tex, tex3_0, (int)size, 3, 0, tileSize, overlap);
        CopyWangTileToImage(tex, tex3_1, (int)size, 3, 1, tileSize, overlap);
        CopyWangTileToImage(tex, tex3_2, (int)size, 3, 2, tileSize, overlap);
        CopyWangTileToImage(tex, tex3_3, (int)size, 3, 3, tileSize, overlap);
        tex.Apply();
        return tex;
    }

    private static void CopyWangTileToImage(Texture2D main, Texture2D wangTile, int wangTileSize, int row, int col, int tileSize, int overlap) 
    {
        float cos = Mathf.Cos(45 * Mathf.Deg2Rad);
        float sin = Mathf.Sin(45 * Mathf.Deg2Rad);
        
        for (int i = 0; i < wangTileSize; i++)
        {
            for (int j = 0; j < wangTileSize; j++)
            {
                float offset = (2 * tileSize - wangTileSize) * 0.5f + overlap;
                float uvx = (j + offset) / (tileSize * 2 + overlap * 2);
                float uvy = (i + offset) / (tileSize * 2 + overlap * 2);
                uvx -= 0.5f;
                uvy -= 0.5f;
                float u = cos * uvx + sin * uvy;
                float v = -sin * uvx + cos * uvy;
                u += 0.5f;
                v += 0.5f;
                Color color = wangTile.GetPixelBilinear(u, v);
                main.SetPixel(j + col * wangTileSize, i + row * wangTileSize, color);
            }
        }
    }

    private static Texture2D GenerateWangTile(ImageBlockData image1, ImageBlockData image2,
        ImageBlockData image3, ImageBlockData image4, int tileSize, int overlap) 
    {
        int count = 2;
        int canvasSize = count * tileSize + overlap * 2;
        Color[] canvas = new Color[canvasSize * canvasSize];

        CopyImageBlockDataToDest(image1, ref canvas, 0, 0, canvasSize, tileSize, overlap);

        ImageBlockData temp2 = new ImageBlockData(image2);
        QuiltingLeft3(Direction.Down, image1, temp2, tileSize, overlap);
        CopyImageBlockDataToDest(temp2, ref canvas, 0, 1, canvasSize, tileSize, overlap);

        ImageBlockData temp3 = new ImageBlockData(image3);
        ImageBlockData canvasTile1 = GetBlockDataFromCanvas(canvas, canvasSize, 0, 0, tileSize, overlap);
        QuiltingDown3(Direction.Left, temp3, canvasTile1, tileSize, overlap);
        CopyImageBlockDataToDest(temp3, ref canvas, 1, 0, canvasSize, tileSize, overlap);

        ImageBlockData temp4 = new ImageBlockData(image4);
        ImageBlockData canvasTile2 = GetBlockDataFromCanvas(canvas, canvasSize, 0, 1, tileSize, overlap);
        ImageBlockData canvasTile3 = GetBlockDataFromCanvas(canvas, canvasSize, 1, 0, tileSize, overlap);
        QuiltingLeft3(Direction.Up, canvasTile3, temp4, tileSize, overlap);
        QuiltingDown3(Direction.Right, temp4, canvasTile2, tileSize, overlap);
        CopyImageBlockDataToDest(temp4, ref canvas, 1, 1, canvasSize, tileSize, overlap);

        Texture2D tex = new Texture2D(canvasSize, canvasSize);
        tex.SetPixels(canvas);
        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 使用原图src缝合出大小为generatedImageSize的图像
    /// </summary>
    /// <param name="src"></param>
    /// <param name="generatedImageSize"></param>
    /// <param name="tileSize"></param>
    /// <returns></returns>
    public static Texture2D DoImageQuilting(Texture2D src, int generatedImageSize, int tileSize) 
    {
        //tileSize一个实际块的长度
        int overlap = tileSize / 6;             //重叠部分的边长
        int blockSize = tileSize + overlap * 2; //算上重叠部分，需要从src中获取的完整块的边长
        int count = Mathf.CeilToInt(generatedImageSize / (float)tileSize);
        int canvasSize = count * tileSize + overlap * 2;
        Color[] canvas = new Color[canvasSize * canvasSize];
        //集合
        List<ImageBlockData> blocks = new List<ImageBlockData>();
        blocks.AddRange(GetBlockImages(src, count * count * 2, blockSize));

        //以一个块的步长，遍历要合成的图像
        ImageBlockData ibd;
        ImageBlockData leftNeighbor;
        ImageBlockData downNeighbor;
        for (int i = 0; i < count; i++) 
        {
            for (int j = 0; j < count; j++) 
            {
                //左下角起始块直接从blocks中随机选取一个填充
                if (i == 0 && j == 0) 
                {
                    ibd = blocks[UnityEngine.Random.Range(0, blocks.Count)];
                    blocks.Remove(ibd);
                    CopyImageBlockDataToDest(ibd, ref canvas, 0, 0, canvasSize, tileSize, overlap);
                    continue;
                }
                if (i == 0)
                {
                    downNeighbor = null;
                }
                else 
                {
                    //从dest拷贝出来一块data，用来计算误差和缝合
                    downNeighbor = GetBlockDataFromCanvas(canvas, canvasSize, i - 1, j, tileSize, overlap);
                }
                if (j == 0)
                {
                    leftNeighbor = null;
                }
                else 
                {
                    //从dest拷贝出来一块data
                    leftNeighbor = GetBlockDataFromCanvas(canvas, canvasSize, i, j - 1, tileSize, overlap);
                }
                ImageBlockData fit = FindFitBlock(leftNeighbor, downNeighbor, blocks, overlap);
                blocks.Remove(fit);
                QuiltingLeft2(leftNeighbor, fit, tileSize, overlap);
                QuiltingDown2(fit, downNeighbor, tileSize, overlap);
                CopyImageBlockDataToDest(fit, ref canvas, i, j, canvasSize, tileSize, overlap);
                //break;
            }
        }
        Texture2D tex = new Texture2D(canvasSize, canvasSize);
        tex.SetPixels(canvas);
        tex.Apply();
        return tex;
    }

    private static float TexelError(Color c1, Color c2) 
    {
        return TexelError2(c1, c2);
    }

    private static float TexelError1(Color c1, Color c2) 
    {
        float r = c1.r - c2.r;
        float g = c1.g - c2.g;
        float b = c1.b - c2.b;
        float a = c1.a - c2.a;
        return r * r + g * g + b * b + a * a;
    }

    private static float TexelError2(Color c1, Color c2) 
    {
        float gray = c1.grayscale - c2.grayscale;
        return gray * gray;
    }

    private static float TexelError3(Color c1, Color c2)
    {
        float r = c1.r - c2.r;
        float g = c1.g - c2.g;
        float b = c1.b - c2.b;
        float a = c1.a - c2.a;
        return r * r * r * r + g * g * g * g + b * b * b * b + a * a * a * a;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="tileSize"></param>
    /// <param name="overlap"></param>
    private static void QuiltingLeft2(ImageBlockData left, ImageBlockData right, int tileSize, int overlap)
    {
        if (left == null || right == null)
        {
            return;
        }
        //find min error path
        int allOverlap = overlap * 2;
        int blockSize = tileSize + allOverlap;

        PointData[,] ps = new PointData[blockSize, allOverlap];
        List<PointData> sList = new List<PointData>();  //已经遍历的点
        List<PointData> uList = new List<PointData>();  //未遍历的点
        for (int col = 0; col < allOverlap; col++) 
        {
            for (int row = 0; row < blockSize; row++) 
            {
                int2 leftPos = new int2(blockSize - allOverlap + col, row); //x,y : col, row
                int2 rightPos = new int2(col, row);
                int leftIndex = leftPos.y * blockSize + leftPos.x;
                int rightIndex = rightPos.y * blockSize + rightPos.x;
                float error = TexelError(left.Cs[leftIndex], right.Cs[rightIndex]);
                PointData pd = new PointData(leftPos, rightPos, error);
                ps[row, col] = pd;
            }
        }
        //set neighbor
        for (int col = 0; col < allOverlap; col++)
        {
            for (int row = 0; row < blockSize; row++)
            {
                for (int offsetx = -1; offsetx <= 1; offsetx++) 
                {
                    for (int offsety = -1; offsety <= 1; offsety++) 
                    {
                        if (offsetx == 0 && offsety == 0) 
                        {
                            continue;
                        }
                        if (offsetx + row < 0 || offsetx + row >= blockSize) 
                        {
                            continue;
                        }
                        if (offsety + col < 0 || offsety + col >= allOverlap) 
                        {
                            continue;
                        }
                        ps[row, col].AddNei(ps[offsetx + row, offsety + col]);
                    }
                }
            }
        }

        int colMin = 0;
        for (int i = 1; i < allOverlap; i++) 
        {
            PointData p = ps[0, i];
            if (p.Error < ps[0, colMin].Error) 
            {
                colMin = i;
            }
        }

        //迪杰斯特拉
        sList.Clear();
        uList.Clear();
        for (int col = 0; col < allOverlap; col++)
        {
            for (int row = 0; row < blockSize; row++)
            {
                ps[row, col].Clear();
                uList.Add(ps[row, col]);
            }
        }
        //选取起始点
        PointData start = ps[0, colMin];
        uList.Remove(start);
        sList.Add(start);
        start.IsInS = true;
        start.MinErrorSum = start.Error;

        while (uList.Count > 0) 
        {
#if UNITY_EDITOR
            int count = blockSize * allOverlap;
            EditorUtility.DisplayProgressBar("ImageQuiltingTool", "Progress:"+sList.Count+"/"+count, (sList.Count) / (float)count);
#endif
            int minErrorIndex = -1;
            //找到最小误差
            for (int m = 0; m < uList.Count; m++)
            {
                uList[m].UpdateError();
                if (minErrorIndex == -1)
                {
                    minErrorIndex = m;
                }
                else 
                {
                    if (uList[m].MinErrorSum < uList[minErrorIndex].MinErrorSum)
                    {
                        minErrorIndex = m;
                    }
                }
            }
            PointData min = uList[minErrorIndex];
            uList.RemoveAt(minErrorIndex);
            sList.Add(min);
            min.IsInS = true;
        }
        //找到row == blockSize-1的最小误差点，然后回溯
        int minRowErrorIndex = 0;
        for (int m = 1; m < allOverlap; m++) 
        {
            if (ps[blockSize - 1, m].MinErrorSum < ps[blockSize - 1, minRowErrorIndex].MinErrorSum) 
            {
                minRowErrorIndex = m;
            }
        }
        //回溯
        List<int2> path2 = new List<int2>();
        PointData end = ps[blockSize - 1, minRowErrorIndex];
        path2.Add(end.Pos2);
        while (end.PrePoint != null) 
        {
            end = end.PrePoint;
            path2.Add(end.Pos2);
        }

        //Debug.Log("path len:"+path2.Count);
        //quilting 
        for (int row = 0; row < blockSize; row++)
        {
            for (int col = 0; col < allOverlap; col++)
            {
                int leftIndex = row * blockSize + blockSize - allOverlap + col;
                int rightIndex = row * blockSize + col;

                bool isCopyLeft = false;
                for (int i = 0; i < path2.Count; i++) 
                {
                    if (path2[i].y == row && path2[i].x >= col)
                    {
                        int dir=0;
                        if (i == 0)
                        {
                            if (i + 1 < path2.Count) 
                            {
                                dir = path2[i + 1].y - path2[i].y;
                            }
                        }
                        else 
                        {
                            dir = path2[i].y - path2[i - 1].y;
                        }
                        if (dir < 0)
                        {
                            isCopyLeft = true;
                        }
                        else
                        {
                            isCopyLeft = false;
                        }
                        break;
                    }
                }
                if (isCopyLeft)
                {
                    right.Cs[rightIndex] = left.Cs[leftIndex];
                }
            }
        }
        return;
        //test
        for (int i = 0; i < path2.Count; i++)
        {
            right.Cs[path2[i].y * blockSize + path2[i].x] = Color.red;
        }
        return;
    }

    /// <summary>
    /// 只缝合中间tilesize大小的部分，其余的是正中间缝合
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <param name="tileSize"></param>
    /// <param name="overlap"></param>
    private static void QuiltingLeft3(Direction ignoreDir, ImageBlockData left, ImageBlockData right, int tileSize, int overlap)
    {
        if (left == null || right == null)
        {
            return;
        }
        //find min error path
        int allOverlap = overlap * 2;
        int blockSize = tileSize + allOverlap;

        PointData[,] ps = new PointData[blockSize, allOverlap];
        List<PointData> sList = new List<PointData>();  //已经遍历的点
        List<PointData> uList = new List<PointData>();  //未遍历的点
        for (int col = 0; col < allOverlap; col++)
        {
            for (int row = 0; row < blockSize; row++)
            {
                int2 leftPos = new int2(blockSize - allOverlap + col, row); //x,y : col, row
                int2 rightPos = new int2(col, row);
                int leftIndex = leftPos.y * blockSize + leftPos.x;
                int rightIndex = rightPos.y * blockSize + rightPos.x;
                float error = TexelError(left.Cs[leftIndex], right.Cs[rightIndex]);
                PointData pd = new PointData(leftPos, rightPos, error);
                ps[row, col] = pd;
            }
        }
        //set neighbor
        for (int col = 0; col < allOverlap; col++)
        {
            for (int row = 0; row < blockSize; row++)
            {
                for (int offsetx = -1; offsetx <= 1; offsetx++)
                {
                    for (int offsety = -1; offsety <= 1; offsety++)
                    {
                        if (offsetx == 0 && offsety == 0)
                        {
                            continue;
                        }
                        if (offsetx + row < 0 || offsetx + row >= blockSize)
                        {
                            continue;
                        }
                        if (offsety + col < 0 || offsety + col >= allOverlap)
                        {
                            continue;
                        }
                        ps[row, col].AddNei(ps[offsetx + row, offsety + col]);
                    }
                }
            }
        }
        //迪杰斯特拉
        sList.Clear();
        uList.Clear();
        int startRow = ignoreDir == Direction.Up ? 0 : overlap;
        int endRow = ignoreDir == Direction.Up ? overlap : 0;
        for (int col = 0; col < allOverlap; col++)
        {
            for (int row = startRow; row < blockSize - endRow; row++)
            {
                ps[row, col].Clear();
                
                if (ignoreDir == Direction.Down)
                {
                    if (col < overlap)
                    {
                        if (row + col - 2*overlap < 0) 
                        {
                            continue;
                        }
                    }
                    else 
                    {
                        if (row - col < 0) 
                        {
                            continue;
                        }
                    }
                }
                else if (ignoreDir == Direction.Up) 
                {
                    if (col < overlap)
                    {
                        if (row - col - tileSize > 0) 
                        {
                            continue;
                        }
                    }
                    else 
                    {
                        if (row + col - tileSize - 2*overlap > 0) 
                        {
                            continue;
                        }
                    }
                }
                uList.Add(ps[row, col]);
            }
        }
        //选取起始点
        PointData start = ps[overlap, overlap];
        if (ignoreDir == Direction.Up)
        {
            //选取row==0的最小error
            int minIndex = 0;
            for (int i = 1; i < allOverlap; i++) 
            {
                if (ps[0, i].Error < ps[0, minIndex].Error) 
                {
                    minIndex = i;
                }
            }
            start = ps[0, minIndex];
        }
        else if(ignoreDir == Direction.Down)
        {
            start = ps[overlap, overlap];
        }

        uList.Remove(start);
        sList.Add(start);
        start.IsInS = true;
        start.MinErrorSum = start.Error;

        while (uList.Count > 0)
        {
#if UNITY_EDITOR
            int count = tileSize * allOverlap;
            EditorUtility.DisplayProgressBar("ImageQuiltingTool", "Progress:" + sList.Count + "/" + count, (sList.Count) / (float)count);
#endif
            int minErrorIndex = -1;
            //找到最小误差
            for (int m = 0; m < uList.Count; m++)
            {
                uList[m].UpdateError();
                if (minErrorIndex == -1)
                {
                    minErrorIndex = m;
                }
                else
                {
                    if (uList[m].MinErrorSum < uList[minErrorIndex].MinErrorSum)
                    {
                        minErrorIndex = m;
                    }
                }
            }
            PointData min = uList[minErrorIndex];
            uList.RemoveAt(minErrorIndex);
            sList.Add(min);
            min.IsInS = true;
        }
        //回溯
        List<int2> path2 = new List<int2>();
        if (ignoreDir == Direction.Up)
        {
            for (int i = overlap - 1; i >= 0; i--)
            {
                path2.Add(new int2(overlap, i + tileSize + overlap));
            }
        }
        int endPos = ignoreDir == Direction.Up ? blockSize - 1 - overlap : blockSize - 1;

        PointData end = ps[endPos, overlap];
        if (ignoreDir == Direction.Right) 
        {
            int minIndex2 = 0;
            for (int i = 1; i < allOverlap; i++) 
            {
                if (ps[endPos, i].MinErrorSum < ps[endPos, minIndex2].MinErrorSum) 
                {
                    minIndex2 = i;
                }
            }
            end = ps[endPos, minIndex2];
        }
        path2.Add(end.Pos2);
        while (end.PrePoint != null)
        {
            end = end.PrePoint;
            path2.Add(end.Pos2);
        }
        if (ignoreDir == Direction.Down)
        {
            for (int i = overlap - 1; i >= 0; i--)
            {
                path2.Add(new int2(overlap, i));
            }
        }

        //Debug.Log("path len:"+path2.Count);
        //quilting 
        for (int row = 0; row < blockSize; row++)
        {
            for (int col = 0; col < allOverlap; col++)
            {
                int leftIndex = row * blockSize + blockSize - allOverlap + col;
                int rightIndex = row * blockSize + col;

                bool isCopyLeft = false;
                for (int i = 0; i < path2.Count; i++)
                {
                    if (path2[i].y == row && path2[i].x >= col)
                    {
                        int dir = 0;
                        if (i == 0)
                        {
                            if (i + 1 < path2.Count)
                            {
                                dir = path2[i + 1].y - path2[i].y;
                            }
                        }
                        else
                        {
                            dir = path2[i].y - path2[i - 1].y;
                        }
                        if (dir < 0)
                        {
                            isCopyLeft = true;
                        }
                        else
                        {
                            isCopyLeft = false;
                        }
                        break;
                    }
                }
                if (isCopyLeft)
                {
                    right.Cs[rightIndex] = left.Cs[leftIndex];
                }
            }
        }
        return;
        //test
        for (int i = 0; i < path2.Count; i++)
        {
            right.Cs[path2[i].y * blockSize + path2[i].x] = Color.red;
        }
        return;
    }


    private static void QuiltingDown2(ImageBlockData up, ImageBlockData down, int tileSize, int overlap)
    {
        if (up == null || down == null)
        {
            return;
        }
        //find min error path
        int allOverlap = overlap * 2;
        int blockSize = tileSize + allOverlap;

        PointData[,] ps = new PointData[allOverlap, blockSize];
        List<PointData> sList = new List<PointData>();  //已经遍历的点
        List<PointData> uList = new List<PointData>();  //未遍历的点
        for (int col = 0; col < blockSize; col++)
        {
            for (int row = 0; row < allOverlap; row++)
            {
                int2 upPos = new int2(col, row); //x,y : col, row
                int2 downPos = new int2(col, blockSize - allOverlap + row);
                int upIndex = upPos.y * blockSize + upPos.x;
                int downIndex = downPos.y * blockSize + downPos.x;
                float error = TexelError(up.Cs[upIndex], down.Cs[downIndex]);
                PointData pd = new PointData(upPos, downPos, error);
                ps[row, col] = pd;
            }
        }
        //set neighbor
        for (int col = 0; col < blockSize; col++)
        {
            for (int row = 0; row < allOverlap; row++)
            {
                for (int offsetx = -1; offsetx <= 1; offsetx++)
                {
                    for (int offsety = -1; offsety <= 1; offsety++)
                    {
                        if (offsetx == 0 && offsety == 0)
                        {
                            continue;
                        }
                        if (offsetx + row < 0 || offsetx + row >= allOverlap)
                        {
                            continue;
                        }
                        if (offsety + col < 0 || offsety + col >= blockSize)
                        {
                            continue;
                        }
                        ps[row, col].AddNei(ps[offsetx + row, offsety + col]);
                    }
                }
            }
        }

        int rowMin = 0;
        for (int i = 1; i < allOverlap; i++)
        {
            PointData p = ps[i, 0];
            if (p.Error < ps[rowMin, 0].Error)
            {
                rowMin = i;
            }
        }

        //迪杰斯特拉
        sList.Clear();
        uList.Clear();
        for (int row = 0; row < allOverlap; row++)
        {
            for (int col = 0; col < blockSize; col++)
            {
                ps[row, col].Clear();
                uList.Add(ps[row, col]);
            }
        }
        //选取起始点
        PointData start = ps[rowMin, 0];
        uList.Remove(start);
        sList.Add(start);
        start.IsInS = true;
        start.MinErrorSum = start.Error;

        while (uList.Count > 0)
        {
#if UNITY_EDITOR
            int count = blockSize * allOverlap;
            EditorUtility.DisplayProgressBar("ImageQuiltingTool", "Progress:" + sList.Count + "/" + count, (sList.Count) / (float)count);
#endif
            int minErrorIndex = -1;
            //找到最小误差
            for (int m = 0; m < uList.Count; m++)
            {
                uList[m].UpdateError();
                if (minErrorIndex == -1)
                {
                    minErrorIndex = m;
                }
                else
                {
                    if (uList[m].MinErrorSum < uList[minErrorIndex].MinErrorSum)
                    {
                        minErrorIndex = m;
                    }
                }
            }
            PointData min = uList[minErrorIndex];
            uList.RemoveAt(minErrorIndex);
            sList.Add(min);
            min.IsInS = true;
        }
        //找到col == blockSize-1的最小误差点，然后回溯
        int minColErrorIndex = 0;
        for (int m = 1; m < allOverlap; m++)
        {
            if (ps[m, blockSize - 1].MinErrorSum < ps[minColErrorIndex, blockSize - 1].MinErrorSum)
            {
                minColErrorIndex = m;
            }
        }
        //回溯
        List<int2> path = new List<int2>();
        PointData end = ps[minColErrorIndex, blockSize - 1];
        path.Add(end.Pos1);
        while (end.PrePoint != null)
        {
            end = end.PrePoint;
            path.Add(end.Pos1);
        }

        //Debug.Log("path len:" + path.Count);
        //quilting 
        for (int col = 0; col < blockSize; col++)
        {
            for (int row = 0; row < allOverlap; row++)
            {
                int upIndex = row * blockSize + col;
                int downIndex = (blockSize - allOverlap + row) * blockSize + col;

                bool isCopyDown = false;
                for (int i = 0; i < path.Count; i++)
                {
                    if (path[i].x == col && path[i].y >= row)
                    {
                        int dir = 0;
                        if (i == 0)
                        {
                            if (i + 1 < path.Count)
                            {
                                dir = path[i + 1].x - path[i].x;
                            }
                        }
                        else
                        {
                            dir = path[i].x - path[i - 1].x;
                        }
                        if (dir < 0)
                        {
                            isCopyDown = true;
                        }
                        else
                        {
                            isCopyDown = false;
                        }
                        break;
                    }
                }
                if (isCopyDown)
                {
                    up.Cs[upIndex] = down.Cs[downIndex];
                }
            }
        }
        return;
        //test
        for (int i = 0; i < path.Count; i++)
        {
            up.Cs[path[i].y * blockSize + path[i].x] = Color.red;
        }
        return;
    }

    private static void QuiltingDown3(Direction ignoreDir, ImageBlockData up, ImageBlockData down, int tileSize, int overlap)
    {
        if (up == null || down == null)
        {
            return;
        }
        //find min error path
        int allOverlap = overlap * 2;
        int blockSize = tileSize + allOverlap;

        PointData[,] ps = new PointData[allOverlap, blockSize];
        List<PointData> sList = new List<PointData>();  //已经遍历的点
        List<PointData> uList = new List<PointData>();  //未遍历的点
        for (int col = 0; col < blockSize; col++)
        {
            for (int row = 0; row < allOverlap; row++)
            {
                int2 upPos = new int2(col, row); //x,y : col, row
                int2 downPos = new int2(col, blockSize - allOverlap + row);
                int upIndex = upPos.y * blockSize + upPos.x;
                int downIndex = downPos.y * blockSize + downPos.x;
                float error = TexelError(up.Cs[upIndex], down.Cs[downIndex]);
                PointData pd = new PointData(upPos, downPos, error);
                ps[row, col] = pd;
            }
        }
        //set neighbor
        for (int col = 0; col < blockSize; col++)
        {
            for (int row = 0; row < allOverlap; row++)
            {
                for (int offsetx = -1; offsetx <= 1; offsetx++)
                {
                    for (int offsety = -1; offsety <= 1; offsety++)
                    {
                        if (offsetx == 0 && offsety == 0)
                        {
                            continue;
                        }
                        if (offsetx + row < 0 || offsetx + row >= allOverlap)
                        {
                            continue;
                        }
                        if (offsety + col < 0 || offsety + col >= blockSize)
                        {
                            continue;
                        }
                        ps[row, col].AddNei(ps[offsetx + row, offsety + col]);
                    }
                }
            }
        }

        //迪杰斯特拉
        sList.Clear();
        uList.Clear();
        int startCol = ignoreDir == Direction.Left ? overlap : 0;
        int endCol = ignoreDir == Direction.Left ? 0 : overlap;
        for (int row = 0; row < allOverlap; row++)
        {
            for (int col = startCol; col < blockSize - endCol; col++)
            {
                ps[row, col].Clear();
                if (ignoreDir == Direction.Left)
                {
                    if (row < overlap)
                    {
                        if (row + col - 2 * overlap < 0) 
                        {
                            continue;
                        }
                    }
                    else 
                    {
                        if (row - col > 0) 
                        {
                            continue;
                        }
                    }
                }
                else if (ignoreDir == Direction.Right) 
                {
                    if (row < overlap)
                    {
                        if (row - col + tileSize < 0) 
                        {
                            continue;
                        }
                    }
                    else 
                    {
                        if (row + col - blockSize > 0) 
                        {
                            continue;
                        }
                    }
                }
                uList.Add(ps[row, col]);
            }
        }
        //选取起始点
        PointData start =ps[overlap, overlap];
        if (ignoreDir == Direction.Left)
        {
            start = ps[overlap, overlap];
        }
        else if(ignoreDir == Direction.Right)
        {
            //
            int minIndex = 0;
            for (int i = 1; i < overlap; i++) 
            {
                if (ps[i, 0].Error < ps[minIndex, 0].Error) 
                {
                    minIndex = i;
                }
            }
            start = ps[minIndex, 0];
        }
        uList.Remove(start);
        sList.Add(start);
        start.IsInS = true;
        start.MinErrorSum = start.Error;

        while (uList.Count > 0)
        {
#if UNITY_EDITOR
            int count = tileSize * allOverlap;
            EditorUtility.DisplayProgressBar("ImageQuiltingTool", "Progress:" + sList.Count + "/" + count, (sList.Count) / (float)count);
#endif
            int minErrorIndex = -1;
            //找到最小误差
            for (int m = 0; m < uList.Count; m++)
            {
                uList[m].UpdateError();
                if (minErrorIndex == -1)
                {
                    minErrorIndex = m;
                }
                else
                {
                    if (uList[m].MinErrorSum < uList[minErrorIndex].MinErrorSum)
                    {
                        minErrorIndex = m;
                    }
                }
            }
            PointData min = uList[minErrorIndex];
            uList.RemoveAt(minErrorIndex);
            sList.Add(min);
            min.IsInS = true;
        }
        //回溯
        List<int2> path = new List<int2>();
        if (ignoreDir == Direction.Right)
        {
            for (int i = overlap - 1; i >= 0; i--)
            {
                path.Add(new int2(tileSize + overlap + i, overlap));
            }
        }

        PointData end = ps[overlap, blockSize - 1];
        if (ignoreDir == Direction.Left)
        {
            int minIndex2 = 0;
            for (int i = 1; i < allOverlap; i++) 
            {
                if (ps[i, blockSize - 1].MinErrorSum < ps[minIndex2, blockSize - 1].MinErrorSum) 
                {
                    minIndex2 = i;
                }
            }
            end = ps[minIndex2, blockSize - 1];
        }
        else if (ignoreDir == Direction.Right) 
        {
            end = ps[overlap, blockSize - 1 - overlap];
        }
        path.Add(end.Pos1);
        while (end.PrePoint != null)
        {
            end = end.PrePoint;
            path.Add(end.Pos1);
        }
        if (ignoreDir == Direction.Left)
        {
            for (int i = overlap - 1; i >= 0; i--)
            {
                path.Add(new int2(i, overlap));
            }
        }
        //Debug.Log("path len:" + path.Count);
        //quilting 
        for (int col = 0; col < blockSize; col++)
        {
            for (int row = 0; row < allOverlap; row++)
            {
                int upIndex = row * blockSize + col;
                int downIndex = (blockSize - allOverlap + row) * blockSize + col;

                bool isCopyDown = false;
                for (int i = 0; i < path.Count; i++)
                {
                    if (path[i].x == col && path[i].y >= row)
                    {
                        int dir = 0;
                        if (i == 0)
                        {
                            if (i + 1 < path.Count)
                            {
                                dir = path[i + 1].x - path[i].x;
                            }
                        }
                        else
                        {
                            dir = path[i].x - path[i - 1].x;
                        }
                        if (dir < 0)
                        {
                            isCopyDown = true;
                        }
                        else
                        {
                            isCopyDown = false;
                        }
                        break;
                    }
                }
                if (isCopyDown)
                {
                    up.Cs[upIndex] = down.Cs[downIndex];
                }
            }
        }
        return;
        //test
        for (int i = 0; i < path.Count; i++)
        {
            up.Cs[path[i].y * blockSize + path[i].x] = Color.red;
        }
        return;
    }


    private static void QuiltingLeft(ImageBlockData left, ImageBlockData right, int tileSize, int overlap) 
    {
        if (left == null || right == null) 
        {
            return;
        }
        //find min error path
        int allOverlap = overlap * 2;
        int blockSize = tileSize + allOverlap;
        List<int[]> pathList = new List<int[]>();
        List<float> errorList = new List<float>();

        for (int col = 0; col < allOverlap; col++) 
        {
            int[] path = new int[blockSize];
            float errorSum = 0;
            int preCol = col;
            for (int row = 0; row < blockSize; row++) 
            {
                int leftIndex;
                int rightIndex;
                float error = 0;
                if (row == 0)
                {
                    leftIndex = row * blockSize + blockSize - allOverlap + col;
                    rightIndex = row * blockSize + col;
                    error = TexelError(left.Cs[leftIndex], right.Cs[rightIndex]);
                    errorSum += error;
                    preCol = col;
                }
                else 
                {
                    float minError = float.MaxValue;
                    int minCol = 0;
                    for (int c = preCol - 1; c <= preCol + 1; c++) 
                    {
                        if (c < 0 || c >= allOverlap) 
                        {
                            continue;
                        }
                        leftIndex = row * blockSize + blockSize - allOverlap + c;
                        rightIndex = row * blockSize + c;
                        error = TexelError(left.Cs[leftIndex], right.Cs[rightIndex]);
                        if (error < minError) 
                        {
                            minError = error;
                            minCol = c;
                        }
                    }
                    errorSum += minError;
                    preCol = minCol;
                }
                path[row] = preCol;
            }
            pathList.Add(path);
            errorList.Add(errorSum);
        }

        int minPath = 0;
        float minErrorV = float.MaxValue;
        for (int i = 0; i < errorList.Count; i++) 
        {
            if (errorList[i] < minErrorV) 
            {
                minErrorV = errorList[i];
                minPath = i;
            }
        }

        //quilting 
        for (int row = 0; row < blockSize; row++) 
        {
            int index = pathList[minPath][row];
            for (int col = 0; col < index; col++) 
            {
                int leftIndex = row * blockSize + blockSize - allOverlap + col;
                int rightIndex = row * blockSize + col;
                right.Cs[rightIndex] = left.Cs[leftIndex];
            }
        }
    }

    private static void QuiltingDown(ImageBlockData up, ImageBlockData down, int tileSize, int overlap)
    {
        if (up == null || down == null)
        {
            return;
        }
        //find min error path
        int allOverlap = overlap * 2;
        int blockSize = tileSize + allOverlap;
        List<int[]> pathList = new List<int[]>();
        List<float> errorList = new List<float>();

        for (int row = 0; row < allOverlap; row++)
        {
            int[] path = new int[blockSize];
            float errorSum = 0;
            int preRow = row;
            for (int col = 0; col < blockSize; col++)
            {
                int upIndex;
                int downIndex;
                float error = 0;
                if (col == 0)
                {
                    downIndex = (row + blockSize - allOverlap ) * blockSize + col;
                    upIndex = row * blockSize + col;
                    error = TexelError(up.Cs[upIndex], down.Cs[downIndex]);
                    errorSum += error;
                    preRow = row;
                }
                else
                {
                    float minError = float.MaxValue;
                    int minRow = 0;
                    for (int c = preRow - 1; c <= preRow + 1; c++)
                    {
                        if (c < 0 || c >= allOverlap)
                        {
                            continue;
                        }
                        downIndex = (c + blockSize - allOverlap) * blockSize + col;
                        upIndex = c * blockSize + col;
                        error = TexelError(up.Cs[upIndex], down.Cs[downIndex]);
                        if (error < minError)
                        {
                            minError = error;
                            minRow = c;
                        }
                    }
                    errorSum += minError;
                    preRow = minRow;
                }
                path[col] = preRow;
            }
            pathList.Add(path);
            errorList.Add(errorSum);
        }

        int minPath = 0;
        float minErrorV = float.MaxValue;
        for (int i = 0; i < errorList.Count; i++)
        {
            if (errorList[i] < minErrorV)
            {
                minErrorV = errorList[i];
                minPath = i;
            }
        }

        //quilting 
        for (int col = 0; col < blockSize; col++)
        {
            int index = pathList[minPath][col];
            for (int row = 0; row < index; row++)
            {
                int downIndex = (row + blockSize - allOverlap) * blockSize + col;
                int upIndex = row * blockSize + col;
                up.Cs[upIndex] = down.Cs[downIndex];
            }
        }
    }

    private static ImageBlockData GetBlockDataFromCanvas(Color[] canvas, int canvasSize, int row, int col, int tileSize, int overlap) 
    {
        int blockSize = tileSize + overlap * 2;
        int startX = col * tileSize;
        int startY = row * tileSize;
        Color[] blockData = new Color[blockSize * blockSize];
        for (int i = 0; i < blockSize; i++) 
        {
            for (int j = 0; j < blockSize; j++) 
            {
                int index = i * blockSize + j;
                int canvasIndex = (startY + i) * canvasSize + startX + j;
                blockData[index] = canvas[canvasIndex];
            }
        }
        ImageBlockData ibd = new ImageBlockData(blockData, blockSize);
        return ibd;
    }

    /// <summary>
    /// 从set中找出一个块，该块的左边与src的左边，右边与右边，上边与上边，下边与下边的误差小于最小误差块的1.1倍，
    /// 且两者整体的误差最大(为了保证不让两个块相似度过高)。
    /// </summary>
    /// <param name="src"></param>
    /// <param name="set"></param>
    /// <param name="overlap"></param>
    /// <returns></returns>
    private static ImageBlockData FindFitBlock3(ImageBlockData src, List<ImageBlockData> set, int overlap) 
    {
        if (set.Count <= 0)
        {
            return null;
        }
        float[] leftError = new float[set.Count];
        float[] rightError = new float[set.Count];
        float[] upError = new float[set.Count];
        float[] downError = new float[set.Count];
        float[] blockError = new float[set.Count];

        for (int i = 0; i < set.Count; i++)
        {
            leftError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Left, src, Direction.Left, set[i], overlap);
            rightError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Right, src, Direction.Right, set[i], overlap);
            upError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Up, src, Direction.Up, set[i], overlap);
            downError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Down, src, Direction.Down, set[i], overlap);
            blockError[i] = CalculateErrorBetweenTwoImageBlocks(src, set[i]);
        }

        int minErrorIndex = 0;
        for (int i = 1; i < leftError.Length; i++)
        {
            float minError = leftError[minErrorIndex] + rightError[minErrorIndex] +
                upError[minErrorIndex] + downError[minErrorIndex];
            float curError = leftError[i] + rightError[i] + upError[i] + downError[i];
            if (curError < minError)
            {
                minErrorIndex = i;
            }
        }
        List<ImageBlockData> fitList = new List<ImageBlockData>();  //符合误差范围内的所有ImageBlock集合.
        List<float> blockErrorList = new List<float>();
        float errorMaxRange = (leftError[minErrorIndex] + upError[minErrorIndex] + upError[minErrorIndex] + downError[minErrorIndex]) * 1.1f; //误差范围为最小误差块的0.1倍以内。
        for (int i = 0; i < leftError.Length; i++)
        {
            float error = leftError[i] + rightError[i] + upError[i] + downError[i];
            if (error <= errorMaxRange)
            {
                fitList.Add(set[i]);
                blockErrorList.Add(blockError[i]);
            }
        }
        int maxErrorIndex = 0;
        for (int i = 1; i < fitList.Count; i++) 
        {
            float error = blockErrorList[i];
            if (error > blockErrorList[maxErrorIndex]) 
            {
                maxErrorIndex = i;
            }
        }

        return fitList[maxErrorIndex];
    }

    /// <summary>
    /// 从set中找出一个块，该块的左边与src的右边，右边与src的左边，上边与src的下边，下边与src的上边误差最小。
    /// </summary>
    /// <param name="src"></param>
    /// <param name="set"></param>
    /// <param name="overlap"></param>
    /// <returns></returns>
    private static ImageBlockData FindFitBlock2(ImageBlockData src, List<ImageBlockData> set, int overlap) 
    {
        if (set.Count <= 0) 
        {
            return null;
        }
        float[] leftError = new float[set.Count];
        float[] rightError = new float[set.Count];
        float[] upError = new float[set.Count];
        float[] downError = new float[set.Count];

        for (int i = 0; i < set.Count; i++) 
        {
            leftError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Left, src, Direction.Right, set[i], overlap);
            rightError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Right, src, Direction.Left, set[i], overlap);
            upError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Up, src, Direction.Down, set[i], overlap);
            downError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Down, src, Direction.Up, set[i], overlap);
        }

        int minErrorIndex = 0;
        for (int i = 1; i < leftError.Length; i++)
        {
            float minError = leftError[minErrorIndex] + rightError[minErrorIndex] +
                upError[minErrorIndex] + downError[minErrorIndex];
            float curError = leftError[i] + rightError[i] + upError[i] + downError[i];
            if (curError < minError)
            {
                minErrorIndex = i;
            }
        }
        return set[minErrorIndex];
    }

    /// <summary>
    /// 找出目标块的集合，该目标块的左边与left的右边、下边与down的上边的重叠误差小于等于最小误差块的1.1倍，
    /// 然后从集合中随机选取一个目标块返回
    /// </summary>
    /// <param name="left"></param>
    /// <param name="down"></param>
    /// <param name="set"></param>
    /// <param name="overlap"></param>
    /// <returns></returns>
    private static ImageBlockData FindFitBlock(ImageBlockData left, ImageBlockData down, List<ImageBlockData> set, int overlap) 
    {
        if (set.Count <= 0) 
        {
            Debug.LogError("set.Count is 0");
            return null;
        }
        float[] leftError = new float[set.Count];  //存储set中每个ImageBlockData的误差
        float[] downError = new float[set.Count];  //存储set中每个ImageBlockData的误差
        List<ImageBlockData> fitList = new List<ImageBlockData>();  //符合误差范围内的所有ImageBlock集合.

        for (int i = 0; i < set.Count; i++) 
        {
            if (left == null)
            {
                leftError[i] = 0;
            }
            else 
            {
                leftError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Right, left, Direction.Left, set[i], overlap);
            }
            if (down == null)
            {
                downError[i] = 0;
            }
            else 
            {
                downError[i] = CalculateErrorBetweenTwoImageBlocks(Direction.Up, down, Direction.Down, set[i], overlap);
            }
        }
        int minErrorIndex = 0;
        for (int i = 1; i < leftError.Length; i++) 
        {
            float minError = leftError[minErrorIndex] + downError[minErrorIndex];
            float curError = leftError[i] + downError[i];
            if (curError < minError) 
            {
                minErrorIndex = i;
            }
        }
        float errorMaxRange = (leftError[minErrorIndex] + downError[minErrorIndex]) * 1.1f; //误差范围为最小误差块的0.1倍以内。
        Debug.Log(leftError[minErrorIndex] + downError[minErrorIndex]);
        Debug.Log(errorMaxRange);
        for (int i = 0; i < leftError.Length; i++) 
        {
            float error = leftError[i] + downError[i];
            if (error <= errorMaxRange) 
            {
                fitList.Add(set[i]);
            }
        }
        //Debug.Log(set.Count+","+fitList.Count);
        //从满足误差范围的集合里随机选取一个块
        int index = UnityEngine.Random.Range(0, fitList.Count);
        Debug.Log("index:"+index+","+fitList.Count);
        return fitList[index];
    }

    private static float CalculateErrorBetweenTwoImageBlocks(ImageBlockData block1, ImageBlockData block2) 
    {
        float errorSum = 0;
        for (int i = 0; i < block1.Size; i++) 
        {
            for (int j = 0; j < block1.Size; j++) 
            {
                int index = i * block1.Size + j;
                float error = TexelError(block1.Cs[index], block2.Cs[index]);
                errorSum += error;
            }
        }
        return errorSum;
    }

    private static float CalculateErrorBetweenTwoImageBlocks(Direction dir1, ImageBlockData block1, Direction dir2, ImageBlockData block2, int overlap) 
    {
        int allOverlap = overlap * 2;   //两张图重叠的区域是overlap*2，详情见paper的图2
        float errorSum = 0;
        switch (dir1) 
        {
            case Direction.Left:
                for (int i = 0; i < block1.Size; i++) 
                {
                    for (int j = 0; j < allOverlap; j++) 
                    {
                        int index1 = i * block1.Size + j;
                        int index2 = 0;
                        if (dir2 == Direction.Left)
                        {
                            index2 = i * block1.Size + j;
                        }
                        else if (dir2 == Direction.Right)
                        {
                            index2 = i * block1.Size + j + block1.Size - allOverlap;
                        }
                        else 
                        {
                            Debug.LogError("dir2 error");
                        }
                        float error = TexelError(block1.Cs[index1], block2.Cs[index2]); 
                        errorSum += error;
                    }
                }
                break;
            case Direction.Down:
                for (int i = 0; i < allOverlap; i++)
                {
                    for (int j = 0; j < block1.Size; j++)
                    {
                        int index1 = i * block1.Size + j;
                        int index2 = 0;
                        if (dir2 == Direction.Down)
                        {
                            index2 = i * block1.Size + j;
                        }
                        else if (dir2 == Direction.Up)
                        {
                            index2 = (i + block1.Size - allOverlap) * block1.Size + j;
                        }
                        else 
                        {
                            Debug.LogError("dir2 error.");
                        }
                        float error = TexelError(block1.Cs[index1], block2.Cs[index2]);
                        errorSum += error;
                    }
                }
                break;
            case Direction.Right:
                for (int i = 0; i < block1.Size; i++)
                {
                    for (int j = 0; j < allOverlap; j++)
                    {
                        int index2 = 0;
                        int index1 = i * block1.Size + j + block1.Size - allOverlap;
                        if (dir2 == Direction.Right)
                        {
                            index2 = i * block1.Size + j + block1.Size - allOverlap;
                        }
                        else if (dir2 == Direction.Left)
                        {
                            index2 = i * block1.Size + j;
                        }
                        else 
                        {
                            Debug.LogError("dir2 error.");
                        }

                        float error = TexelError(block1.Cs[index1], block2.Cs[index2]);
                        errorSum += error;
                    }
                }
                break;
            case Direction.Up:
                for (int i = 0; i < allOverlap; i++)
                {
                    for (int j = 0; j < block1.Size; j++)
                    {
                        int index2 = 0;
                        int index1 = (i + block1.Size - allOverlap) * block1.Size + j;
                        if (dir2 == Direction.Up)
                        {
                            index2 = (i + block1.Size - allOverlap) * block1.Size + j;
                        }
                        else if (dir2 == Direction.Down)
                        {
                            index2 = i * block1.Size + j;
                        }
                        else 
                        {
                            Debug.LogError("dir2 error.");
                        }
                        float error = TexelError(block1.Cs[index1], block2.Cs[index2]);
                        errorSum += error;
                    }
                }
                break;
        }
        return errorSum;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="dest"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private static void CopyImageBlockDataToDest(ImageBlockData data, ref Color[] canvas, int row, int col, int canvasSize, int tileSize, int overlap) 
    {
        int startX = col * tileSize;
        int startY = row * tileSize;
        int blockSize = tileSize + overlap * 2;
        if (blockSize != data.Size || canvas.Length != canvasSize * canvasSize) 
        {
            return;
        }
        //test:overlap->0
        for (int i = 0; i < blockSize; i++) 
        {
            for (int j = 0; j < blockSize; j++) 
            {
                int index = i * blockSize + j;
                int destIndex = (startY + i) * canvasSize + startX + j;
                if (startX + i < 0 || startY + j < 0 || destIndex < 0) 
                {
                    continue;
                }
                canvas[destIndex] = data.Cs[index];
            }
        }
    }

    /// <summary>
    /// 从原图src中随机获取数量为count,大小为blockSize的图块
    /// </summary>
    /// <param name="src"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    private static ImageBlockData[] GetBlockImages(Texture2D src, int count, int blockSize) 
    {
        if (src == null || blockSize >= src.width || count <= 0) 
        {
            return null;
        }
        ImageBlockData[] blocks = new ImageBlockData[count];
        int maxX = src.width - blockSize;
        int maxY = src.height - blockSize;
        for (int i = 0; i < count; i++) 
        {
            //int x = UnityEngine.Random.Range(0, maxX);
            int y = UnityEngine.Random.Range(0, maxY);
            int x = maxX / count * i;

            Color[] cs = src.GetPixels(x, y, blockSize, blockSize);
            blocks[i] = new ImageBlockData(cs, blockSize);
        }
        return blocks;
    }
}
