using UnityEngine;
using Nanodogs.Toolkit;
using UnityEditor;

public class CUBEEmoteCreatorWindow : EditorWindow
{
    // the emote asset being created/edited
    public EmoteAsset currentEmote = null;

    [MenuItem("Nanodogs/CUBE/Emote Creator", priority = -1)]
    public static void ShowWindow()
    {
        GetWindow<CUBEEmoteCreatorWindow>("CUBE Emote Creator");
    }

    private void OnGUI()
    {
        currentEmote = (EmoteAsset)EditorGUILayout.ObjectField("Current Emote Asset", currentEmote, typeof(EmoteAsset), false);
        if (GUI.Button(new Rect(10, 40, 150, 30), "Create New Emote Asset"))
        {
            EmoteAsset createdAsset = CreateInstance<EmoteAsset>();
            string path = EditorUtility.SaveFilePanelInProject("Save Emote Asset", "NewEmoteAsset", "asset", "Please enter a file name to save the emote asset to");
            if (path.Length > 0)
            {
                AssetDatabase.CreateAsset(createdAsset, path);
                AssetDatabase.SaveAssets();
                currentEmote = createdAsset;
            }
        }

        if(currentEmote != null)
        {
            if (GUI.Button(new Rect(10, 80, 150, 30), "Open Editor"))
            {
                EmoteAssetEditorWindow.ShowWindow(currentEmote);
            }
        }
    }
}
