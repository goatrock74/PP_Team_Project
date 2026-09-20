using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class BeachPrototypeBuilder
{
    private const string ScenePath = "Assets/PJH/Scene/BeachPrototype.unity";

    static BeachPrototypeBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    private static void BuildIfNeeded()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            return;

        Scene previousScene = SceneManager.GetActiveScene();
        bool canOpenResult = previousScene.IsValid() && !previousScene.isDirty;

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        GameObject root = new GameObject("BeachPrototype");
        GameObject gridObject = new GameObject("Grid", typeof(Grid));
        gridObject.transform.SetParent(root.transform);

        TileBase grass = LoadTile("Assets/_Sprite/Tiles/Summer/All tiles Spring_331.asset");
        TileBase grassVariation = LoadTile("Assets/_Sprite/Tiles/Summer/All tiles Spring_326.asset");
        TileBase sand = LoadTile("Assets/_Sprite/Tiles/Summer/All tiles Spring_161.asset");
        TileBase sandVariation = LoadTile("Assets/_Sprite/Tiles/Summer/All tiles Spring_162.asset");
        TileBase wood = LoadTile("Assets/_Sprite/Tiles/Summer/All tiles Spring_2.asset");
        TileBase shallowWater = LoadTile("Assets/PJH/TileMap/Water_Tileset_12.asset");
        TileBase deepWater = LoadTile("Assets/PJH/TileMap/Water_Tileset_41.asset");
        TileBase waterHighlightA = LoadTile("Assets/PJH/TileMap/Water_Tileset_166.asset");
        TileBase waterHighlightB = LoadTile("Assets/PJH/TileMap/Water_Tileset_182.asset");
        TileBase foam = LoadTile("Assets/PJH/TileMap/Water_Tileset_247.asset");

        if (grass == null || sand == null || shallowWater == null || deepWater == null)
        {
            Debug.LogError("BeachPrototype 생성 실패: 필요한 타일 에셋을 찾지 못했습니다.");
            EditorSceneManager.CloseScene(scene, true);
            if (previousScene.IsValid()) SceneManager.SetActiveScene(previousScene);
            return;
        }

        Tilemap grassMap = CreateTilemap(gridObject.transform, "01_Grass", 0);
        Tilemap sandMap = CreateTilemap(gridObject.transform, "02_Sand", 1);
        Tilemap waterMap = CreateTilemap(gridObject.transform, "03_Water", 0);
        Tilemap foamMap = CreateTilemap(gridObject.transform, "04_ShoreFoam", 2);
        Tilemap pierMap = CreateTilemap(gridObject.transform, "05_PierAndShop", 3);
        Tilemap detailMap = CreateTilemap(gridObject.transform, "06_Details", 4);

        System.Random random = new System.Random(24092026);

        const int minX = -40;
        const int maxX = 40;
        const int minY = -22;
        const int maxY = 22;

        for (int y = minY; y <= maxY; y++)
        {
            int coastX = CoastX(y);

            for (int x = minX; x <= maxX; x++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);

                if (x <= -29)
                {
                    grassMap.SetTile(position,
                        grassVariation != null && random.NextDouble() < 0.08
                            ? grassVariation
                            : grass);
                }
                else if (x < coastX)
                {
                    sandMap.SetTile(position,
                        sandVariation != null && random.NextDouble() < 0.045
                            ? sandVariation
                            : sand);
                }
                else if (x <= coastX + 4)
                {
                    waterMap.SetTile(position, shallowWater);
                }
                else
                {
                    waterMap.SetTile(position, deepWater);
                }
            }

            if (foam != null)
            {
                Vector3Int foamPosition = new Vector3Int(coastX, y, 0);
                foamMap.SetTile(foamPosition, foam);
                foamMap.SetTransformMatrix(
                    foamPosition,
                    Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 90f)));
            }
        }

        // 마을에서 해변으로 이어지는 넓은 입구
        for (int y = -3; y <= 3; y++)
        for (int x = -40; x <= -25; x++)
            sandMap.SetTile(new Vector3Int(x, y, 0), sand);

        // 아래쪽 해안에서 바다로 길게 뻗는 2칸 폭 부두
        int pierY = -10;
        int pierStart = CoastX(pierY) - 2;
        if (wood != null)
        {
            for (int y = pierY; y <= pierY + 1; y++)
            for (int x = pierStart; x <= pierStart + 17; x++)
                pierMap.SetTile(new Vector3Int(x, y, 0), wood);

            // 위쪽 모래사장의 작은 낚시 상점 블록아웃
            for (int y = 10; y <= 14; y++)
            for (int x = -20; x <= -13; x++)
                pierMap.SetTile(new Vector3Int(x, y, 0), wood);
        }

        // 바다는 비워 보이지 않도록 물결을 드문드문 배치한다.
        for (int y = minY + 2; y <= maxY - 2; y += 4)
        {
            int startX = CoastX(y) + 7 + (y & 1);
            for (int x = startX; x <= maxX - 2; x += 8)
            {
                TileBase highlight = ((x + y) & 1) == 0
                    ? waterHighlightA
                    : waterHighlightB;
                if (highlight != null)
                    detailMap.SetTile(new Vector3Int(x, y, 0), highlight);
            }
        }

        // 해변 이동 공간을 남긴 채 가장자리에만 작은 변형 무늬를 둔다.
        if (sandVariation != null)
        {
            Vector3Int[] shellLikeMarks =
            {
                new(-23, 16, 0), new(-12, 18, 0), new(-5, 12, 0),
                new(-22, -15, 0), new(-12, -18, 0), new(-3, -16, 0),
                new(-18, 4, 0), new(-8, -3, 0)
            };
            foreach (Vector3Int p in shellLikeMarks)
                detailMap.SetTile(p, sandVariation);
        }

        CreateCamera(root.transform);
        CreateFishingArea(root.transform);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (previousScene.IsValid())
            SceneManager.SetActiveScene(previousScene);

        if (canOpenResult)
        {
            EditorSceneManager.CloseScene(scene, true);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.LogWarning(
                "BeachPrototype.unity를 생성했습니다. 현재 씬에 저장하지 않은 변경이 있어 자동으로 열지는 않았습니다.");
        }

        Debug.Log("바다맵 프로토타입 생성 완료: " + ScenePath);
    }

    private static int CoastX(int y)
    {
        if (y >= 15) return 7;
        if (y >= 7) return 5;
        if (y >= -2) return 6;
        if (y >= -8) return 4;
        if (y >= -14) return 6;
        return 5;
    }

    private static TileBase LoadTile(string path)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>(path);
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
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 24f;
        camera.backgroundColor = new Color32(32, 79, 112, 255);
        camera.clearFlags = CameraClearFlags.SolidColor;
    }

    private static void CreateFishingArea(Transform parent)
    {
        GameObject area = new GameObject("SeaFishingArea", typeof(BoxCollider2D));
        area.transform.SetParent(parent, false);
        area.transform.position = new Vector3(24f, 0f, 0f);

        BoxCollider2D collider = area.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(32f, 44f);

        int waterLayer = LayerMask.NameToLayer("Water");
        if (waterLayer >= 0)
            area.layer = waterLayer;
    }
}
