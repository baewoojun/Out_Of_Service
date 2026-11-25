#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

// 매직아일랜드 다리(MIBR = gate8) + 음식점·상점·편의시설(MIFA = facilities/fac.py) 구운 FBX 를 씬에 놓기.
// 순서: 1) Tools/LotteWorld/Magic Island/Setup Ride Materials (MIBR·MIFA 폴더도 같이 처리됨)
//       2) Tools/LotteWorld/Magic Island/Place Bridge + Facilities (11-25)
// FBX 는 매직아일랜드 모듈 좌표(블렌더 X = 동, Y = 북, Z = 섬 바닥 위) 그대로 → MagicIsland 루트 아래 로컬 (0, -6, 0) 에 놓음
// (섬 바닥 = 로컬 y -6, 위 다리 = 0, 아래 다리 = -5.6). 다시 누르면 지우고 새로 놓음. 씬 저장은 직접.
public static class MagicIslandBridgeFacilitiesSetup
{
    const string Root = "Assets/Maps/LotteWorld/Rides";
    static readonly string[] Packs = { "MIBR", "MIFA" };

    [MenuItem("Tools/LotteWorld/Magic Island/Place Bridge + Facilities (11-25)")]
    public static void Place()
    {
        var mi = GameObject.Find("MagicIsland");
        if (mi == null) { Debug.LogError("[MI 11-25] 씬에 MagicIsland 루트가 없음 — 매직아일랜드 씬을 열고 다시 누르세요"); return; }
        var parentT = mi.transform.Find("MI_Layout_v7") ?? mi.transform;
        int doors = 0, cols = 0;
        foreach (var p in Packs)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{p}/{p}.fbx");
            if (fbx == null) { Debug.LogWarning($"[MI 11-25] {Root}/{p}/{p}.fbx 없음"); continue; }
            var old = parentT.Find(p); if (old) Undo.DestroyObjectImmediate(old.gameObject);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx, parentT);
            go.name = p;
            // FBX 루트 회전(임포트가 넣은 X 89.98 등)은 그대로 둠 — v7 놀이기구 '제자리'와 같은 방식
            go.transform.localPosition = go.transform.localPosition + new Vector3(0, -6f, 0);
            // 자식 좌표가 블렌더 그대로(x, 북, -위)로 들어온 경우 → v7 놀이기구(MI19 등 루트 X 89.98)처럼 루트를 X 90 돌림
            var ex = go.transform.localEulerAngles.x;
            if (Mathf.Abs(Mathf.DeltaAngle(ex, 0f)) < 1f) go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f) * go.transform.localRotation;
            Undo.RegisterCreatedObjectUndo(go, "Place " + p);
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.gameObject.name;
                if (mf.sharedMesh == null || mf.GetComponent<Collider>()) continue;
                if (n.Contains("Decal")) continue;   // 데칼은 충돌 없음
                var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.convex = false; cols++;   // 들어가는 방 = Convex 끔
            }
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Door_")) { doors++; t.gameObject.isStatic = false; }   // 문: 원점 = 경첩, 로컬 Y 회전
                else t.gameObject.isStatic = true;
            // 움직이는 부품은 static 끔
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Elevator_Car") || t.name.EndsWith("_Steps")) t.gameObject.isStatic = false;
        }
        Debug.Log($"[MI 11-25] 다리·편의시설 놓음: 문 {doors}개(원점 = 경첩), MeshCollider {cols}개. layout v7 의 '2 블록' 자리표시 상자(음식점·상점·WC)와 옛 v1 다리는 꺼 주세요.");
    }
}
#endif
