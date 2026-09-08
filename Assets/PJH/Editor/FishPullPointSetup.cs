using PJH.Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class FishPullPointSetup
{
    private const string SessionKey = "PJH.FishPullPointSetup.Applied";
    private const string ClipPath = "Assets/PJH/Animation/FishingHook.anim";
    private const string ScenePath = "Assets/PJH/Scene/PJH.unity";

    static FishPullPointSetup()
    {
        EditorApplication.delayCall += ApplyOnce;
    }

    [MenuItem("Tools/PJH/Apply Fish Pull Point Keys")]
    private static void ApplyFromMenu()
    {
        Apply();
    }

    private static void ApplyOnce()
    {
        if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (Apply())
            SessionState.SetBool(SessionKey, true);
    }

    private static bool Apply()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedTemporarily = !scene.IsValid() || !scene.isLoaded;

        if (openedTemporarily)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        GameObject agentRenderer = FindInScene(scene, "AgentRenderer");
        if (agentRenderer == null)
        {
            Debug.LogWarning("AgentRenderer를 찾지 못해 FishPullPoint 키를 적용하지 못했습니다.");
            if (openedTemporarily)
                EditorSceneManager.CloseScene(scene, true);
            return false;
        }

        Transform pullPoint = agentRenderer.transform.Find("FishPullPoint");
        if (pullPoint == null)
        {
            GameObject pointObject = new GameObject("FishPullPoint");
            pullPoint = pointObject.transform;
            pullPoint.SetParent(agentRenderer.transform, false);
        }

        pullPoint.localPosition = new Vector3(1.5f, 0.12f, 0f);

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogWarning($"회수 애니메이션을 찾지 못했습니다: {ClipPath}");
            return false;
        }

        float[] times =
        {
            0f, 1f / 12f, 2f / 12f, 3f / 12f, 4f / 12f,
            5f / 12f, 6f / 12f, 7f / 12f, 8f / 12f, 9f / 12f
        };

        float[] xValues =
        {
            1.50f, 1.50f, 1.55f, 1.55f, 1.15f,
            1.15f, 0.20f, 0.15f, 0.15f, 0.15f
        };

        float[] yValues =
        {
            0.12f, 0.12f, 0.12f, 0.12f, 0.72f,
            1.12f, 1.50f, 1.55f, 1.55f, 1.55f
        };

        SetLinearCurve(clip, "m_LocalPosition.x", times, xValues);
        SetLinearCurve(clip, "m_LocalPosition.y", times, yValues);
        SetLinearCurve(clip, "m_LocalPosition.z", times, new float[times.Length]);

        FishingController controller = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            controller = root.GetComponentInChildren<FishingController>(true);
            if (controller != null)
                break;
        }

        if (controller != null)
        {
            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty caughtFishPoint = serializedController.FindProperty("caughtFishPoint");
            if (caughtFishPoint != null)
            {
                caughtFishPoint.objectReferenceValue = pullPoint;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorUtility.SetDirty(clip);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        if (openedTemporarily)
            EditorSceneManager.CloseScene(scene, true);

        Debug.Log("FishPullPoint 생성, FishingHook 위치 키프레임, FishingController 연결을 완료했습니다.");
        return true;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static void SetLinearCurve(
        AnimationClip clip,
        string propertyName,
        float[] times,
        float[] values)
    {
        AnimationCurve curve = new AnimationCurve();

        for (int i = 0; i < times.Length; i++)
            curve.AddKey(new Keyframe(times[i], values[i]));

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Linear);

            AnimationUtility.SetKeyRightTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Linear);
        }

        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
            "FishPullPoint",
            typeof(Transform),
            propertyName);

        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }
}
