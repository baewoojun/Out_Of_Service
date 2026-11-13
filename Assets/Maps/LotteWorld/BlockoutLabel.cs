#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 씬 뷰 기즈모가 켜져 있을 때만 구조물 이름을 그린다. 플레이어 빌드에는 포함되지 않는다.
public class BlockoutLabel : MonoBehaviour
{
    const float FullNameDistance = 40f;
    const float HideCodeDistance = 250f;
    const int CodeFontSize = 12;
    const int FullFontSize = 14;

    public string Caption;
    public Color LabelColor = Color.white;

    GUIStyle style;

    public void Set(string caption)
    {
        Caption = caption;
        LabelColor = ColorFor(caption);
    }

    static Color ColorFor(string caption)
    {
        string code = DisplayCode(caption);
        if (string.IsNullOrEmpty(code))
            return Color.white;
        if (code[0] == 'A' || code == "GardenStage")
            return new Color(0.95f, 0.15f, 0.12f);
        if (code[0] == 'F')
            return new Color(1f, 0.45f, 0.72f);
        if (code[0] == 'S')
            return new Color(0.62f, 0.32f, 0.95f);
        if (code[0] == 'X')
            return new Color(0.72f, 0.72f, 0.74f);
        return new Color(0.93f, 0.93f, 0.93f);
    }

    // "A13 | 회전목마" -> "A13", "A15_2 | 키즈토리아" -> "A15_2", "X_언더랜드" -> "X"
    static string ShortCode(string caption) => DisplayCode(caption);

    static string DisplayCode(string caption)
    {
        if (string.IsNullOrEmpty(caption))
            return "";
        int bar = caption.IndexOf(" | ", System.StringComparison.Ordinal);
        if (bar > 0)
            return caption.Substring(0, bar);
        int end = 1;
        while (end < caption.Length && char.IsDigit(caption[end]))
            end++;
        if (end == caption.Length || caption[end] == '_')
            return caption.Substring(0, end);
        return caption;
    }

    static bool IsBlocked(string caption)
    {
        return caption.Length > 0 && caption[0] == 'X';
    }

    bool IsSelfOrChildSelected()
    {
        Transform[] selected = Selection.transforms;
        for (int i = 0; i < selected.Length; i++)
        {
            Transform picked = selected[i];
            if (picked != null && picked.IsChildOf(transform))
                return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        string text = string.IsNullOrEmpty(Caption) ? name : Caption;
        if (string.IsNullOrEmpty(text))
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        bool showFull = IsSelfOrChildSelected();
        SceneView view = SceneView.lastActiveSceneView;
        Camera cam = view != null ? view.camera : null;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            float distance = Vector3.Distance(camPos, bounds.ClosestPoint(camPos));
            if (distance <= FullNameDistance)
                showFull = true;
            else if (!showFull && distance > HideCodeDistance)
                return;
        }

        // 막힌 곳(X)은 번호 "X"를 그리지 않는다. 선택됐거나 40m 안일 때만 전체 이름.
        if (!showFull && IsBlocked(text))
            return;

        if (style == null)
        {
            style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
        style.fontSize = showFull ? FullFontSize : CodeFontSize;
        style.normal.textColor = LabelColor;
        string drawn = showFull ? text : ShortCode(text);
        Handles.Label(new Vector3(bounds.center.x, bounds.max.y + 3f, bounds.center.z), drawn, style);
    }
}
#endif
