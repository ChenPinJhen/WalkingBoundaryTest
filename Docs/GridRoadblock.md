# GridRoadblock.cs

`GridRoadblock` Prefab 可以放在任意路線。腳本優先使用 Inspector 指定的 `Headset Override`，否則從 Meta Camera Rig 取得頭顯位置。使用路障本地座標的 X 和 Z 判斷頭顯是否進入寬度及觸發深度內。

觸發後，腳本把 GLB 裡第一個可用的直立網格縮放到設定的寬高，並以底部為轉軸。計時不受觀眾離開影響。`Fall Delay` 到期後開始倒下，`Fall Duration` 控制動畫時間。

用 `Previous Roadblock` 設定先後。第二個可用 `Release Previous On Trigger` 通知第一個倒下，第一個可用 `Wait For Release` 等第二個通知。各路障每次啟用只觸發一次。Gizmos 顯示編輯預覽與觸發區。
