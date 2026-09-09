using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
 
/// <summary>
/// 애니메이션 이벤트를 뒤져주는 에디터 도구.
///
/// "AnimationEvent has no function name specified!" 는 클립에 <b>Function 칸이 빈 이벤트</b>가
/// 박혀 있을 때 뜬다. Animation 창에서 눈으로 찾으려면 0프레임에서 재생헤드에 가려지거나
/// 마커가 겹쳐서 안 보이는 경우가 많아 이걸로 찾는 게 확실하다.
///
/// ★ 이 파일은 반드시 <b>Editor 라는 이름의 폴더</b> 안에 있어야 한다.
///   (예: Assets/KSM/00.Scripts/Editor/AnimationEventTool.cs)
///   안 그러면 빌드할 때 UnityEditor 참조 때문에 컴파일이 깨진다.
///
/// 메뉴: 상단 Tools → 애니메이션 이벤트
/// </summary>
public static class AnimationEventTool
{
    [MenuItem("Tools/애니메이션 이벤트/빈 이벤트 찾기")]
    private static void FindEmpty()
    {
        int found = 0;
 
        foreach (AnimationClip clip in LoadAllClips())
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
 
            for (int i = 0; i < events.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(events[i].functionName)) continue;
 
                found++;
 
                Debug.Log(
                    $"<color=#FF9999><b>빈 이벤트</b></color>  클립 <b>{clip.name}</b>  " +
                    $"{events[i].time:0.###}초 (index {i})\n{AssetDatabase.GetAssetPath(clip)}",
                    clip);
            }
        }
 
        Debug.Log(found == 0
            ? "빈 애니메이션 이벤트가 없습니다."
            : $"빈 애니메이션 이벤트 {found}개를 찾았습니다. 위 로그를 클릭하면 해당 클립이 선택됩니다.");
    }
 
    [MenuItem("Tools/애니메이션 이벤트/빈 이벤트 전부 삭제")]
    private static void DeleteEmpty()
    {
        if (!EditorUtility.DisplayDialog(
                "빈 애니메이션 이벤트 삭제",
                "Function 칸이 비어 있는 이벤트를 전부 지웁니다.\n" +
                "이름이 채워진 이벤트는 건드리지 않습니다.\n\n계속할까요?",
                "삭제", "취소"))
            return;
 
        int removed = 0;
        int clipCount = 0;
 
        foreach (AnimationClip clip in LoadAllClips())
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
 
            var kept = new List<AnimationEvent>(events.Length);
 
            foreach (AnimationEvent e in events)
            {
                if (string.IsNullOrWhiteSpace(e.functionName)) removed++;
                else kept.Add(e);
            }
 
            if (kept.Count == events.Length) continue;   // 바뀐 게 없으면 건너뛴다
 
            Undo.RecordObject(clip, "Delete Empty Animation Events");
            AnimationUtility.SetAnimationEvents(clip, kept.ToArray());
            EditorUtility.SetDirty(clip);
 
            clipCount++;
            Debug.Log($"{clip.name} 에서 빈 이벤트를 지웠습니다.", clip);
        }
 
        if (clipCount > 0) AssetDatabase.SaveAssets();
 
        Debug.Log(removed == 0
            ? "지울 빈 이벤트가 없었습니다."
            : $"클립 {clipCount}개에서 빈 이벤트 {removed}개를 지웠습니다.");
    }
 
    [MenuItem("Tools/애니메이션 이벤트/모든 이벤트 목록 보기")]
    private static void ListAll()
    {
        var sb = new System.Text.StringBuilder("───── 애니메이션 이벤트 목록 ─────\n");
        int total = 0;
 
        foreach (AnimationClip clip in LoadAllClips())
        {
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
            if (events.Length == 0) continue;
 
            sb.Append($"\n<b>{clip.name}</b>\n");
 
            foreach (AnimationEvent e in events)
            {
                string name = string.IsNullOrWhiteSpace(e.functionName)
                    ? "<color=#FF9999>(비어 있음)</color>"
                    : e.functionName;
 
                sb.Append($"   {e.time:0.###}초  →  {name}\n");
                total++;
            }
        }
 
        sb.Append($"\n총 {total}개");
        Debug.Log(sb.ToString());
    }
 
    // ════════════════════════════════════════════════════════════
 
    /// <summary>프로젝트의 모든 애니메이션 클립. FBX 안에 들어있는 수정 불가 클립은 뺀다</summary>
    private static IEnumerable<AnimationClip> LoadAllClips()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
 
            if (clip == null) continue;
            if ((clip.hideFlags & HideFlags.NotEditable) != 0) continue;   // 임포트된 읽기 전용
 
            yield return clip;
        }
    }
}