using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class BeachPrototypeBuilder
{
    private const int BuilderVersion = 3;
    private const string BuilderVersionKey =
        "PJH.BeachPrototypeBuilder.SeaTileVersion";

    private const string ScenePath =
        "Assets/PJH/Scene/BeachPrototype_SeaTile.unity";

    private const string SpriteSheetPath =
        "Assets/PJH/03.Sprite/SeaTileSprite/" +
        "Sparkeltons Beachy Tileset 1.1 (1).png";

    private const string GeneratedTileFolder =
        "Assets/PJH/TileMap/SeaTileGenerated";

    static BeachPrototypeBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("PJH/Build Beach Prototype (SeaTileSprite)")]
    public static void RebuildFromMenu()
    {
        Build(true);
    }

    private static void BuildIfNeeded()
    {
        bool sceneExists =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;

        if (sceneExists &&
            EditorPrefs.GetInt(BuilderVersionKey, 0) >= BuilderVersion)
            return;

        Build(true);
    }

    private static void Build(bool overwrite)
    {
        if (!overwrite &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        Sprite[] sprites = AssetDatabase
            .LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .ToArray();

        if (sprites.Length < 79)
        {
            Debug.LogError(
                "해변 프로토타입 생성 실패: SeaTileSprite가 " +
                "16x16 Multiple Sprite로 임포트되었는지 확인하세요.");
            return;
        }

        EnsureFolder(GeneratedTileFolder);

        // 베이스 타일은 한 장을 도배하는 구조가 아니라 2x2 네 장이 한 세트다.
        TileBase waterTopLeft = GetOrCreateTile(sprites, 4);
        TileBase waterTopRight = GetOrCreateTile(sprites, 5);
        TileBase waterBottomLeft = GetOrCreateTile(sprites, 10);
        TileBase waterBottomRight = GetOrCreateTile(sprites, 11);

        TileBase verticalShoreA = GetOrCreateTile(sprites, 17);

        TileBase wetSandTopLeft = GetOrCreateTile(sprites, 20);
        TileBase wetSandTopRight = GetOrCreateTile(sprites, 21);
        TileBase wetSandBottomLeft = GetOrCreateTile(sprites, 28);
        TileBase wetSandBottomRight = GetOrCreateTile(sprites, 29);

        TileBase drySandTopLeft = GetOrCreateTile(sprites, 36);
        TileBase drySandTopRight = GetOrCreateTile(sprites, 37);
        TileBase drySandBottomLeft = GetOrCreateTile(sprites, 44);
        TileBase drySandBottomRight = GetOrCreateTile(sprites, 45);

        TileBase[] animatedWaveTiles =
        {
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_01_166-169.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_02_170-173.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_03_174-177.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_04_178-181.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_05_182-185.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_06_186-189.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_07_190-193.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_08_194-197.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_09_198-201.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_10_202-205.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_11_206-209.asset"),
            LoadTile("Assets/PJH/TileMap/WaterWaveAnimations/OceanWave_12_210-213.asset")
        };

        TileBase[] pebbleTiles =
        {
            GetOrCreateTile(sprites, 52),
            GetOrCreateTile(sprites, 53),
            GetOrCreateTile(sprites, 54),
            GetOrCreateTile(sprites, 55),
            GetOrCreateTile(sprites, 56),
            GetOrCreateTile(sprites, 57)
        };

        TileBase[] beachPlantTiles =
        {
            GetOrCreateTile(sprites, 58),
            GetOrCreateTile(sprites, 59),
            GetOrCreateTile(sprites, 60),
            GetOrCreateTile(sprites, 61),
            GetOrCreateTile(sprites, 62),
            GetOrCreateTile(sprites, 63)
        };

        TileBase dockTopLeft = GetOrCreateTile(sprites, 64);
        TileBase dockTopMiddle = GetOrCreateTile(sprites, 65);
        TileBase dockTopMiddleAlt = GetOrCreateTile(sprites, 66);
        TileBase dockTopRight = GetOrCreateTile(sprites, 67);
        TileBase dockBottomLeft = GetOrCreateTile(sprites, 75);
        TileBase dockBottomMiddle = GetOrCreateTile(sprites, 76);
        TileBase dockBottomMiddleAlt = GetOrCreateTile(sprites, 77);
        TileBase dockBottomRight = GetOrCreateTile(sprites, 78);

        Scene previousScene = SceneManager.GetActiveScene();
        bool canOpenResult = previousScene.IsValid() && !previousScene.isDirty;
        bool replacingOpenResult =
            previousScene.IsValid() && previousScene.path == ScenePath;

        // 결과 씬 자체가 열려 있으면 Additive로 같은 경로에 저장할 수 없다.
        // 이 경우 깨끗한 기존 결과 씬을 먼저 내리는 Single 모드로 새로 만든다.
        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            replacingOpenResult
                ? NewSceneMode.Single
                : NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        GameObject root = new GameObject("BeachPrototype_SeaTile");
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        gridObject.transform.SetParent(root.transform, false);

        Tilemap groundMap = CreateTilemap(
            gridObject.transform, "01_BeachGround", 0);
        Tilemap waterMap = CreateTilemap(
            gridObject.transform, "02_Sea", 0);
        Tilemap shoreMap = CreateTilemap(
            gridObject.transform, "03_Shoreline", 1);
        Tilemap detailMap = CreateTilemap(
            gridObject.transform, "04_BeachDetails", 2);
        Tilemap dockMap = CreateTilemap(
            gridObject.transform, "05_FishingPier", 3);

        const int minX = -32;
        const int maxX = 36;
        const int minY = -18;
        const int maxY = 18;
        const int shoreX = 5;

        System.Random random = new System.Random(20092026);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                if (x < shoreX - 2)
                {
                    groundMap.SetTile(
                        position,
                        PickTwoByTwo(
                            drySandTopLeft,
                            drySandTopRight,
                            drySandBottomLeft,
                            drySandBottomRight,
                            x,
                            y));
                }
                else if (x < shoreX)
                {
                    groundMap.SetTile(
                        position,
                        PickTwoByTwo(
                            wetSandTopLeft,
                            wetSandTopRight,
                            wetSandBottomLeft,
                            wetSandBottomRight,
                            x,
                            y));
                }
                else if (x == shoreX)
                {
                    shoreMap.SetTile(position, verticalShoreA);
                }
                else
                {
                    waterMap.SetTile(
                        position,
                        PickTwoByTwo(
                            waterTopLeft,
                            waterTopRight,
                            waterBottomLeft,
                            waterBottomRight,
                            x,
                            y));

                    // 애니메이션 물결은 바닥이 아니라 투명 오버레이로 드물게 둔다.
                    if (x >= shoreX + 3 &&
                        x % 5 == 0 &&
                        y % 4 == 0)
                    {
                        int waveIndex = Mathf.Abs(x / 5 + y / 4) %
                                        animatedWaveTiles.Length;
                        TileBase waveTile = animatedWaveTiles[waveIndex];

                        if (waveTile != null)
                            detailMap.SetTile(position, waveTile);
                    }
                }
            }
        }

        // 이동 공간이 답답하지 않도록 장식은 가장자리 위주로 드문드문 둔다.
        for (int y = minY + 2; y <= maxY - 2; y += 4)
        {
            int pebbleX = -25 + random.Next(0, 10);
            detailMap.SetTile(
                new Vector3Int(pebbleX, y, 0),
                pebbleTiles[random.Next(pebbleTiles.Length)]);

            if ((y & 1) == 0)
            {
                int plantX = -10 + random.Next(0, 7);
                detailMap.SetTile(
                    new Vector3Int(plantX, y + 1, 0),
                    beachPlantTiles[random.Next(beachPlantTiles.Length)]);
            }
        }

        CreatePier(
            dockMap,
            1,
            -9,
            20,
            dockTopLeft,
            dockTopMiddle,
            dockTopMiddleAlt,
            dockTopRight,
            dockBottomLeft,
            dockBottomMiddle,
            dockBottomMiddleAlt,
            dockBottomRight);

        CreateCamera(root.transform);
        CreateFishingArea(root.transform, shoreX, maxX, minY, maxY);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorPrefs.SetInt(BuilderVersionKey, BuilderVersion);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!replacingOpenResult && previousScene.IsValid())
            SceneManager.SetActiveScene(previousScene);

        if (replacingOpenResult)
        {
            // 새 씬이 이미 활성화되어 있으므로 그대로 유지한다.
        }
        else if (canOpenResult)
        {
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            Selection.activeObject =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.LogWarning(
                "새 해변 씬은 생성했지만 현재 씬에 저장하지 않은 변경이 있어 " +
                "자동으로 열지 않았습니다.");
        }

        Debug.Log("SeaTileSprite 해변 프로토타입 생성 완료: " + ScenePath);
    }

    private static void CreatePier(
        Tilemap map,
        int startX,
        int bottomY,
        int length,
        TileBase topLeft,
        TileBase topMiddle,
        TileBase topMiddleAlt,
        TileBase topRight,
        TileBase bottomLeft,
        TileBase bottomMiddle,
        TileBase bottomMiddleAlt,
        TileBase bottomRight)
    {
        for (int offset = 0; offset < length; offset++)
        {
            bool isFirst = offset == 0;
            bool isLast = offset == length - 1;

            TileBase top = isFirst
                ? topLeft
                : isLast
                    ? topRight
                    : (offset % 4 == 0 ? topMiddleAlt : topMiddle);

            TileBase bottom = isFirst
                ? bottomLeft
                : isLast
                    ? bottomRight
                    : (offset % 4 == 0 ? bottomMiddleAlt : bottomMiddle);

            map.SetTile(new Vector3Int(startX + offset, bottomY + 1, 0), top);
            map.SetTile(new Vector3Int(startX + offset, bottomY, 0), bottom);
        }
    }

    private static TileBase GetOrCreateTile(Sprite[] sprites, int index)
    {
        string spriteName =
            $"Sparkeltons Beachy Tileset 1.1 (1)_{index}";
        Sprite sprite = sprites.FirstOrDefault(value => value.name == spriteName);

        if (sprite == null)
            return null;

        string tilePath = $"{GeneratedTileFolder}/SeaTile_{index:00}.asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);

        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }

        tile.sprite = sprite;
        tile.color = Color.white;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static TileBase LoadTile(string path)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>(path);
    }

    private static TileBase PickTwoByTwo(
        TileBase topLeft,
        TileBase topRight,
        TileBase bottomLeft,
        TileBase bottomRight,
        int x,
        int y)
    {
        bool useRight = (x & 1) != 0;
        bool useTop = (y & 1) != 0;

        if (useTop)
            return useRight ? topRight : topLeft;

        return useRight ? bottomRight : bottomLeft;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static Tilemap CreateTilemap(
        Transform parent,
        string name,
        int sortingOrder)
    {
        GameObject gameObject = new GameObject(
            name,
            typeof(Tilemap),
            typeof(TilemapRenderer));
        gameObject.transform.SetParent(parent, false);

        TilemapRenderer renderer = gameObject.GetComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        return gameObject.GetComponent<Tilemap>();
    }

    private static void CreateCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(-1f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 19f;
        camera.backgroundColor = new Color32(39, 70, 100, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateFishingArea(
        Transform parent,
        int shoreX,
        int maxX,
        int minY,
        int maxY)
    {
        GameObject area = new GameObject("SeaFishingArea", typeof(BoxCollider2D));
        area.transform.SetParent(parent, false);

        float left = shoreX + 1f;
        float width = maxX - left + 1f;
        area.transform.position = new Vector3(
            left + width * 0.5f - 0.5f,
            (minY + maxY) * 0.5f,
            0f);

        BoxCollider2D collider = area.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(width, maxY - minY + 1f);

        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0)
            area.layer = waterLayer;
    }
}
