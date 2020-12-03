using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System;

public class ImageQuiltingEditor : EditorWindow
{
    Texture2D m_src;
    Texture2D m_transferDest;
    string m_exportFolder = "Assets";
    int m_srcImageWidth = 0;
    int m_srcImageHeight = 0;
    int m_targetImageSize=1024;
    int m_tileSize = 24;
    [MenuItem("Tools/BC2/资源管理/ImageQuilting")]
    public static void ShowWindow() 
    {
        EditorWindow ew = EditorWindow.GetWindow(typeof(ImageQuiltingEditor));
        ew.ShowUtility();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("贴图缝合工具");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("SrcImage:", GUILayout.Width(150));
        m_src = EditorGUILayout.ObjectField(m_src, typeof(Texture2D), false) as Texture2D;
        EditorGUILayout.EndHorizontal();

        if (m_src != null) 
        {
            m_srcImageWidth = m_src.width;
            m_srcImageHeight = m_src.height;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SrcImageSize:", GUILayout.Width(150));
            EditorGUILayout.LabelField(m_srcImageWidth+","+m_srcImageHeight);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("TransferTargetImage:", GUILayout.Width(150));
        m_transferDest = EditorGUILayout.ObjectField(m_transferDest, typeof(Texture2D), false) as Texture2D;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("TargetImageSize:", GUILayout.Width(120));
        int size = EditorGUILayout.IntSlider(m_targetImageSize, 64, 8192);
        if (size > 0 && size != m_targetImageSize) 
        { 
            m_targetImageSize = Mathf.ClosestPowerOfTwo(size);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("TileSize:", GUILayout.Width(120));
        int tileSize = EditorGUILayout.IntField(m_tileSize);
        if (tileSize != m_tileSize) 
        {
            tileSize = tileSize <= 0 ? 1 : tileSize;
            m_tileSize = tileSize;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("SaveFolder:", GUILayout.Width(80));
        EditorGUILayout.TextField(m_exportFolder);
        if (GUILayout.Button("...", GUILayout.Width(30))) 
        {
            BrowseSaveDir();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Quilting")) 
        {
            DoQuilting();
            AssetDatabase.Refresh();
        }
        if (GUILayout.Button("GenerateWangTile")) 
        {
            DoGenerateWangTile();
            AssetDatabase.Refresh();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void BrowseSaveDir()
    {
        string output = EditorUtility.OpenFolderPanel(
            "Save data path",
            m_exportFolder,
            ""
        );

        if (!output.StartsWith(Application.dataPath))
        {
            UnityEngine.Debug.LogError(output + " is not under " + Application.dataPath);
            return;
        }
        m_exportFolder = output.Substring(output.IndexOf("Assets"));

        GUI.FocusControl("");
    }

    private void DoQuilting() 
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        Texture2D tex = ImageQuiltingTool.DoImageQuilting(m_src, m_targetImageSize, m_tileSize);
        sw.Stop();
        //TimeSpan ts = sw.Elapsed;
        UnityEngine.Debug.Log("run time:"+sw.ElapsedMilliseconds);
        EditorUtility.ClearProgressBar();
        SaveTexture(tex);
    }

    private void DoGenerateWangTile() 
    {
        Texture2D tex = ImageQuiltingTool.DoQuiltingWangTileImage(m_src, m_tileSize);
        EditorUtility.ClearProgressBar();
        SaveTexture(tex);
    }

    private void SaveTexture(Texture2D tex) 
    {
        string path = EditorUtility.SaveFilePanel("保存图片", m_exportFolder, "quilting", "tga");
        UnityEngine.Debug.Log(path);
        if (tex == null) 
        {
            return;
        }
        byte[] bs = tex.EncodeToTGA();
        
        if (bs != null && !string.IsNullOrEmpty(path)) 
        {
            File.WriteAllBytes(path, bs);
        }
    }

}
