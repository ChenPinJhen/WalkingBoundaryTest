# GridRoadblock.cs 逐行說明

假路障、觸發順序、倒下動畫與模型預覽。原始碼位於 [GridRoadblock.cs](../Assets/GridRoadblock.cs)。

行號以目前專案原始碼為準。每列對應一個有內容的原始碼行。空白行用來分段，單獨的 `{` 與 `}` 分別開啟和結束區塊，所以略過。跨行運算式按原始碼順序逐行拆開。

## 路障串接範例

A 設為 Wait For Release。B 的 Previous Roadblock 指向 A，並開啟 Release Previous On Trigger。觀眾觸發 A 後，A 留在原地。觀眾折返並觸發 B 時，B 顯示並寫入 A 的 releasedAt，A 下一次更新就會按新時間起算倒下動畫。這套偵測直接讀頭顯的位置，不需要碰撞器事件。

## Inspector 設定

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 1 | `using UnityEngine;` | 匯入 Unity 的基本型別與 API。 |
| 3 | `[DisallowMultipleComponent]` | 限制每個 GameObject 只能掛一個這種元件。 |
| 4 | `public sealed class GridRoadblock : MonoBehaviour` | 定義獨立於外圍邊界的假路障元件。 |
| 6 | `[Header("網格與觸發範圍")]` | 建立網格與觸發距離的 Inspector 分組。 |
| 7 | `[SerializeField] private GameObject gridModel;` | 保存來源 GLB 或 Prefab。 |
| 8 | `[Tooltip("可指定頭顯追蹤點。留空時優先使用 Meta Camera Rig 的 CenterEyeAnchor。")]` | 提示頭顯覆寫欄位的用途。 |
| 9 | `[SerializeField] private Transform headsetOverride;` | 可指定另一個追蹤點，方便桌面測試。 |
| 10 | `[Min(0.1f), SerializeField] private float width = 1.1f;` | 保存網格寬度，預設 1.1 公尺。 |
| 11 | `[Min(0.1f), SerializeField] private float height = 1.8f;` | 保存網格高度，預設 1.8 公尺。 |
| 12 | `[Tooltip("距網格正面或背面的觸發距離。頭顯也要在網格寬度內。")]` | 提示正面和背面都可進入觸發區。 |
| 13 | `[Min(0.05f), SerializeField] private float triggerDistance = 0.85f;` | 保存觸發區往前及往後各延伸的距離。 |
| 15 | `[Header("觸發順序")]` | 建立前後路障控制分組。 |
| 16 | `[Tooltip("指定場景中的前一個路障。它觸發後，觀眾走進這個路障的範圍才會顯示。")]` | 提示前一個路障必須先觸發。 |
| 17 | `[SerializeField] private GridRoadblock previousRoadblock;` | 保存前一個場景路障元件的引用。 |
| 18 | `[Tooltip("這個路障觸發時，通知前一個路障開始倒下。")]` | 提示這個路障能要求前一個倒下。 |
| 19 | `[SerializeField] private bool releasePreviousOnTrigger;` | 保存是否要發出倒下通知。 |
| 21 | `[Header("倒下與消失")]` | 建立倒下時間設定分組。 |
| 22 | `[Tooltip("從首次觸發到開始倒下的秒數。觀眾離開後仍會繼續計時。")]` | 提示倒數在觀眾離開後依然繼續。 |
| 23 | `[Min(0f), SerializeField] private float fallDelay = 3f;` | 保存從觸發到開始倒下的預設秒數。 |
| 24 | `[Tooltip("倒下動畫的秒數。倒平後立即消失。")]` | 提示動畫播完立即隱藏。 |
| 25 | `[Min(0f), SerializeField] private float fallDuration = 0.65f;` | 保存完整倒下要花的時間。 |
| 26 | `[Tooltip("等待後一個路障觸發後才倒下。後一個路障須開啟 Release Previous On Trigger。")]` | 提示等待模式需要後一個路障發通知。 |
| 27 | `[SerializeField] private bool waitForRelease;` | 啟用後會關閉一般倒數，等待其他路障通知。 |

## 編輯預覽

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 29 | `#if UNITY_EDITOR` | 開始只在 Editor 編譯的模型預覽。 |
| 30 | `[SerializeField] private bool showScenePreview = true;` | 保存是否顯示橘色編輯預覽。 |
| 32 | `private void OnDrawGizmos()` | Scene 繪製 Gizmos 時呼叫。 |
| 34 | `if (Application.isPlaying \|\| !enabled \|\| !showScenePreview)` | Play 中、停用或不預覽時跳過。 |
| 35 | `return;` | 結束這次繪圖。 |
| 37 | `Mesh mesh = WalkingGridPreview.FindPanel(gridModel);` | 從來源模型取出有效的直立板面。 |
| 38 | `if (mesh == null)` | 找不到模型就跳過。 |
| 39 | `return;` | 結束繪圖。 |
| 41 | `Matrix4x4 oldMatrix = Gizmos.matrix;` | 保存其他 Gizmos 使用的矩陣。 |
| 42 | `Color oldColor = Gizmos.color;` | 保存其他 Gizmos 使用的顏色。 |
| 43 | `WalkingGridPreview.DrawPanel(mesh, transform.localToWorldMatrix, width, height, new Color(1f, 0.55f, 0.2f, 0.8f));` | 依物件世界變換畫出指定寬高的橘色模型。 |
| 44 | `Gizmos.matrix = oldMatrix;` | 還原 Gizmos 矩陣。 |
| 45 | `Gizmos.color = oldColor;` | 還原 Gizmos 顏色。 |
| 47 | `#endif` | 結束 Editor 條件編譯。 |

## 狀態與生命週期

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 49 | `private Transform headset;` | 保存目前用來判斷位置的 Transform。 |
| 50 | `private Transform pivot;` | 保存網格倒下的根轉軸。 |
| 51 | `private float triggeredAt;` | 記錄觸發發生的遊戲時間。 |
| 52 | `private float releasedAt;` | 記錄前一個路障收到釋放通知的時間。 |
| 53 | `private float fallDirection;` | 用正負 1 選擇倒向哪一面。 |
| 54 | `private int triggerFrame;` | 記錄出現時所在的影格編號。 |
| 55 | `private bool wasInsideWhileLocked;` | 記錄解鎖前觀眾是否已在觸發區，以免解鎖當下自動觸發。 |
| 56 | `private bool triggered;` | 路障已經出現就設為 true。 |
| 57 | `private bool finished;` | 倒完或初始化失敗後設為 true。 |
| 59 | `private void OnEnable()` | 每次啟用時重新初始化一次路障。 |
| 61 | `headset = headsetOverride;` | 優先採用 Inspector 覆寫的頭顯。 |
| 62 | `triggered = finished = false;` | 把已觸發和已結束都歸零。 |
| 63 | `wasInsideWhileLocked = false;` | 清除解鎖前的重疊紀錄。 |
| 64 | `releasedAt = float.PositiveInfinity;` | 用正無限大表示還沒收到倒下通知。 |
| 65 | `if (pivot != null)` | 曾生成轉軸的話重用它。 |
| 67 | `pivot.localRotation = Quaternion.identity;` | 把上一輪的倒下角度重設成直立。 |
| 68 | `pivot.gameObject.SetActive(false);` | 等待觸發前把網格藏起來。 |
| 72 | `private void OnDisable()` | 停用元件時執行隱藏。 |
| 74 | `if (pivot != null)` | 確認網格已實際建立。 |
| 75 | `pivot.gameObject.SetActive(false);` | 將轉軸連同子網格一起停用。 |

## 靠近觸發、連鎖與倒下

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 78 | `private void LateUpdate()` | 每幀更新觸發和動畫。 |
| 80 | `if (finished)` | 若本次已經完成就停止運算。 |
| 81 | `return;` | 不再執行本幀。 |
| 83 | `if (!triggered)` | 尚未觸發時才做頭顯與進入範圍判定。 |
| 85 | `if (headset == null)` | 若尚未知道要追蹤誰，嘗試尋找。 |
| 86 | `headset = FindHeadset();` | 搜尋 Rig 或主相機的 Transform。 |
| 87 | `if (headset == null)` | 搜尋失敗就等待後續影格。 |
| 88 | `return;` | 離開本幀。 |
| 90 | `Vector3 localHead = transform.InverseTransformPoint(headset.position);` | 把世界中的頭顯位置換算為路障本地座標。 |
| 91 | `bool inside = Mathf.Abs(localHead.x) <= width * 0.5f &&` | 頭顯的絕對 X 必須不超過半寬。 |
| 92 | `Mathf.Abs(localHead.z) <= triggerDistance;` | 同時要求絕對 Z 在正反面的觸發距離內，判定不使用高度。 |
| 93 | `if (previousRoadblock != null &&` | 有指定前一個路障時才檢查連鎖條件。 |
| 94 | `(!previousRoadblock.triggered \|\| previousRoadblock.triggerFrame == Time.frameCount))` | 前一個尚未觸發或正好同幀觸發時，這個仍屬鎖定。 |
| 96 | `wasInsideWhileLocked = inside;` | 保留此幀是否已經重疊。 |
| 97 | `return;` | 本幀不能出現，結束判定。 |
| 100 | `if (!inside)` | 頭顯不在區域內時處理離開。 |
| 102 | `wasInsideWhileLocked = false;` | 清除既有重疊，允許下次重新走入。 |
| 103 | `return;` | 此幀沒有觸發。 |
| 106 | `if (wasInsideWhileLocked)` | 若一解鎖就已站在區域內，還要等先離開。 |
| 107 | `return;` | 結束本次判定。 |
| 109 | `if (pivot == null && !CreateGrid())` | 第一次才建立模型。舊轉軸存在時利用短路運算略過建立。 |
| 111 | `Debug.LogError("GridRoadblock 需要一個含直立網格面的 GLB 或 Prefab。", this);` | 生成失敗時在 Console 說明需要有效的面。 |
| 112 | `finished = true;` | 標記結束，避免每幀重複報錯。 |
| 113 | `return;` | 停止目前影格。 |
| 116 | `// Topple away from the side the visitor approached.` | 註解說明路障的倒向應背離觀眾來向。 |
| 117 | `fallDirection = localHead.z > 0f ? -1f : 1f;` | 從正 Z 進來就使用負 X 旋轉方向，反面進來則反向。 |
| 118 | `triggeredAt = Time.time;` | 保存當前遊戲時間，作為倒數起點。 |
| 119 | `triggerFrame = Time.frameCount;` | 保存當前影格，供後一個路障避免同幀觸發。 |
| 120 | `triggered = true;` | 記下已經觸發。 |
| 121 | `pivot.gameObject.SetActive(true);` | 開啟轉軸及其子網格。 |
| 122 | `if (releasePreviousOnTrigger && previousRoadblock != null)` | 有開通知且前一個存在，才更新它的釋放時間。 |
| 123 | `previousRoadblock.releasedAt = Mathf.Min(previousRoadblock.releasedAt, triggeredAt);` | 以最早收到的通知為準，較晚的通知不會推遲倒下。 |
| 126 | `float timedFallAt = waitForRelease ? float.PositiveInfinity : triggeredAt + Mathf.Max(0f, fallDelay);` | 計算預定的倒下時間。等待通知時給正無限大，普通模式則從觸發時刻加延遲。 |
| 127 | `float fallTime = Time.time - Mathf.Min(timedFallAt, releasedAt);` | 在倒數與通知中選較早的開始時間，從目前時間扣掉它。 |
| 128 | `if (fallTime < 0f)` | 若還沒到開始時間就維持當前姿勢。 |
| 129 | `return;` | 結束本幀動畫更新。 |
| 131 | `float progress = fallDuration > 0f ? Mathf.Clamp01(fallTime / fallDuration) : 1f;` | 把已倒下時間換成 0 到 1 的進度。0 秒的動畫直接完成。 |
| 132 | `pivot.localRotation = Quaternion.Euler(fallDirection * 90f * progress * progress, 0f, 0f);` | 繞本地 X 軸倒到 90°。進度平方讓起初轉得慢，後段加速。 |
| 133 | `if (progress >= 1f)` | 進度到 1 代表完全倒平。 |
| 135 | `pivot.gameObject.SetActive(false);` | 隱藏整個網格。 |
| 136 | `finished = true;` | 標記本輪完成，後續影格不再觸發。 |

## 頭顯來源

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 140 | `private static Transform FindHeadset()` | 定義尋找追蹤點的共用方法。 |
| 142 | `OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();` | 搜尋目前載入場景的第一個啟用 Meta Camera Rig。 |
| 143 | `if (rig != null && rig.centerEyeAnchor != null)` | Rig 和中央頭顯錨點都存在時優先使用。 |
| 144 | `return rig.centerEyeAnchor;` | 回傳 CenterEyeAnchor。 |
| 146 | `Camera main = Camera.main;` | 找不到 Rig 時備援取得 MainCamera 標籤的相機。 |
| 147 | `return main != null ? main.transform : null;` | 若相機存在就用它的 Transform，否則回傳 null。 |

## 生成網格與轉軸

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 150 | `private bool CreateGrid()` | 定義從來源生成假路障板面的函式。 |
| 152 | `if (gridModel == null)` | 不接受未配置的模型。 |
| 153 | `return false;` | 回傳 false 代表生成失敗。 |
| 155 | `foreach (MeshFilter source in gridModel.GetComponentsInChildren<MeshFilter>(true))` | 掃描模型階層，包含停用的子物件。 |
| 157 | `Mesh mesh = source.sharedMesh;` | 取得目前來源的 Mesh。 |
| 158 | `if (mesh == null \|\| !source.TryGetComponent(out MeshRenderer sourceRenderer))` | 若缺少 Mesh 或 Renderer 就不能使用。 |
| 159 | `continue;` | 改檢查下一個來源。 |
| 161 | `Bounds bounds = mesh.bounds;` | 取得網格局部包圍盒。 |
| 162 | `bool widthAlongZ = bounds.size.z > bounds.size.x;` | 用較大的水平尺寸判斷寬度在哪個軸。 |
| 163 | `float meshWidth = widthAlongZ ? bounds.size.z : bounds.size.x;` | 根據判斷取出來源寬度。 |
| 164 | `if (meshWidth < 0.001f \|\| bounds.size.y < 0.001f)` | 過小的寬高無法做有效縮放。 |
| 165 | `continue;` | 跳過不合格來源。 |
| 167 | `pivot = new GameObject("Grid Fall Pivot").transform;` | 建立位於路障原點的倒下轉軸。 |
| 168 | `pivot.SetParent(transform, false);` | 將轉軸掛在路障本體下。 |
| 169 | `pivot.gameObject.SetActive(false);` | 先隱藏，待觸發流程完成後才顯示。 |
| 171 | `Transform panel = new GameObject("Grid Panel").transform;` | 建立控制板面寬高的容器。 |
| 172 | `panel.SetParent(pivot, false);` | 掛到倒下轉軸下。 |
| 173 | `panel.localScale = new Vector3(width / meshWidth, height / bounds.size.y, 1f);` | 寬高各除以原始模型尺寸，厚度維持原狀。 |
| 175 | `Transform model = new GameObject("Mesh").transform;` | 建立實際渲染 Mesh 的物件。 |
| 176 | `model.SetParent(panel, false);` | 把它掛在板面容器裡。 |
| 177 | `model.localRotation = widthAlongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;` | 若寬度沿 Z，就轉到沿 X。 |
| 178 | `model.localPosition = Vector3.up * bounds.extents.y - model.localRotation * bounds.center;` | 抵銷旋轉後的中心偏移，再抬半個高度，讓底邊接在轉軸。 |
| 179 | `model.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;` | 加入 MeshFilter 並共享原模型網格。 |
| 180 | `model.gameObject.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;` | 加入 MeshRenderer 並共享原材質。 |
| 181 | `return true;` | 找到一面並完成建構後回報成功。 |
| 184 | `return false;` | 掃描完沒有可用模型時回報失敗。 |

## 選取時的定位框

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 187 | `private void OnDrawGizmosSelected()` | 選到路障時 Unity 會加畫定位輔助線。 |
| 189 | `Gizmos.matrix = transform.localToWorldMatrix;` | 採用物件的世界變換，讓輔助線跟隨位移旋轉。 |
| 190 | `Gizmos.color = Color.yellow;` | 選擇黃色畫邊界輪廓。 |
| 191 | `Gizmos.DrawWireCube(Vector3.up * height * 0.5f, new Vector3(width, height, 0.03f));` | 用扁薄線框標出網格立起時的尺寸。 |
| 192 | `Gizmos.color = Color.cyan;` | 選擇青色表示觸發區。 |
| 193 | `Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0.02f, triggerDistance * 2f));` | 在地面畫出以路障為中心、總深度為兩倍觸發距離的方框。 |

## 共用網格預覽工具

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 197 | `#if UNITY_EDITOR` | 以下是 Editor 專用的共用模型預覽工具。 |
| 198 | `internal static class WalkingGridPreview` | static 類別不需要掛元件，internal 允許同組件的 WalkingBoundary 使用。 |
| 200 | `internal static Mesh FindPanel(GameObject model)` | 從傳入模型取得一份可預覽的直立網格。 |
| 202 | `if (model == null)` | 未指定模型時沒有可搜尋的物件。 |
| 203 | `return null;` | 提早回傳 null。 |
| 205 | `foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))` | 遍歷各個子物件的 MeshFilter。 |
| 207 | `Mesh mesh = filter.sharedMesh;` | 取共享的 Mesh。 |
| 208 | `if (mesh != null && filter.TryGetComponent(out MeshRenderer _) &&` | 先確認可渲染。out 底線代表檢查 Renderer 是否存在即可，不留引用。 |
| 209 | `mesh.bounds.size.y >= 0.001f && Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z) >= 0.001f)` | 再確認寬高都大於最小尺寸。 |
| 210 | `return mesh;` | 回傳第一份符合條件的 Mesh。 |
| 212 | `return null;` | 搜尋完沒有結果就回傳 null。 |
| 215 | `internal static void DrawPanel(Mesh mesh, Matrix4x4 panel, float width, float height, Color color)` | 定義按指定矩陣、寬、高和顏色繪製預覽的方法。 |
| 217 | `Bounds bounds = mesh.bounds;` | 取得來源包圍盒。 |
| 218 | `bool widthAlongZ = bounds.size.z > bounds.size.x;` | 確認寬度是否沿 Z。 |
| 219 | `float meshWidth = widthAlongZ ? bounds.size.z : bounds.size.x;` | 按結果取得模型寬度。 |
| 220 | `Quaternion rotation = widthAlongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;` | 將板面統一轉成寬度沿 X。 |
| 221 | `Vector3 position = Vector3.up * bounds.extents.y - rotation * bounds.center;` | 計算使底部對齊原點的修正位置。 |
| 222 | `Gizmos.matrix = panel * Matrix4x4.Scale(new Vector3(width / meshWidth, height / bounds.size.y, 1f))` | 從呼叫方給的世界矩陣開始，乘上寬高的縮放矩陣。 |
| 223 | `* Matrix4x4.TRS(position, rotation, Vector3.one);` | 最後乘來源模型的校正位移與旋轉。矩陣從右往左作用於頂點。 |
| 224 | `Gizmos.color = color;` | 把指定顏色套給 Gizmos。 |
| 225 | `Gizmos.DrawMesh(mesh);` | 把共享網格畫在 Scene 供編輯定位。 |
| 228 | `#endif` | 結束這組 Editor 專用函式。 |
