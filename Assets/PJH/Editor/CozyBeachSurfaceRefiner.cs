using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

// Samples the CURRENT painted ground, not the original map-generation formula.
// A scanline lookup contains geometry data, not an imported art texture.
public static class CozyBeachSurfaceRefiner
{
    private const string Folder = "Assets/PJH/CozyBeach";

    [MenuItem("PJH/Cozy Beach/Naturalize Existing Surfaces")]
    public static void Refine()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetSceneByPath(Folder + "/CozyBeach.unity");
        if (!scene.IsValid() || !scene.isLoaded) return;
        var maps = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>()).ToArray();
        foreach (string name in new[] { "01_SeaAndSand", "02_GrassBorder" })
        {
            var map = maps.FirstOrDefault(m => m.name == name);
            if (map == null) continue;
            if (map.transform.lossyScale != Vector3.one || map.transform.rotation != Quaternion.identity)
            {
                Debug.LogWarning("[CozyBeach] Surface refinement needs an unscaled, unrotated grid.");
                continue;
            }
            BakeBoundary(map);
        }
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
        CozyBeachMapBuilder.ExportOverview();
        Debug.Log("[CozyBeach] Existing ground edges refined. No tiles, props, colliders or scene objects moved.");
    }

    private static void BakeBoundary(Tilemap map)
    {
        const int pixels = 16;
        var bounds = map.cellBounds;
        int rows = bounds.size.y * pixels;
        if (rows == 0) return;
        var edges = new float[rows];
        var rowLeft = Enumerable.Repeat(float.PositiveInfinity, rows).ToArray();
        var images = new Dictionary<Texture2D, Texture2D>();
        var masks = new Dictionary<Sprite, bool[]>();
        try
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var sprite = map.GetSprite(new Vector3Int(x, y, 0));
                    if (sprite == null) continue;
                    if (!masks.TryGetValue(sprite, out var mask))
                    {
                        if (!images.TryGetValue(sprite.texture, out var readable))
                        {
                            readable = new Texture2D(2, 2);
                            readable.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
                            images.Add(sprite.texture, readable);
                        }
                        mask = new bool[pixels * pixels];
                        Rect rect = sprite.rect;
                        for (int py = 0; py < pixels; py++)
                        for (int px = 0; px < pixels; px++)
                        {
                            Color c = readable.GetPixel((int)rect.x + Mathf.Min((int)rect.width-1, (int)((px+0.5f)*rect.width/pixels)),
                                (int)rect.y + Mathf.Min((int)rect.height-1, (int)((py+0.5f)*rect.height/pixels)));
                            mask[py*pixels+px] = c.a > 0.5f && c.b <= c.r * 1.08f;
                        }
                        masks.Add(sprite, mask);
                    }
                    for (int py = 0; py < pixels; py++)
                    for (int px = 0; px < pixels; px++)
                        if (mask[py*pixels+px])
                        {
                            int row = (y-bounds.yMin)*pixels+py;
                            edges[row] += 1f/pixels;
                            rowLeft[row] = Mathf.Min(rowLeft[row], x + px/(float)pixels);
                        }
                }
            }
            var values = new Color[rows];
            // Tilemap bounds may retain empty columns after the user erases tiles.
            // Start at actual painted land, never at those stale bounds.
            for (int row = 0; row < rows; row++)
                if (!float.IsPositiveInfinity(rowLeft[row])) edges[row] += rowLeft[row];
            float left = map.CellToWorld(Vector3Int.zero).x;
            for (int row = 0; row < rows; row++)
            {
                float sum = 0, total = 0;
                if (edges[row] > 0)
                {
                    for (int dy = -16; dy <= 16; dy++)
                    {
                        int other = Mathf.Clamp(row+dy, 0, rows-1);
                        if (edges[other] <= 0) continue; // Preserve the open entrance.
                        float weight = 17-Mathf.Abs(dy);
                        sum += edges[other]*weight; total += weight;
                    }
                }
                values[row] = new Color(left+(total > 0 ? sum/total : 0), 0, 0, 1);
            }
            string path = Folder + "/" + map.name + "_Boundary.asset";
            var lookup = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (lookup == null)
            {
                lookup = new Texture2D(1, rows, TextureFormat.RGBAFloat, false, true);
                AssetDatabase.CreateAsset(lookup, path);
            }
            else if (lookup.height != rows) lookup.Reinitialize(1, rows, TextureFormat.RGBAFloat, false);
            lookup.name = map.name + "_Boundary";
            lookup.filterMode = FilterMode.Point; lookup.wrapMode = TextureWrapMode.Clamp;
            lookup.SetPixels(values); lookup.Apply(false, false); EditorUtility.SetDirty(lookup);
            var material = map.GetComponent<TilemapRenderer>().sharedMaterial;
            Undo.RecordObject(material, "Naturalize beach surface");
            material.SetTexture("_BoundaryTex", lookup);
            material.SetFloat("_BoundaryY", map.CellToWorld(new Vector3Int(0,bounds.yMin,0)).y);
            material.SetFloat("_BoundaryHeight", bounds.size.y);
            material.SetFloat("_UseBoundary", 1);
            EditorUtility.SetDirty(material);
        }
        finally { foreach (var image in images.Values) Object.DestroyImmediate(image); }
    }
}
