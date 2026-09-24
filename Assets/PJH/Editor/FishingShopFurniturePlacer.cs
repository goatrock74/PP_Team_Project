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

// An editor-only, explicit placement command. Scene furniture has no runtime dependency on this file.
public static class FishingShopFurniturePlacer
{
    private const string ScenePath = "Assets/PJH/Scene/PJH.unity";
    private const string Source = "Assets/PJH/03.Sprite/FishingShopGagu/roguelikeIndoor_transparent.png";
    private const string Sheet = "Assets/PJH/03.Sprite/FishingShopGagu/Furniture_Grid16.png";
    private const string RootName = "FishingShopFurniture";
    private const string BackupFolder = "C:/Users/pjoon/Documents/Codex/2026-08-30/8-2/output/shop-backups";

    private sealed class Furnishing
    {
        public string name;
        public int x, y, width, height;
        public Vector2Int[] cells;
        public bool solid;
        public Furnishing(string name, int x, int y, int width, int height, bool solid, params Vector2Int[] cells)
        { this.name = name; this.x = x; this.y = y; this.width = width; this.height = height; this.solid = solid; this.cells = cells; }
    }

    private static Vector2Int C(int x, int y) => new Vector2Int(x, y);
    private static string Key(Vector2Int c) => $"Furniture_{c.x}_{c.y}";

    // Coordinates are relative to the bottom-left occupied floor cell, in whole grid cells.
    // Source cells run from top-left to bottom-right within each furnishing.
    private static Furnishing[] Layout() => new[]
    {
        new Furnishing("01_BackStorage_Cabinet", 1, 7, 3, 1, true,
            C(0,17),C(1,17),C(2,17)),
        new Furnishing("02_BackDisplay_Shelf", 4, 8, 3, 1, true,
            C(19,17),C(20,17),C(21,17)),
        new Furnishing("03_SalesCounter", 6, 6, 4, 1, true,
            C(0,12),C(1,12),C(1,12),C(2,12)),
        new Furnishing("04_Shopkeeper_Stool", 8, 8, 1, 1, true, C(17,3)),
        new Furnishing("05_BackCorner_Plant", 10, 8, 1, 1, true, C(16,0)),
        new Furnishing("06_LeftDisplay_Cabinet", 1, 4, 3, 1, true,
            C(4,14),C(5,14),C(6,14)),
        new Furnishing("07_RightBench", 8, 3, 3, 1, true, C(4,8),C(5,8),C(6,8)),
        new Furnishing("08_Entrance_Rug", 4, 1, 3, 1, false, C(16,14),C(17,14),C(18,14)),
        new Furnishing("09_LeftRoundTable", 1, 1, 2, 1, true, C(3,1),C(4,1)),
        new Furnishing("10_RightSmallTable", 9, 0, 1, 1, true, C(7,0)),
        new Furnishing("11_RightTable_Chair", 8, 0, 1, 1, true, C(2,2)),
        new Furnishing("12_LeftCorner_Plant", 0, 8, 1, 1, true, C(17,0)),
        new Furnishing("13_RightSide_Plant", 10, 4, 1, 1, true, C(17,0))
    };

    [MenuItem("Tools/PJH/Refresh Fishing Shop Furniture Layout")]
    public static void RefreshLayout()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var root = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).SingleOrDefault(t => t.name == RootName);
        if (root == null) { Place(); return; }
        var floor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)).Single(t => t.name == "FishingShopTIle");
        var occupied = new List<Vector3Int>();
        foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin) if (floor.HasTile(cell)) occupied.Add(cell);
        int minX = occupied.Min(p => p.x), minY = occupied.Min(p => p.y);
        var layout = Layout();
        if (layout.Any(f => root.Find(f.name) == null)) throw new InvalidOperationException("Expected furniture is missing; layout was not changed.");
        var sprites = ImportSprites(layout);
        var floorRenderer = floor.GetComponent<TilemapRenderer>();
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Refine fishing shop furniture");
        foreach (var f in layout)
        {
            Transform obj = root.Find(f.name);
            Undo.RecordObject(obj, "Align furniture");
            obj.position = floor.CellToWorld(new Vector3Int(minX + f.x, minY + f.y, 0));
            foreach (Transform part in obj.Cast<Transform>().ToArray())
                if (part.name.StartsWith("Part_")) Undo.DestroyObjectImmediate(part.gameObject);
            var sorting = obj.GetComponent<SortingGroup>();
            Undo.RecordObject(sorting, "Sort furniture");
            sorting.sortingOrder = floorRenderer.sortingOrder + (f.solid ? 20 - f.y : 1);
            for (int row = 0; row < f.height; row++) for (int col = 0; col < f.width; col++)
            {
                var part = new GameObject($"Part_{col}_{row}");
                Undo.RegisterCreatedObjectUndo(part, "Furniture sprite part");
                part.transform.SetParent(obj, false);
                part.transform.localPosition = new Vector3(col, f.height - row - 1, 0);
                var renderer = part.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[Key(f.cells[row * f.width + col])];
                renderer.sharedMaterial = floorRenderer.sharedMaterial;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            }
            if (f.solid)
            {
                var collider = obj.GetComponent<BoxCollider2D>();
                Undo.RecordObject(collider, "Furniture footprint");
                float height = Mathf.Min(f.height, f.height == 1 ? .625f : 1.25f);
                collider.size = new Vector2(f.width - .125f, height);
                collider.offset = new Vector2(f.width * .5f, height * .5f + .0625f);
            }
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log($"[FishingShop] Refined {layout.Length} furniture objects; {root.GetComponentsInChildren<SpriteRenderer>().Length} sprite parts.");
    }

    [MenuItem("Tools/PJH/Place Fishing Shop Furniture")]
    public static void Place()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before placing furniture.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException("Open the PJH scene before placing furniture.");
        if (scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.name == RootName))
        { Debug.Log("[FishingShop] Furniture already exists. Move its child objects individually; no duplicate was created."); return; }
        Tilemap floor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)).Single(t => t.name == "FishingShopTIle");
        var occupied = new List<Vector3Int>();
        foreach (Vector3Int cell in floor.cellBounds.allPositionsWithin)
            if (floor.HasTile(cell)) occupied.Add(cell);
        int minX = occupied.Min(p => p.x), minY = occupied.Min(p => p.y);
        Furnishing[] layout = Layout();
        foreach (var f in layout)
        {
            if (f.cells.Length != f.width * f.height) throw new InvalidOperationException("Incomplete furniture: " + f.name);
            for (int y = 0; y < f.height; y++) for (int x = 0; x < f.width; x++)
                if (!floor.HasTile(new Vector3Int(minX + f.x + x, minY + f.y + y, 0)))
                    throw new InvalidOperationException("Furniture would extend outside the floor: " + f.name);
        }
        Directory.CreateDirectory(BackupFolder);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save scene before backing it up.");
        string backup = Path.Combine(BackupFolder, "PJH_before_furniture_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        File.Copy(ScenePath, backup, false);
        Dictionary<string, Sprite> sprites = ImportSprites(layout);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Place fishing shop furniture");
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Place fishing shop furniture");
        root.transform.SetParent(floor.transform.parent, false);
        TilemapRenderer floorRenderer = floor.GetComponent<TilemapRenderer>();
        try
        {
            foreach (Furnishing f in layout)
            {
                var obj = new GameObject(f.name);
                obj.transform.SetParent(root.transform, false);
                Vector3 origin = floor.CellToWorld(new Vector3Int(minX + f.x, minY + f.y, 0));
                Vector3 stepX = floor.CellToWorld(new Vector3Int(minX + f.x + 1, minY + f.y, 0)) - origin;
                Vector3 stepY = floor.CellToWorld(new Vector3Int(minX + f.x, minY + f.y + 1, 0)) - origin;
                obj.transform.position = origin;
                obj.transform.rotation = floor.transform.rotation;
                obj.transform.localScale = new Vector3(stepX.magnitude / root.transform.lossyScale.x, stepY.magnitude / root.transform.lossyScale.y, 1);
                var group = obj.AddComponent<SortingGroup>();
                group.sortingLayerID = floorRenderer.sortingLayerID;
                group.sortingOrder = floorRenderer.sortingOrder + (f.solid ? 20 - f.y : 1);
                for (int row = 0; row < f.height; row++) for (int col = 0; col < f.width; col++)
                {
                    Vector2Int cell = f.cells[row * f.width + col];
                    var part = new GameObject($"Part_{col}_{row}");
                    part.transform.SetParent(obj.transform, false);
                    part.transform.localPosition = new Vector3(col, f.height - row - 1, 0);
                    var renderer = part.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprites[Key(cell)];
                    renderer.sharedMaterial = floorRenderer.sharedMaterial;
                    renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                }
                if (f.solid)
                {
                    var collider = obj.AddComponent<BoxCollider2D>();
                    // Keep the collider at the furniture's footprint, below its upper visual area.
                    float footprintHeight = Mathf.Min(f.height, f.height == 1 ? .625f : 1.25f);
                    collider.size = new Vector2(f.width - .125f, footprintHeight);
                    collider.offset = new Vector2(f.width * .5f, footprintHeight * .5f + .0625f);
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save furniture scene.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = root;
            if (SceneView.lastActiveSceneView != null)
            {
                Bounds bounds = new Bounds(floor.CellToWorld(new Vector3Int(minX, minY, 0)), Vector3.zero);
                bounds.Encapsulate(floor.CellToWorld(new Vector3Int(occupied.Max(p => p.x) + 1, occupied.Max(p => p.y) + 1, 0)));
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Frame(bounds, true);
            }
            Debug.Log($"[FishingShop] Placed {layout.Length} grid-aligned furniture objects; {root.GetComponentsInChildren<SpriteRenderer>().Length} sprite parts. Backup: {backup}");
        }
        catch
        {
            Undo.RevertAllDownToGroup(undoGroup);
            throw;
        }
    }

    private static Dictionary<string, Sprite> ImportSprites(Furnishing[] layout)
    {
        if (!File.Exists(Sheet)) File.Copy(Source, Sheet, false);
        AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
#pragma warning disable 618
        importer.spritesheet = layout.SelectMany(f => f.cells).Distinct().Select(c => new SpriteMetaData
        {
            name = Key(c), rect = new Rect(c.x * 17, 305 - c.y * 17 - 16, 16, 16),
            alignment = (int)SpriteAlignment.BottomLeft, pivot = Vector2.zero
        }).ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();
        var result = AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>().ToDictionary(s => s.name);
        foreach (Vector2Int c in layout.SelectMany(f => f.cells).Distinct())
            if (!result.ContainsKey(Key(c))) throw new InvalidOperationException("Missing imported sprite " + Key(c));
        return result;
    }
}
