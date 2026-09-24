using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 새로 추가된 낚시 상점 스프라이트를 기존 가구를 보존한 채 별도 묶음으로 배치한다.
/// 스크립트가 컴파일되면 한 번 자동 실행되며, 이후에는 메뉴에서 다시 실행할 수 있다.
/// </summary>
public static class FishingShopNewDecorPlacer
{
    private const string ScenePath = "Assets/PJH/Scene/PJH.unity";
    private const string SpriteSheetPath = "Assets/PJH/03.Sprite/FishingShopGagu/tileset.png";
    private const string FurnitureRootName = "FishingShopFurniture";
    private const string DecorRootName = "14_NewFishingShopDecor";
    private const string BackupFolder = "C:/Users/pjoon/Documents/Codex/2026-08-30/8-2/output/shop-backups";

    private sealed class Decor
    {
        public readonly string name;
        public readonly string spriteName;
        public readonly Vector2 position;
        public readonly int sortingOrder;
        public readonly bool solid;

        public Decor(string name, string spriteName, float x, float y, int sortingOrder, bool solid)
        {
            this.name = name;
            this.spriteName = spriteName;
            position = new Vector2(x, y);
            this.sortingOrder = sortingOrder;
            this.solid = solid;
        }
    }

    // 좌표는 FishingShopFurniture 기준 로컬 좌표다.
    // 통로는 비워 두고, 벽과 기존 수조/진열대 주변만 채운다.
    private static readonly Decor[] Layout =
    {
        new Decor("01_LeftTackleDisplay", "tileset_5", -3.15f, 3.02f, 13, true),
        new Decor("02_LeftSupplyShelf", "tileset_7", -1.50f, 3.02f, 13, true),
        new Decor("03_RightGlassCooler", "tileset_11", 4.35f, 2.18f, 14, true),

        new Decor("04_FishDisplayTable", "tileset_0", -3.00f, 1.20f, 17, true),
        new Decor("05_DisplayStool", "tileset_1", -4.10f, 0.52f, 18, true),

        // 진열대 위의 작은 생선 상품
        new Decor("06_TableFishYellow", "tileset_12", -3.50f, 1.62f, 18, false),
        new Decor("07_TableFishRed", "tileset_13", -2.82f, 1.63f, 18, false),
        new Decor("08_TableFishPurple", "tileset_15", -2.18f, 1.63f, 18, false),

        // 기존 청록색 수조가 빈 유리 상자로 보이지 않도록 내부에 물고기를 넣는다.
        new Decor("09_AquariumFishYellow", "tileset_12", 3.43f, -1.65f, 23, false),
        new Decor("10_AquariumFishGreen", "tileset_14", 4.22f, -1.72f, 23, false),
        new Decor("11_AquariumFishOrange", "tileset_16", 5.02f, -1.62f, 23, false),
    };

    [MenuItem("Tools/PJH/Place New Fishing Shop Decor")]
    public static void PlaceNewDecor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play 모드를 종료한 뒤 가구를 배치하세요.");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("PJH 씬을 연 뒤 실행하세요.");

        Transform furnitureRoot = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .SingleOrDefault(transform => transform.name == FurnitureRootName);

        if (furnitureRoot == null)
            throw new InvalidOperationException("FishingShopFurniture 오브젝트를 찾을 수 없습니다.");

        if (furnitureRoot.Find(DecorRootName) != null)
        {
            Debug.Log("[FishingShop] 새 장식이 이미 배치되어 있어 중복 생성하지 않았습니다.");
            return;
        }

        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name);

        foreach (Decor decor in Layout)
            if (!sprites.ContainsKey(decor.spriteName))
                throw new InvalidOperationException($"{SpriteSheetPath}에서 {decor.spriteName} 스프라이트를 찾을 수 없습니다.");

        Tilemap floor = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
            .Single(tilemap => tilemap.name == "FishingShopTIle");
        TilemapRenderer floorRenderer = floor.GetComponent<TilemapRenderer>();

        Directory.CreateDirectory(BackupFolder);
        string backupPath = Path.Combine(
            BackupFolder,
            "PJH_before_new_shop_decor_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        File.Copy(ScenePath, backupPath, false);

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place new fishing shop decor");

        try
        {
            GameObject decorRootObject = new GameObject(DecorRootName);
            Undo.RegisterCreatedObjectUndo(decorRootObject, "Create fishing shop decor root");
            decorRootObject.transform.SetParent(furnitureRoot, false);

            foreach (Decor decor in Layout)
            {
                GameObject item = new GameObject(decor.name);
                Undo.RegisterCreatedObjectUndo(item, "Create fishing shop decor");
                item.transform.SetParent(decorRootObject.transform, false);
                item.transform.localPosition = new Vector3(decor.position.x, decor.position.y, 0f);

                SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[decor.spriteName];
                renderer.sharedMaterial = floorRenderer.sharedMaterial;
                renderer.sortingLayerID = floorRenderer.sortingLayerID;
                renderer.sortingOrder = floorRenderer.sortingOrder + decor.sortingOrder;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;

                if (!decor.solid)
                    continue;

                BoxCollider2D collider = item.AddComponent<BoxCollider2D>();
                Vector2 spriteSize = renderer.sprite.bounds.size;
                collider.size = new Vector2(
                    Mathf.Max(0.35f, spriteSize.x * 0.85f),
                    Mathf.Clamp(spriteSize.y * 0.32f, 0.25f, 0.60f));
                collider.offset = new Vector2(0f, -spriteSize.y * 0.5f + collider.size.y * 0.5f);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException("새 낚시 상점 장식이 들어간 씬을 저장하지 못했습니다.");

            Selection.activeGameObject = decorRootObject;
            Debug.Log($"[FishingShop] 새 가구와 장식 {Layout.Length}개를 배치했습니다. 백업: {backupPath}");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

}
