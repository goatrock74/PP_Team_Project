using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WaterWaveAnimatedTileBuilder
{
    private const string SpriteSheetPath =
        "Assets/WaterTiles/Tiles/WaterTile/Water_Tileset.png";

    private const string OutputFolder =
        "Assets/PJH/TileMap/WaterWaveAnimations";

    private const int FirstSpriteIndex = 166;
    private const int RowCount = 6;
    private const int AnimationsPerRow = 2;
    private const int FramesPerAnimation = 4;

    static WaterWaveAnimatedTileBuilder()
    {
        EditorApplication.delayCall += BuildIfNeeded;
    }

    [MenuItem("PJH/Rebuild Water Wave Animated Tiles (166-213)")]
    public static void RebuildFromMenu()
    {
        Build();
    }

    private static void BuildIfNeeded()
    {
        string[] existing = AssetDatabase.FindAssets(
            "t:AnimatedTile",
            new[] { OutputFolder });

        if (existing.Length >= RowCount * AnimationsPerRow)
            return;

        Build();
    }

    private static void Build()
    {
        Sprite[] sprites = AssetDatabase
            .LoadAllAssetsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError(
                "Water Wave Animated Tile 생성 실패: " +
                "Water_Tileset.png의 Sprite를 불러오지 못했습니다.");
            return;
        }

        EnsureFolder(OutputFolder);

        int animationNumber = 1;

        for (int row = 0; row < RowCount; row++)
        {
            for (int group = 0; group < AnimationsPerRow; group++)
            {
                int startIndex =
                    FirstSpriteIndex +
                    row * AnimationsPerRow * FramesPerAnimation +
                    group * FramesPerAnimation;

                Sprite[] frames = new Sprite[FramesPerAnimation];

                for (int frame = 0; frame < FramesPerAnimation; frame++)
                {
                    int spriteIndex = startIndex + frame;
                    string spriteName = $"Water_Tileset_{spriteIndex}";

                    frames[frame] = sprites.FirstOrDefault(
                        sprite => sprite.name == spriteName);

                    if (frames[frame] == null)
                    {
                        Debug.LogError(
                            $"Water Wave Animated Tile 생성 실패: " +
                            $"{spriteName}을 찾지 못했습니다.");
                        return;
                    }
                }

                int endIndex = startIndex + FramesPerAnimation - 1;
                string assetPath =
                    $"{OutputFolder}/OceanWave_{animationNumber:00}_" +
                    $"{startIndex}-{endIndex}.asset";

                AnimatedTile tile =
                    AssetDatabase.LoadAssetAtPath<AnimatedTile>(assetPath);

                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<AnimatedTile>();
                    AssetDatabase.CreateAsset(tile, assetPath);
                }

                SerializedObject serializedTile = new SerializedObject(tile);
                SerializedProperty animatedSprites =
                    serializedTile.FindProperty("m_AnimatedSprites");

                animatedSprites.arraySize = FramesPerAnimation;

                for (int frame = 0; frame < FramesPerAnimation; frame++)
                {
                    animatedSprites
                        .GetArrayElementAtIndex(frame)
                        .objectReferenceValue = frames[frame];
                }

                // 기존 OceanAnime과 같은 속도다. 작은 물결이 급하게 깜빡이지 않는다.
                serializedTile.FindProperty("m_MinSpeed").floatValue = 2f;
                serializedTile.FindProperty("m_MaxSpeed").floatValue = 2f;
                serializedTile.FindProperty("m_AnimationStartTime").floatValue = 0f;
                // 여러 물결 타일을 섞어 칠했을 때 모두 동시에 움직이지 않게 한다.
                serializedTile.FindProperty("m_AnimationStartFrame").intValue =
                    (animationNumber - 1) % FramesPerAnimation;
                serializedTile.FindProperty("m_TileColliderType").intValue = 0;

                serializedTile.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(tile);
                animationNumber++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Object folder = AssetDatabase.LoadAssetAtPath<Object>(OutputFolder);
        Selection.activeObject = folder;
        EditorGUIUtility.PingObject(folder);

        Debug.Log(
            "Water_Tileset 166~213으로 4프레임 Animated Tile 12개를 " +
            "생성했습니다: " + OutputFolder);
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
}
