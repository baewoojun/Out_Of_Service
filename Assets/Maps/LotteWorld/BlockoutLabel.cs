#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 씬 뷰 기즈모가 켜져 있을 때만 구조물 이름을 그린다. 플레이어 빌드에는 포함되지 않는다.
public class BlockoutLabel : MonoBehaviour
{
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
        if (string.IsNullOrEmpty(caption))
            return Color.white;
        if (caption[0] == 'A' || caption == "GardenStage")
            return new Color(0.95f, 0.15f, 0.12f);
        if (caption[0] == 'F')
            return new Color(1f, 0.45f, 0.72f);
        if (caption[0] == 'S')
            return new Color(0.62f, 0.32f, 0.95f);
        if (caption[0] == 'X')
            return new Color(0.72f, 0.72f, 0.74f);
        return new Color(0.93f, 0.93f, 0.93f);
    }

    void OnDrawGizmos()
    {
        string text = string.IsNullOrEmpty(Caption) ? name : Caption;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                bounds.Encapsulate(renderers[i].bounds);
        }

        if (style == null)
        {
            style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
        style.normal.textColor = LabelColor;
        Handles.Label(new Vector3(bounds.center.x, bounds.max.y + 3f, bounds.center.z), text, style);
    }
}
#endif
