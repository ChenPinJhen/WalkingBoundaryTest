# WalkingBoundaryEditor.cs

這支腳本放在 `Assets/Editor`。Unity 選到 `WalkingBoundary` 時仍先畫出原有欄位。當 `Route` 選擇 Hybrid，Inspector 會顯示目前階段和切換按鈕。

按鈕呼叫 `SetHybridDiagonal`。編輯時設定進入 Play 的起始階段，Play 時立即重設成所選階段並更新網格和腳步。Inspector 同時記錄 Undo 與 Prefab 覆寫。這支 Editor 腳本不會打包進 Quest。
