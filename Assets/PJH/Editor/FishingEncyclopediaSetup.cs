using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PJH.Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PJH.Editor
{
    public static class FishingEncyclopediaSetup
    {
        private const string RootName = "FishingEncyclopediaUI";
        private const string BackgroundName = "BackGround Panel";
        private const string GridName = "Fish Grid";
        private const string DetailPanelName = "Fish Detail Panel";
        private const string FishDataFolder = "Assets/PJH/02.Fishing infom/Fish Data";

        [MenuItem("PJH/Setup Fishing Encyclopedia %#e")]
        public static void Setup()
        {
            GameObject root = FindInActiveScene(RootName);
            if (root == null)
            {
                Debug.LogError($"'{RootName}' 오브젝트를 찾지 못했습니다.");
                return;
            }

            Transform background = FindChildRecursive(root.transform, BackgroundName);
            Transform grid = FindChildRecursive(root.transform, GridName);
            if (background == null || grid == null)
            {
                Debug.LogError("도감의 BackGround Panel 또는 Fish Grid를 찾지 못했습니다.");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Setup Fishing Encyclopedia");

            FishSlotUI[] slots = ConfigureSlots(grid);
            FishDataSO[] fishData = LoadFishData();
            FishDetailPanelUI detailPanel = ConfigureDetailPanel(background);

            FishCollectionUI collection = root.GetComponent<FishCollectionUI>();
            if (collection == null)
                collection = Undo.AddComponent<FishCollectionUI>(root);

            SerializedObject collectionObject = new SerializedObject(collection);
            SetObjectArray(collectionObject.FindProperty("fishSlots"), slots);
            SetObjectArray(collectionObject.FindProperty("fishDataList"), fishData);
            collectionObject.FindProperty("fishDetailPanelUI").objectReferenceValue = detailPanel;
            collectionObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            Selection.activeGameObject = root;
            Debug.Log($"낚시 도감 연결 완료: 슬롯 {slots.Length}개, 물고기 데이터 {fishData.Length}개");
        }

        private static FishSlotUI[] ConfigureSlots(Transform grid)
        {
            List<Transform> slotTransforms = new List<Transform>();
            foreach (Transform child in grid)
            {
                if (child.name.StartsWith("Fish Slot ", StringComparison.Ordinal))
                    slotTransforms.Add(child);
            }

            slotTransforms.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));

            List<FishSlotUI> slots = new List<FishSlotUI>();
            foreach (Transform slotTransform in slotTransforms)
            {
                Button button = slotTransform.GetComponent<Button>();
                Transform iconTransform = FindChildRecursive(slotTransform, "Fish Icon");
                Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

                if (button == null || icon == null)
                {
                    Debug.LogWarning($"{slotTransform.name}: Button 또는 Fish Icon을 찾지 못했습니다.");
                    continue;
                }

                FishSlotUI slot = slotTransform.GetComponent<FishSlotUI>();
                if (slot == null)
                    slot = Undo.AddComponent<FishSlotUI>(slotTransform.gameObject);

                SerializedObject slotObject = new SerializedObject(slot);
                slotObject.FindProperty("button").objectReferenceValue = button;
                slotObject.FindProperty("fishIcon").objectReferenceValue = icon;
                slotObject.ApplyModifiedPropertiesWithoutUndo();
                slots.Add(slot);
            }

            return slots.ToArray();
        }

        private static FishDataSO[] LoadFishData()
        {
            return AssetDatabase.FindAssets("t:FishDataSO", new[] { FishDataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<FishDataSO>)
                .Where(data => data != null)
                .OrderBy(data => ExtractNumber(data.name))
                .ToArray();
        }

        private static FishDetailPanelUI ConfigureDetailPanel(Transform background)
        {
            Transform existing = FindChildRecursive(background, DetailPanelName);
            GameObject panelObject = existing != null ? existing.gameObject : CreateUIObject(DetailPanelName, background);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.one;
            panelRect.anchorMax = Vector2.one;
            panelRect.pivot = Vector2.one;
            panelRect.anchoredPosition = new Vector2(-40f, -120f);
            panelRect.sizeDelta = new Vector2(480f, 560f);

            Image panelImage = GetOrAddComponent<Image>(panelObject);
            panelImage.color = new Color(0.12f, 0.22f, 0.29f, 0.92f);

            Image icon = CreateOrFindImage(panelRect, "Fish Detail Icon", new Vector2(192f, 192f), new Vector2(0f, -70f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.enabled = false;

            TextMeshProUGUI nameText = CreateOrFindText(panelRect, "Fish Name Text", new Vector2(420f, 60f), new Vector2(0f, -300f), 36f);
            TextMeshProUGUI priceText = CreateOrFindText(panelRect, "Fish Price Text", new Vector2(420f, 60f), new Vector2(0f, -370f), 30f);

            FishDetailPanelUI detailPanel = panelObject.GetComponent<FishDetailPanelUI>();
            if (detailPanel == null)
                detailPanel = Undo.AddComponent<FishDetailPanelUI>(panelObject);

            SerializedObject detailObject = new SerializedObject(detailPanel);
            detailObject.FindProperty("fishIcon").objectReferenceValue = icon;
            detailObject.FindProperty("fishName").objectReferenceValue = nameText;
            detailObject.FindProperty("fishPrice").objectReferenceValue = priceText;
            detailObject.ApplyModifiedPropertiesWithoutUndo();

            return detailPanel;
        }

        private static Image CreateOrFindImage(Transform parent, string name, Vector2 size, Vector2 position)
        {
            Transform existing = FindChildRecursive(parent, name);
            GameObject target = existing != null ? existing.gameObject : CreateUIObject(name, parent);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return GetOrAddComponent<Image>(target);
        }

        private static TextMeshProUGUI CreateOrFindText(Transform parent, string name, Vector2 size, Vector2 position, float fontSize)
        {
            Transform existing = FindChildRecursive(parent, name);
            GameObject target = existing != null ? existing.gameObject : CreateUIObject(name, parent);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = GetOrAddComponent<TextMeshProUGUI>(target);
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = name == "Fish Name Text" ? "물고기를 선택하세요" : string.Empty;
            return text;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject target = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(target, $"Create {name}");
            target.layer = LayerMask.NameToLayer("UI");
            target.transform.SetParent(parent, false);
            return target;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(target);
        }

        private static void SetObjectArray<T>(SerializedProperty property, IReadOnlyList<T> values) where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static GameObject FindInActiveScene(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform found = FindChildRecursive(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static int ExtractNumber(string value)
        {
            Match match = Regex.Match(value, @"\d+");
            return match.Success ? int.Parse(match.Value) : int.MaxValue;
        }
    }
}
