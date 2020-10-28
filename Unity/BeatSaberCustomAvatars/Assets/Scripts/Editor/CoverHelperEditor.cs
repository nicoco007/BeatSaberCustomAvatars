using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Experimental.Rendering;

[CustomEditor(typeof(CoverHelper))]
public class CoverHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Capture"))
        {
            Capture();
        }
    }

    private void Capture()
    {
        CoverHelper coverHelper = (CoverHelper)target;
        Camera camera = coverHelper.gameObject.GetComponent<Camera>();

        RenderTexture rt = RenderTexture.GetTemporary(coverHelper.imageSize, coverHelper.imageSize, 24, GraphicsFormat.R8G8B8A8_UNorm, 4);
        RenderTexture.active = rt;

        camera.backgroundColor = new Color(0, 0, 0, 1);
        camera.targetTexture = rt;
        camera.Render();

        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        
        Color[] colors = texture.GetPixels();

        if (coverHelper.enforceOpacity)
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i].a = 1;
            }
        } else
        {
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i].a = 1 - colors[i].a;
            }
        }

        texture.SetPixels(colors);

        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(SceneManager.GetActiveScene().path), "Cover.png"), texture.EncodeToPNG());

        camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);

        AssetDatabase.Refresh();
    }
}