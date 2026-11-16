# WalkingBoundaryEditor.cs 逐行說明

混合路線切換按鈕。原始碼位於 [WalkingBoundaryEditor.cs](../Assets/Editor/WalkingBoundaryEditor.cs)。

行號以目前專案原始碼為準。每列對應一個有內容的原始碼行。空白行用來分段，單獨的 `{` 與 `}` 分別開啟和結束區塊，所以略過。跨行運算式按原始碼順序逐行拆開。

## 哪裡負責實際切換

這個類別只負責顯示按鈕、把點擊交給 WalkingBoundary，並告知 Unity 記錄與重畫。階段重設、模型排列和觸發判斷都由 WalkingBoundary 執行。這也讓同一個 SetHybridDiagonal 方法能被自動切換和按鈕共用。

## 綁定自訂 Inspector

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 1 | `using UnityEditor;` | 匯入自訂 Inspector、Undo、Prefab 和 SceneView 的 Editor API。 |
| 2 | `using UnityEngine;` | 匯入 GUILayout 等 Unity UI 排版型別。 |
| 4 | `[CustomEditor(typeof(WalkingBoundary))]` | 指定這個 Editor 負責繪製 WalkingBoundary 元件。 |
| 5 | `public sealed class WalkingBoundaryEditor : Editor` | 繼承 UnityEditor.Editor 並封閉進一步繼承。 |

## 繪製與切換階段

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 7 | `public override void OnInspectorGUI()` | 覆寫 Inspector 繪製入口。 |
| 9 | `DrawDefaultInspector();` | 先保留 Unity 預設 Inspector，原有的序列化欄位都能編輯。 |
| 10 | `serializedObject.Update();` | 同步 SerializedObject，讓讀取階段與路線時拿到剛修改的值。 |
| 11 | `if (serializedObject.FindProperty("route").intValue != (int)WalkingBoundary.Route.Hybrid)` | 用 route 的序列化整數對照 Hybrid 枚舉值。 |
| 12 | `return;` | 其他路線直接結束，保留預設 Inspector。 |
| 14 | `bool diagonal = serializedObject.FindProperty("hybridDiagonal").boolValue;` | 讀取隱藏的布林欄位，true 代表斜線。 |
| 15 | `EditorGUILayout.LabelField("目前階段", diagonal ? "斜直線" : "方形");` | 標籤顯示目前處於方形還是斜直線。 |
| 16 | `if (!GUILayout.Button(diagonal ? "切換到方形限縮" : "切換到斜直線限縮"))` | 按鈕顯示能切到的另一階段，Button 回傳是否被點擊。 |
| 17 | `return;` | 沒有點擊時不修改元件。 |
| 19 | `WalkingBoundary boundary = (WalkingBoundary)target;` | target 是 Unity 正在顯示的物件，轉型成 WalkingBoundary 才能呼叫切換函式。 |
| 20 | `Undo.RecordObject(boundary, "Switch hybrid route phase");` | 在修改前記錄狀態，使編輯可 Undo。 |
| 21 | `boundary.SetHybridDiagonal(!diagonal);` | 切到布林值的反面。Play Mode 下由元件重新建立該階段狀態。 |
| 22 | `PrefabUtility.RecordPrefabInstancePropertyModifications(boundary);` | 若這個元件是 Prefab 實例，記錄場景上的欄位覆寫。 |
| 23 | `EditorUtility.SetDirty(boundary);` | 通知 Unity 這個物件有變更，需要保存。 |
| 24 | `SceneView.RepaintAll();` | 請所有 Scene 視窗重畫，以更新預覽。 |
