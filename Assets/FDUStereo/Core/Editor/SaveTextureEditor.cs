using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class SaveTextureEditor : EditorWindow
{
    private static SaveTextureEditor _instance;

    [MenuItem("SH/Save Texture")]
    public static void Init()
    {
        if (_instance == null)
        {
            _instance = GetWindow<SaveTextureEditor>();
        }
    }

    private string mFolder = "Texture";
    private string mFileName = "TextureName";
    private Camera mTargetCamera;
    private string mMsg = "Select Camera";
    private UnityEditor.MessageType mMsgType = UnityEditor.MessageType.Info;
    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        mTargetCamera = (Camera)EditorGUILayout.ObjectField("Camera", mTargetCamera, typeof(Camera));
        if (EditorGUI.EndChangeCheck())
        {
            if (mTargetCamera != null)
            {
                RenderTexture rt = mTargetCamera.targetTexture;
                if (rt == null)
                {
                    mMsg = "No RenderTexture attached"; mMsgType = UnityEditor.MessageType.Error; mFileName = string.Format("{0}", mTargetCamera.name);
                }
                else
                {
                    mFileName = string.Format("{0}_{1}x{2}", mTargetCamera.name, rt.width, rt.height);
                }
            }
            else
            {
                mFileName = "TextureName";
            }
        }
        mFolder = EditorGUILayout.TextField("Folder", mFolder);
        mFileName = EditorGUILayout.TextField("Filename", mFileName);

        if (GUILayout.Button("Save", GUILayout.Width(44)))
        {
            Save();
        }

        EditorGUILayout.HelpBox(mMsg, mMsgType);
    }

    void Save()
    {
        RenderTexture rt = mTargetCamera.targetTexture;
        if (rt == null) { mMsg = "No RenderTexture attached"; mMsgType = UnityEditor.MessageType.Error; return; }

        RenderTexture svRT = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

        if (!Directory.Exists(mFolder))
        {
            Directory.CreateDirectory(mFolder);
        }

        File.WriteAllBytes(string.Format("{0}/{1}.png", mFolder, mFileName), tex.EncodeToPNG());

        RenderTexture.active = svRT;

        mMsg = "Saved: " + string.Format("{0}/{1}.png", mFolder, mFileName);
        mMsgType = UnityEditor.MessageType.Info;
    }
}
