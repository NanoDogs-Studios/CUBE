using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

// This script defines a custom editor window to preview the scene view
// from the perspective of a GameObject (CamPos) inside the window itself
[ExecuteInEditMode]
[ExecuteAlways]
public class CameraPositionPreviewWindow : EditorWindow
{
    private GameObject camPosObject; // The empty GameObject used as a camera position
    private Camera previewCamera;    // Temporary camera used for preview
    private RenderTexture previewTexture;
    private float fieldOfView = 60f;
    private bool enablePostProcessing = false;
    private float previewScale = 0.5f; // scale down the image in the window
    private bool displayInGameView = false;

    [MenuItem("Tools/Camera Position Preview")]
    public static void ShowWindow()
    {
        GetWindow<CameraPositionPreviewWindow>("CamPos Preview");
        EditorApplication.update += ForceRepaint; // Ensure update even when unfocused
    }

    private static void ForceRepaint()
    {
        var window = GetWindow<CameraPositionPreviewWindow>(false, null, false);
        if (window != null)
        {
            window.Repaint(); // This forces OnGUI to run each editor frame
        }
    }

    private void OnEnable()
    {
        if (previewCamera == null)
        {
            GameObject tempCam = new GameObject("CamPosPreviewCamera");
            tempCam.hideFlags = HideFlags.HideAndDontSave;
            previewCamera = tempCam.AddComponent<Camera>();
            previewCamera.enabled = false; // we only render manually to the texture by default
        }
        EditorApplication.update += UpdatePreview; // hook update loop
    }

    private void OnDisable()
    {
        RestoreSceneView();
        EditorApplication.update -= UpdatePreview;
        EditorApplication.update -= ForceRepaint;
    }

    private void UpdatePreview()
    {
        // This method runs every editor frame regardless of focus
        if (previewCamera != null && camPosObject != null)
        {
            previewCamera.transform.position = camPosObject.transform.position;
            previewCamera.transform.rotation = camPosObject.transform.rotation;
            previewCamera.fieldOfView = fieldOfView;
            UniversalAdditionalCameraData cameraData = previewCamera.transform.gameObject.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = enablePostProcessing;
            }
            else
            {
                cameraData = previewCamera.transform.gameObject.AddComponent<UniversalAdditionalCameraData>();
                cameraData.renderPostProcessing = enablePostProcessing;
            }
            if (!displayInGameView)
            {
                previewCamera.targetTexture = previewTexture;
                previewCamera.enabled = false;
                previewCamera.Render();
            }
            else
            {
                // Directly show in Game View by making camera active
                previewCamera.targetTexture = null;
                previewCamera.enabled = true;
                previewCamera.tag = "MainCamera";
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Camera Position Preview Tool", EditorStyles.boldLabel);

        camPosObject = (GameObject)EditorGUILayout.ObjectField("CamPos Object", camPosObject, typeof(GameObject), true);

        fieldOfView = EditorGUILayout.Slider("Field of View", fieldOfView, 1f, 179f);
        enablePostProcessing = EditorGUILayout.Toggle("Enable Post Processing", enablePostProcessing);
        previewScale = EditorGUILayout.Slider("Preview Scale", previewScale, 0.1f, 1f);
        displayInGameView = EditorGUILayout.Toggle("Display In Game View", displayInGameView);

        if (camPosObject == null)
        {
            EditorGUILayout.HelpBox("Assign a GameObject to preview its view.", MessageType.Info);
            return;
        }

        if (!displayInGameView)
        {
            // Ensure preview texture exists
            int texWidth = Mathf.Max(1, (int)(position.width * previewScale));
            int texHeight = Mathf.Max(1, (int)((position.height - 100) * previewScale));
            if (previewTexture == null || previewTexture.width != texWidth || previewTexture.height != texHeight)
            {
                if (previewTexture != null)
                {
                    previewTexture.Release();
                    DestroyImmediate(previewTexture);
                }
                previewTexture = new RenderTexture(texWidth, texHeight, 24);
            }

            // Draw preview in the window
            Rect previewRect = GUILayoutUtility.GetRect(position.width * previewScale, (position.height - 100) * previewScale);
            if (previewTexture != null)
            {
                GUI.DrawTexture(previewRect, previewTexture, ScaleMode.ScaleToFit, false);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Camera is currently being displayed directly in the Game View.", MessageType.Info);
        }

        if (GUILayout.Button("Restore Scene View"))
        {
            RestoreSceneView();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (camPosObject != null)
        {
            // Draw camera frustum gizmos
            Handles.color = Color.cyan;
            Matrix4x4 temp = Handles.matrix;
            Handles.matrix = camPosObject.transform.localToWorldMatrix;
            Handles.DrawWireCube(Vector3.forward * 0.5f, new Vector3(0.5f, 0.5f, 1f));
            Handles.matrix = temp;
        }
    }

    private void RestoreSceneView()
    {
        if (previewCamera != null)
        {
            DestroyImmediate(previewCamera.gameObject);
            previewCamera = null;
        }
        if (previewTexture != null)
        {
            previewTexture.Release();
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    [InitializeOnLoadMethod]
    private static void InitSceneGizmo()
    {
        SceneView.duringSceneGui += (sceneView) =>
        {
            var window = GetWindow<CameraPositionPreviewWindow>(false, null, false);
            if (window != null && window.camPosObject != null)
            {
                window.OnSceneGUI(sceneView);
            }
        };
    }
}
