#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// 퍼레이드 바깥 가장자리에서 5m 더 바깥 선. 그 안쪽은 건물 금지 구역. 에디터에서만 보인다.
public class ParadeKeepOutGizmo : MonoBehaviour
{
    public Vector3[] Points;

    void OnDrawGizmos()
    {
        if (Points == null || Points.Length < 2)
            return;
        Handles.color = Color.yellow;
        for (int i = 0; i < Points.Length; i++)
            Handles.DrawDottedLine(Points[i], Points[(i + 1) % Points.Length], 6f);
    }
}
#endif
