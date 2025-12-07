using UnityEditor;
using UnityEngine;

public class EmoteAssetEditorWindow : EditorWindow
{
    private EmoteAsset emoteAsset;

    private int selectedKeyframeIndex = -1;

    private GameObject previewRoot;           // Parent root for all preview instances
    private GameObject previewInstance;       // Current keyframe instance being edited
    private Transform selectedBone;           // Currently selected bone/transform

    private bool editPoseMode = false;        // Are we currently editing a pose?

    private const string PREVIEW_ROOT_NAME = "EmoteKeyframePreviewRoot";

    // --- NEW: handle mode enum instead of bool ---
    private enum HandleMode
    {
        Position,
        Rotation
    }

    private HandleMode handleMode = HandleMode.Position;

    [MenuItem("Window/Emote Asset Editor (Example)")]
    public static void OpenWindow()
    {
        GetWindow<EmoteAssetEditorWindow>("Emote Asset Editor");
    }

    public static void ShowWindow(EmoteAsset asset)
    {
        var window = GetWindow<EmoteAssetEditorWindow>("Emote Asset Editor");
        window.emoteAsset = asset;
        window.Show();
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindOrCreatePreviewRoot();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SetToolsHidden(false);
        CleanupPreviewInstance();
        CleanupPreviewRoot();
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            emoteAsset = (EmoteAsset)EditorGUILayout.ObjectField("Emote Asset", emoteAsset, typeof(EmoteAsset), false);

            if (emoteAsset == null)
            {
                EditorGUILayout.HelpBox("Assign an EmoteAsset to begin editing.", MessageType.Info);
                return;
            }
        }

        EditorGUILayout.Space();
        DrawEmoteSettings();
        EditorGUILayout.Space();
        DrawKeyframeList();
        EditorGUILayout.Space();
        DrawPoseEditorControls();
    }

    #region Emote basic settings

    private void DrawEmoteSettings()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Emote Settings", EditorStyles.boldLabel);

            emoteAsset.emoteName = EditorGUILayout.TextField("Emote Name", emoteAsset.emoteName);
            emoteAsset.emoteDuration = EditorGUILayout.FloatField("Duration (seconds)", emoteAsset.emoteDuration);
            emoteAsset.loopEmote = EditorGUILayout.Toggle("Loop Emote", emoteAsset.loopEmote);
        }
    }

    #endregion

    #region Keyframe list

    private void DrawKeyframeList()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Keyframes", EditorStyles.boldLabel);

            // Keyframe count
            int currentCount = emoteAsset.keyframes != null ? emoteAsset.keyframes.Length : 0;
            int newCount = Mathf.Max(0, EditorGUILayout.IntField("Number of Keyframes", currentCount));

            if (newCount != currentCount)
            {
                ResizeKeyframeArray(newCount);
            }

            if (emoteAsset.keyframes == null || emoteAsset.keyframes.Length == 0)
            {
                EditorGUILayout.HelpBox("No keyframes defined.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            for (int i = 0; i < emoteAsset.keyframes.Length; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Keyframe {i + 1}", GUILayout.Width(80));
                    emoteAsset.keyframes[i] = (GameObject)EditorGUILayout.ObjectField(emoteAsset.keyframes[i], typeof(GameObject), false);

                    GUI.enabled = emoteAsset.keyframes[i] != null;
                    if (GUILayout.Toggle(selectedKeyframeIndex == i, "Select", "Button", GUILayout.Width(60)))
                    {
                        if (selectedKeyframeIndex != i)
                        {
                            SelectKeyframe(i);
                        }
                    }
                    GUI.enabled = true;
                }
            }
        }
    }

    private void ResizeKeyframeArray(int newCount)
    {
        int oldCount = emoteAsset.keyframes != null ? emoteAsset.keyframes.Length : 0;
        GameObject[] newArray = new GameObject[newCount];

        for (int i = 0; i < Mathf.Min(newCount, oldCount); i++)
        {
            newArray[i] = emoteAsset.keyframes[i];
        }

        emoteAsset.keyframes = newArray;

        if (selectedKeyframeIndex >= newCount)
        {
            selectedKeyframeIndex = -1;
            CleanupPreviewInstance();
        }
    }

    private void SelectKeyframe(int index)
    {
        selectedKeyframeIndex = index;
        selectedBone = null;
        editPoseMode = false;
        handleMode = HandleMode.Position;
        SetToolsHidden(false);

        CleanupPreviewInstance();

        if (emoteAsset.keyframes == null || index < 0 || index >= emoteAsset.keyframes.Length)
            return;

        GameObject prefab = emoteAsset.keyframes[index];
        if (prefab == null)
            return;

        FindOrCreatePreviewRoot();

        // Instantiate a connected prefab instance so that changes can be applied back.
        previewInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, previewRoot.transform);
        previewInstance.name = prefab.name + "_Preview";

        FramePreviewInSceneView();
    }

    #endregion

    #region Pose editor controls

    private void DrawPoseEditorControls()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Pose Editor", EditorStyles.boldLabel);

            if (selectedKeyframeIndex < 0 || emoteAsset.keyframes == null || selectedKeyframeIndex >= emoteAsset.keyframes.Length)
            {
                EditorGUILayout.HelpBox("Select a keyframe to edit its pose.", MessageType.Info);
                return;
            }

            GameObject keyframePrefab = emoteAsset.keyframes[selectedKeyframeIndex];
            if (keyframePrefab == null)
            {
                EditorGUILayout.HelpBox("Selected keyframe has no prefab assigned.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Selected Keyframe:", keyframePrefab.name);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool canEdit = previewInstance != null;

                EditorGUI.BeginDisabledGroup(!canEdit);
                bool newEditPose = GUILayout.Toggle(editPoseMode, "Edit Pose", "Button");
                EditorGUI.EndDisabledGroup();

                if (newEditPose != editPoseMode)
                {
                    editPoseMode = newEditPose;
                    if (!editPoseMode)
                    {
                        selectedBone = null;
                        handleMode = HandleMode.Position;
                    }
                    SetToolsHidden(editPoseMode);
                }

                if (GUILayout.Button("Refocus Scene View"))
                {
                    FramePreviewInSceneView();
                }

                if (GUILayout.Button("Reset Preview"))
                {
                    SelectKeyframe(selectedKeyframeIndex); // Re-select to rebuild instance
                }
            }

            EditorGUILayout.Space();

            if (editPoseMode && previewInstance != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Handle Mode", GUILayout.Width(90));
                    handleMode = (HandleMode)GUILayout.Toolbar((int)handleMode, new[] { "Position", "Rotation" });
                }

                EditorGUILayout.Space();

                if (selectedBone != null)
                {
                    EditorGUILayout.LabelField("Selected Bone", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Name", selectedBone.name);
                    EditorGUILayout.Vector3Field("Local Position", selectedBone.localPosition);
                    EditorGUILayout.Vector3Field("Local Rotation", selectedBone.localEulerAngles);
                }
                else
                {
                    EditorGUILayout.HelpBox("Click a bone/Transform in the Scene view to select it.", MessageType.Info);
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Apply Pose To Prefab"))
                    {
                        ApplyPoseToPrefab();
                    }

                    if (GUILayout.Button("Discard Changes"))
                    {
                        SelectKeyframe(selectedKeyframeIndex); // Rebuild instance from prefab
                    }
                }

                EditorGUILayout.HelpBox(
                    "In Edit Pose mode:\n" +
                    "- Use the Scene view as your preview/editor camera.\n" +
                    "- Click bones in the Scene view to select them.\n" +
                    "- Use the Position/Rotation handles (or Unity's move/rotate tools) to pose.",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "To edit this keyframe's pose: enable 'Edit Pose', then use the Scene view to select and move/rotate bones.",
                    MessageType.Info);
            }
        }
    }

    private void ApplyPoseToPrefab()
    {
        if (previewInstance == null || selectedKeyframeIndex < 0 || emoteAsset.keyframes == null)
            return;

        GameObject prefab = emoteAsset.keyframes[selectedKeyframeIndex];
        if (prefab == null)
            return;

        PrefabUtility.ApplyPrefabInstance(previewInstance, InteractionMode.UserAction);
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"Applied pose back to prefab '{prefab.name}'.");
    }

    #endregion

    #region Scene view handling & gizmos

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!editPoseMode || previewInstance == null)
            return;

        Event e = Event.current;

        // Handle selection of bones by clicking in the Scene view
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
            if (picked != null && picked.transform.IsChildOf(previewInstance.transform))
            {
                // your rig uses colliders as children of bones, so select the parent as the actual bone
                selectedBone = picked.transform.parent;
                Repaint();
            }
        }

        // Draw custom handle for the selected bone
        if (selectedBone != null)
        {
            EditorGUI.BeginChangeCheck();

            if (handleMode == HandleMode.Position)
            {
                Vector3 pos = Handles.PositionHandle(selectedBone.position, selectedBone.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedBone, "Move Bone");
                    selectedBone.position = pos;
                }
            }
            else // Rotation
            {
                Quaternion rot = Handles.RotationHandle(selectedBone.rotation, selectedBone.position);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selectedBone, "Rotate Bone");
                    selectedBone.rotation = rot;
                }
            }
        }
    }

    private void FramePreviewInSceneView()
    {
        if (previewInstance == null)
            return;

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
            return;

        Renderer[] renderers = previewInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        sceneView.Frame(bounds, false);
    }

    #endregion

    #region Preview root management

    private void FindOrCreatePreviewRoot()
    {
        if (previewRoot != null)
            return;

        previewRoot = GameObject.Find(PREVIEW_ROOT_NAME);
        if (previewRoot == null)
        {
            previewRoot = new GameObject(PREVIEW_ROOT_NAME);
            previewRoot.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave;
        }
    }

    private void CleanupPreviewInstance()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
    }

    private void CleanupPreviewRoot()
    {
        if (previewRoot != null)
        {
            DestroyImmediate(previewRoot);
            previewRoot = null;
        }
    }

    #endregion

    #region Tools visibility helper

    private void SetToolsHidden(bool hidden)
    {
        Tools.hidden = hidden;
        SceneView.RepaintAll();
    }

    #endregion
}
