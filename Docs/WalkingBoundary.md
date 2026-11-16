# WalkingBoundary.cs 逐行說明

[原始碼](../Assets/WalkingBoundary.cs) 負責三種路線、邊界顯示與越界畫面。表格略過空白行和單獨的區塊括號。

## 收縮流程

- 方形走近網格時先登記本次收縮。直線與混合斜線在折返時登記。當前尺寸在這一刻保持不變。
- 下一步會把相同的頭顯位置分別投進目前區域與預計縮小的區域。兩者都離開網格顯示帶並保留 2 公分緩衝時，才在網格隱藏的那一幀套用尺寸。下一次看到網格時，已經是縮小後的位置。
- 模型讀取同一個已生效的區域尺寸。這個流程隱藏的是網格，場景模型若在視野內，仍可能看出尺寸切換。
- 以方形半邊 2.5 公尺、每邊縮 0.125 公尺、顯示距離 0.6 公尺為例，退到 X = 1.8 時舊網格已隱藏，但新邊界距離只剩 0.575 公尺，所以先等待。退到 X = 1.7 時新邊界距離為 0.675 公尺，才會套用收縮。

## Inspector 與欄位

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 1 | `using System;` | 匯入 System，供 Layout 使用 Action 委派。 |
| 2 | `using System.Collections.Generic;` | 匯入泛型集合，供 List 與 Dictionary 使用。 |
| 3 | `using UnityEngine;` | 匯入 Unity 的元件、向量、相機及數學 API。 |
| 4 | `#if UNITY_EDITOR` | 從這裡起只編譯進 Unity Editor。 |
| 5 | `using UnityEngine.InputSystem;` | 匯入新版 Input System 的 Keyboard。 |
| 6 | `using UnityEngine.XR;` | 匯入 XRSettings，用來判斷是否正在使用 XR 裝置。 |
| 7 | `#endif` | 結束 Editor 條件編譯區塊。 |
| 9 | `[DisallowMultipleComponent]` | 同一個 GameObject 只能掛一個 WalkingBoundary。 |
| 10 | `public sealed class WalkingBoundary : MonoBehaviour` | 定義可掛到 GameObject 的元件。sealed 表示不能用繼承擴充這個類別。 |
| 12 | `public enum Route { Square = 0, Straight = 1, Hybrid = 2 }` | 宣告三種路線的整數值。方形沿用舊圓形的 0，直線沿用 1，讓已存的場景選項能對應到新版本。 |
| 14 | `[Header("路線與模型")]` | 新增 Inspector「路線與模型」分組標題。 |
| 15 | `[SerializeField] private Route route;` | 保存所選路線。SerializeField 讓 private 欄位仍能被 Unity 儲存及編輯。 |
| 16 | `[Tooltip("指定 XR 頭顯追蹤點，例如 Meta Camera Rig 的 CenterEyeAnchor。")]` | 在 Inspector 提示要填哪一個頭顯追蹤點。 |
| 17 | `[SerializeField] private Transform headset;` | 保存追蹤物件的 Transform，執行時從它讀取頭的位置。 |
| 18 | `[SerializeField] private GameObject footsteps;` | 保存腳步模型。方形不產生腳步，直線階段會依此模型建立指引。 |
| 19 | `[Tooltip("拖入網格 GLB 或 Prefab。使用第一個有 MeshRenderer 的網格面。")]` | 提示網格來源可以是 GLB 或 Prefab。 |
| 20 | `[SerializeField] private GameObject gridModel;` | 保存網格來源，初始化時會從中取網格與材質。 |
| 22 | `[Header("起始行走範圍（公尺）")]` | 新增以公尺為單位的初始尺寸分組。 |
| 23 | `[UnityEngine.Serialization.FormerlySerializedAs("circleDiameter")]` | 讀取舊場景中名為 circleDiameter 的值，轉存到新欄位。 |
| 24 | `[Min(1f), SerializeField] private float squareSize = 5f;` | 設定方形邊長預設 5 公尺。Min 限制 Inspector 可輸入的下限為 1。 |
| 25 | `[Min(0.5f), SerializeField] private float straightLength = 7f;` | 設定直線初始全長，預設 7 公尺。 |
| 26 | `[Min(0.5f), SerializeField] private float straightWidth = 2f;` | 設定直線寬度，預設 2 公尺。混合斜線也會使用此寬度作為上限。 |
| 28 | `[Header("收縮與顯示（公尺）")]` | 新增收縮和網格顯示的分組。 |
| 29 | `[Tooltip("方形每次觸發時各邊的內縮量，或直線每次折返時每端縮短的距離。")]` | 提示同一個數值在方形代表各邊內縮量，在直線代表每端縮短量。 |
| 30 | `[Min(0f), SerializeField] private float shrinkStep = 0.25f;` | 設定每次縮減量，腳本預設 0.25 公尺。場景上的已存值可覆寫此預設。 |
| 31 | `[Tooltip("第二種直線路線的最小全長。方形和混合路線的最終區域是 1 × 1 公尺。")]` | 說明 Minimum Size 只控制一般直線最小全長，方形與混合模式以 1 公尺作為終點。 |
| 32 | `[Min(0.5f), SerializeField] private float minimumSize = 1f;` | 保存一般直線最小全長，可在 Inspector 設定。 |
| 33 | `[Min(0f), SerializeField] private float revealDistance = 0.6f;` | 保存頭顯離邊緣多近才顯示網格的門檻。 |
| 34 | `[Min(0.1f), SerializeField] private float gridHeight = 2f;` | 保存網格垂直高度，預設 2 公尺。 |
| 36 | `[Header("越界畫面")]` | 新增越界顯示的 Inspector 分組。 |
| 37 | `[SerializeField] private bool solidColorOutside = true;` | 允許用 Inspector 開關越界純色效果，預設開啟。 |
| 38 | `[SerializeField] private Color outsideColor = Color.white;` | 保存越界背景顏色，預設純白。 |
| 40 | `[SerializeField, HideInInspector] private bool hybridDiagonal;` | 保存混合模式目前階段。HideInInspector 隱藏原始布林值，改由自訂按鈕操作。 |
| 42 | `[Header("腳步")]` | 新增腳步相關設定的分組。 |
| 43 | `[Min(0.2f), SerializeField] private float footstepSpacing = 0.55f;` | 保存腳步間距。程式會以此估算指引數量。 |
| 44 | `[Min(0f), SerializeField] private float pathInset = 0.25f;` | 保存起點和終點離實際邊界的內縮距離。 |
| 45 | `[Min(0.05f), SerializeField] private float arrivalTolerance = 0.2f;` | 保存抵達折返點時允許的誤差。 |
| 46 | `[SerializeField] private float footstepYaw;` | 保存腳步模型額外的 Y 軸旋轉角度。 |
| 48 | `#if UNITY_EDITOR` | 以下欄位與模擬資料只編譯進編輯器。 |
| 49 | `[Header("電腦測試")]` | 新增電腦測試分組。 |
| 50 | `[Tooltip("在 Unity Editor 沒有啟用 XR 顯示時，以方向鍵模擬走動。")]` | 提示方向鍵模擬的啟用條件。 |
| 51 | `[SerializeField] private bool simulateWithArrowKeys = true;` | 保存方向鍵模擬的開關。 |
| 52 | `[Min(0.1f), SerializeField] private float simulationSpeed = 1.5f;` | 保存鍵盤移動速度，單位為公尺每秒。 |
| 53 | `[SerializeField] private bool showScenePreview = true;` | 保存是否在 Scene 畫出模型預覽。 |
| 54 | `private Transform simulatedRig;` | 保存模擬應移動的 Rig 或頭顯 Transform。 |
| 55 | `private Vector3 simulatedOffset;` | 累加本腳本造成的世界座標位移，之後可扣回。 |
| 56 | `#endif` | 結束編輯器專用欄位區段。 |
| 58 | `private readonly List<Transform> steps = new();` | 建立腳步物件清單以重用實例。readonly 讓清單本身不可換成另一份。 |
| 59 | `private readonly List<Transform> panels = new();` | 建立網格面清單，索引對應四個邊。 |
| 60 | `private Transform guideRoot;` | 保存執行時腳步群組的根物件。 |
| 61 | `private Transform gridRoot;` | 保存執行時網格群組的根物件。 |
| 62 | `private Mesh panelMesh;` | 暫存從 GLB 取出的 Mesh。 |
| 63 | `private Material[] panelMaterials;` | 暫存來源的共享材質陣列供四面使用。 |
| 64 | `private Quaternion panelRotation;` | 保存把來源寬度軸轉成 X 軸所需的旋轉。 |
| 65 | `private Vector3 panelCenter;` | 保存旋轉後模型包圍盒的中心。 |
| 66 | `private Vector3 panelSize;` | 保存統一方向後的模型包圍尺寸。 |
| 67 | `private float extent;` | 保存目前的半長。方形時也等於半寬。 |
| 68 | `private float targetExtent;` | 保存已登記但尚未套用的目標半長。 |
| 69 | `private bool waitingAtStart;` | 記錄直線是否仍在等體驗者抵達起點。 |
| 70 | `private bool activeHybridDiagonal;` | 記錄上一次套用的混合階段，用於偵測設定變更。 |
| 71 | `private bool outsideViewActive;` | 記錄上一幀是否已切到純色，以便退出時恢復相機。 |
| 72 | `private readonly Dictionary<Camera, (int mask, CameraClearFlags flags, Color color)> cameraViews = new();` | 以 Camera 為索引保存原始遮罩、清除方式及背景顏色。 |
| 73 | `private int direction;` | 保存直線行進方向。1 朝本地正 Z，−1 朝本地負 Z。 |
| 74 | `private Route activeRoute;` | 保存上次實際初始化的模式。 |
| 76 | `private void OnEnable()` | Unity 啟用這個元件時呼叫 OnEnable，建立執行所需的資源。 |
| 78 | `if (headset == null \|\| gridModel == null)` | 檢查頭顯或網格是否未指定。任一缺少都無法處理。 |
| 80 | `Debug.LogError("WalkingBoundary 需要頭顯和有 MeshRenderer 的網格面。", this);` | 把缺少引用的情況寫到 Console，附上元件供定位。 |
| 81 | `enabled = false;` | 停用元件，避免之後每幀使用空引用。 |
| 82 | `return;` | 立即結束初始化。 |
| 85 | `foreach (MeshFilter filter in gridModel.GetComponentsInChildren<MeshFilter>(true))` | 列舉模型階層內的 MeshFilter。true 也搜尋停用的子物件。 |
| 87 | `if (filter.sharedMesh == null \|\| !filter.TryGetComponent(out MeshRenderer renderer))` | 若沒有 Mesh 或同物件找不到 MeshRenderer，該物件不能當成渲染來源。 |
| 88 | `continue;` | 略過目前項目並檢查下一個。 |
| 90 | `panelMesh = filter.sharedMesh;` | 選用來源的共享網格。 |
| 91 | `panelMaterials = renderer.sharedMaterials;` | 取出來源的共享材質，保留原來外觀。 |
| 92 | `break;` | 找到第一個來源後結束迴圈。 |
| 95 | `if (panelMesh == null)` | 若掃描完還沒找到任何可渲染網格，進入錯誤分支。 |
| 97 | `Debug.LogError("WalkingBoundary 找不到可用的網格面。", this);` | 在 Console 說明網格來源問題。 |
| 98 | `enabled = false;` | 停用元件，避免後續生成無效網格。 |
| 99 | `return;` | 結束初始化。 |
| 102 | `Bounds bounds = panelMesh.bounds;` | 讀取 Mesh 自身座標中的包圍盒。 |
| 103 | `bool widthAlongZ = bounds.size.z > bounds.size.x;` | 比較 X 和 Z 尺寸，以較長的水平軸當作板面的寬度。 |
| 104 | `panelRotation = widthAlongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;` | 來源寬度沿 Z 時繞 Y 軸轉 90 度，否則用不旋轉的 identity。 |
| 105 | `panelSize = widthAlongZ ? new Vector3(bounds.size.z, bounds.size.y, bounds.size.x) : bounds.size;` | 若轉過 90 度，就交換 X、Z 尺寸以反映新的寬度和厚度。 |
| 106 | `panelCenter = panelRotation * bounds.center;` | 把包圍盒中心旋轉到與網格板相同的方向。 |
| 107 | `if (panelSize.x < 0.001f \|\| panelSize.y < 0.001f)` | 檢查板面寬與高是否近乎零，避免稍後除以零。 |
| 109 | `Debug.LogError("WalkingBoundary 的網格模型需要直立的網格面。", this);` | 在 Console 說明網格應為直立且具有可用的寬高。 |
| 110 | `enabled = false;` | 停用元件。 |
| 111 | `return;` | 結束初始化。 |
| 114 | `guideRoot = CreateRoot("Footstep Guide");` | 建立 Footstep Guide 子群組。 |
| 115 | `gridRoot = CreateRoot("Boundary Grid");` | 建立 Boundary Grid 子群組。 |
| 116 | `OVRCameraRig cameraRig = headset.GetComponentInParent<OVRCameraRig>();` | 沿頭顯的父階層搜尋 Meta 的 OVRCameraRig。 |
| 117 | `Camera[] cameras = cameraRig != null ? cameraRig.GetComponentsInChildren<Camera>(true)` | 找到 Rig 時搜尋它底下所有 Camera，包含為左右眼保留的停用相機。 |
| 118 | `: headset.GetComponentsInChildren<Camera>(true);` | 找不到 Rig 時改搜尋所指定的頭顯物件及其子物件。 |
| 119 | `foreach (Camera camera in cameras)` | 逐一巡覽選定的相機。 |
| 120 | `cameraViews.Add(camera, (camera.cullingMask, camera.clearFlags, camera.backgroundColor));` | 把三個原始設定快照成具名稱的 tuple，供越界後回復。 |
| 121 | `ResetRoute();` | 套用模式的初始尺寸、方向及版面。 |
| 122 | `#if UNITY_EDITOR` | 開始 Editor 專用初始化。 |
| 123 | `OVRCameraRig rig = headset.GetComponentInParent<OVRCameraRig>();` | 在頭顯上層找到要移動的 Meta Camera Rig。 |
| 124 | `simulatedRig = rig != null ? rig.transform : headset;` | 有 Rig 就移動整組，否則以頭顯 Transform 作為模擬目標。 |
| 125 | `#endif` | 結束 Editor 專用初始化。 |
| 128 | `private void OnDisable()` | Unity 停用元件時呼叫 OnDisable，負責清理。 |
| 130 | `#if UNITY_EDITOR` | 開始 Editor 專用清理。 |
| 131 | `ResetSimulatedOffset();` | 撤銷方向鍵累積的位移。 |
| 132 | `#endif` | 結束 Editor 專用清理。 |
| 133 | `SetOutsideView(false);` | 要求從純色畫面恢復相機。 |
| 134 | `cameraViews.Clear();` | 清除相機原始狀態快照。 |
| 135 | `ReleaseRoot(guideRoot);` | 隱藏並銷毀腳步根物件及所有子物件。 |
| 136 | `ReleaseRoot(gridRoot);` | 隱藏並銷毀邊界網格根物件及子物件。 |
| 137 | `steps.Clear();` | 清除腳步物件清單。 |
| 138 | `panels.Clear();` | 清除網格物件清單。 |
| 139 | `panelMesh = null;` | 釋放腳本持有的網格參考。 |
| 142 | `#if UNITY_EDITOR` | 開始只在 Editor 編譯的方向鍵更新區塊。 |
| 143 | `private void Update()` | Unity 每幀呼叫 Update，在 LateUpdate 邊界判斷之前套用模擬位移。 |
| 145 | `if (!simulateWithArrowKeys \|\| XRSettings.isDeviceActive)` | 如果方向鍵模擬關閉，或 XR 已經有啟用中的裝置，本幀停止模擬。 |
| 147 | `ResetSimulatedOffset();` | 扣回之前累加的模擬位移。 |
| 148 | `return;` | 結束本幀鍵盤處理。 |
| 151 | `Keyboard keyboard = Keyboard.current;` | 取得目前 Input System 鍵盤。 |
| 152 | `if (keyboard == null \|\| simulatedRig == null)` | 沒有鍵盤或沒有要移動的物件就略過。 |
| 153 | `return;` | 在缺少輸入或目標時立即返回。 |
| 155 | `float x = (keyboard.rightArrowKey.isPressed ? 1f : 0f) -` | 按右鍵時給 X 加 1。 |
| 156 | `(keyboard.leftArrowKey.isPressed ? 1f : 0f);` | 按左鍵時給 X 減 1，同按左右會抵銷。 |
| 157 | `float z = (keyboard.upArrowKey.isPressed ? 1f : 0f) -` | 按上鍵時給 Z 加 1。 |
| 158 | `(keyboard.downArrowKey.isPressed ? 1f : 0f);` | 按下鍵時給 Z 減 1，同按上下會抵銷。 |
| 159 | `Vector3 input = new(x, 0f, z);` | 組合成只在 XZ 平面移動的向量。 |
| 160 | `if (input == Vector3.zero)` | 向量為零表示沒有方向輸入。 |
| 161 | `return;` | 本幀不做任何移動。 |
| 163 | `Quaternion routeYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);` | 取邊界物件的水平旋轉，讓方向鍵和地面座標方向一致。 |
| 164 | `Vector3 movement = routeYaw * input.normalized * (simulationSpeed * Time.deltaTime);` | 把輸入正規化，使斜走與單軸同速，再乘速度及影格時間得到位移。 |
| 165 | `simulatedRig.position += movement;` | 把位移直接加到 Rig 的世界位置。 |
| 166 | `simulatedOffset += movement;` | 累計這次位移，以便停用時撤回。 |
| 169 | `private void ResetSimulatedOffset()` | 定義負責還原鍵盤位移的方法。 |
| 171 | `if (simulatedRig != null && simulatedOffset != Vector3.zero)` | 確認目標仍存在並且有累積位移。 |
| 172 | `simulatedRig.position -= simulatedOffset;` | 從目前世界位置扣回本腳本移動的部分。 |
| 173 | `simulatedOffset = Vector3.zero;` | 把記錄清零，避免下一次還原時重複扣除。 |
| 175 | `#endif` | 結束 Editor 專用鍵盤程式。 |
| 177 | `private static void ReleaseRoot(Transform root)` | 定義可用於兩個生成根物件的清理方法。 |
| 179 | `if (root == null)` | 允許傳入未成功建立或已被銷毀的根物件。 |
| 180 | `return;` | 沒有物件就結束。 |
| 182 | `root.gameObject.SetActive(false);` | 立即停用物件，因為 Destroy 會延後處理。 |
| 183 | `Destroy(root.gameObject);` | 請 Unity 銷毀整組 GameObject。 |
| 186 | `private Transform CreateRoot(string objectName)` | 定義建立子群組的函式，傳入名字並回傳 Transform。 |
| 188 | `Transform root = new GameObject(objectName).transform;` | 建立空 GameObject 並取它的 Transform。 |
| 189 | `root.SetParent(transform, false);` | 設掛載本腳本的物件為父物件，false 代表不保留原世界變換。 |
| 190 | `return root;` | 回傳建立好的根 Transform。 |
| 193 | `private void ResetRoute()` | 定義重設到所選階段起點的方法。 |
| 195 | `activeRoute = route;` | 記下目前 Route，讓 LateUpdate 知道已套用。 |
| 196 | `activeHybridDiagonal = hybridDiagonal;` | 記下目前混合階段。 |

## 路線尺寸與中心

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 197 | `extent = targetExtent = StartingExtent;` | 讓目前半長與目標半長都回到初始半長，清除尚未套用的收縮。 |
| 198 | `direction = 1;` | 初始方向為本地正 Z。 |
| 199 | `waitingAtStart = true;` | 重新要求直線先到達負端起點。 |
| 200 | `UpdateLayout();` | 依初始資料更新模型排列。 |
| 203 | `private bool IsSquare => route == Route.Square \|\| route == Route.Hybrid && !hybridDiagonal;` | 當路線是 Square，或 Hybrid 正處於方形階段時，使用方形行為。 |
| 204 | `private float SquareHalf => Mathf.Max(0.5f, squareSize * 0.5f);` | 方形全邊長乘 0.5 取得半邊長，至少 0.5 公尺。 |
| 205 | `private float StartingExtent => IsSquare ? SquareHalf : route == Route.Hybrid` | 起始半長先依是否方形選擇，若是方形就直接取半邊長。 |
| 206 | `? Mathf.Max(0.5f, (SquareHalf - 0.5f) * Mathf.Sqrt(2f)) : Mathf.Max(0.25f, straightLength * 0.5f);` | 斜線用角落區中心到原中心的對角距離。一般直線用全長除以 2。 |
| 207 | `private float HalfWidth => GetHalfWidth(extent);` | 取得當前半寬。 |
| 208 | `private float GetHalfWidth(float halfLength) => IsSquare ? halfLength : route == Route.Hybrid` | 用指定半長預測當前或收縮後的半寬。 |
| 209 | `? Mathf.Min(halfLength, Mathf.Max(0.5f, straightWidth * 0.5f)) : Mathf.Max(0.25f, straightWidth * 0.5f);` | 混合斜線的寬度可能隨長度收縮，一般直線仍維持固定寬度。 |
| 210 | `private float MinimumExtent => route == Route.Straight` | 決定收縮下限，先區分一般直線與另外兩種路線。 |
| 211 | `? Mathf.Clamp(minimumSize * 0.5f, 0.25f, StartingExtent) : 0.5f;` | 一般直線以設定的最小全長除以 2 並限制於合理範圍，其餘固定半長 0.5。 |
| 212 | `private Vector3 AreaCenter => GetAreaCenter(extent);` | 取得當前區域中心。 |
| 213 | `private Vector3 GetAreaCenter(float halfLength) => route == Route.Hybrid && IsSquare` | 用指定半長預測中心。 |
| 214 | `? new Vector3(halfLength - SquareHalf, 0f, SquareHalf - halfLength) : Vector3.zero;` | 混合方形的中心隨收縮移向左上角。 |
| 215 | `private Quaternion AreaRotation => route == Route.Hybrid && hybridDiagonal` | 只有混合斜線需要旋轉地面座標系。 |
| 216 | `? Quaternion.Euler(0f, 135f, 0f) : Quaternion.identity;` | 繞 Y 軸轉 135°，使正 Z 行進方向朝原方形的右下角。 |
| 217 | `private float PathExtent => extent - Mathf.Clamp(pathInset, 0f, extent * 0.45f);` | 腳步端點在邊界前預留 pathInset，預留量上限是半長的 45%。 |
| 218 | `private float Tolerance => Mathf.Clamp(arrivalTolerance, 0.05f, PathExtent * 0.45f);` | 限制折返容許誤差，避免路線很短時起點和終點判定重疊。 |
| 220 | `public void SetHybridDiagonal(bool diagonal)` | 提供 Inspector 或腳本切換混合階段。 |
| 222 | `if (route != Route.Hybrid \|\| hybridDiagonal == diagonal)` | 若不是 Hybrid 或已經在要求的階段，就不重設。 |
| 223 | `return;` | 結束這次切換。 |
| 224 | `hybridDiagonal = diagonal;` | 記錄要使用的階段。 |
| 225 | `if (Application.isPlaying && gridRoot != null)` | 僅在 Play 且執行物件已建立時立即重設。 |
| 226 | `ResetRoute();` | 重算所選階段起始狀態。編輯時只儲存變數供下次 Play。 |
| 229 | `private void LateUpdate()` | Unity 在本幀 Update 後呼叫，這裡讀頭顯位置並處理邊界。 |

## 收縮等待與網格可見性

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 231 | `if (route != activeRoute \|\| hybridDiagonal != activeHybridDiagonal)` | 檢查使用者的模式或階段是否和上次套用的不同。 |
| 232 | `ResetRoute();` | 若已變更就從該階段起始尺寸重設。 |
| 234 | `Vector3 headPosition = transform.InverseTransformPoint(headset.position);` | 把頭顯世界位置轉換成邊界控制器本地位置。 |
| 235 | `Vector3 local = ToAreaPosition(headPosition, extent);` | 用當前尺寸換算成區域座標。 |
| 236 | `if (extent != targetExtent)` | 目前半長與目標不同表示還在做收縮動畫。 |
| 238 | `if (IsClearOfGrid(local, extent) &&` | 先確認體驗者已離開目前網格的顯示範圍。 |
| 239 | `IsClearOfGrid(ToAreaPosition(headPosition, targetExtent), targetExtent))` | 也要確認同一位置在收縮後仍不會顯示網格。 |
| 241 | `// Both layouts are hidden from this position. Apply the new size before rendering.` | 註解說明尺寸在渲染前一次套用。 |
| 242 | `HideUnused(panels, 0);` | 先停用所有網格面。 |
| 243 | `extent = targetExtent;` | 採用新的半長。 |
| 244 | `UpdateLayout();` | 同步更新腳步和網格。模型跟隨元件會讀取這個新尺寸。 |
| 245 | `local = ToAreaPosition(headPosition, extent);` | 用新中心及尺寸重新計算頭顯的區域座標。 |
| 248 | `else if (route == Route.Hybrid && IsSquare && extent <= MinimumExtent)` | 若混合的方形階段已收縮完成到最小值，就進入交接。 |
| 250 | `SetHybridDiagonal(true);` | 切到斜線階段並重設版面。 |
| 251 | `local = ToAreaPosition(headPosition, extent);` | 混合路線切換後，重新換算中心和方向。 |
| 253 | `else if (IsSquare)` | 若尚未切換且當前是方形，使用靠近事件。 |
| 255 | `if (NearestEdgeDistance(local, extent) <= RevealThreshold(extent))` | 方形靠近任意一面到達顯示門檻時登記收縮。 |
| 256 | `QueueShrink();` | 寫入目標尺寸，等體驗者退回後才套用。 |
| 258 | `else` | 其他情況是一般直線或混合斜線。 |
| 260 | `TrackStraight(new Vector2(local.x, local.z));` | 非方形路線仍使用折返位置作為觸發條件。 |
| 263 | `UpdateVisibility(local);` | 用更新後的座標決定顯示哪面及是否越界。 |
| 266 | `private Vector3 ToAreaPosition(Vector3 position, float halfLength) =>` | 定義把控制器本地座標轉成區域座標的工具。 |
| 267 | `Quaternion.Inverse(AreaRotation) * (position - GetAreaCenter(halfLength));` | 扣掉該尺寸的區域中心，再反轉區域旋轉。 |
| 269 | `private float NearestEdgeDistance(Vector3 position, float halfLength) =>` | 定義頭顯到最近邊界的有號距離。 |
| 270 | `Mathf.Min(GetHalfWidth(halfLength) - Mathf.Abs(position.x), halfLength - Mathf.Abs(position.z));` | 取左右和前後的最小距離。負值表示越界。 |
| 272 | `private float RevealThreshold(float halfLength) =>` | 依指定半長計算目前或預計的顯示門檻。 |
| 273 | `Mathf.Min(revealDistance, 0.85f * Mathf.Min(GetHalfWidth(halfLength), halfLength));` | 門檻最多為最短半邊的 85%，讓很小的區域也有隱藏網格的中心區。 |
| 275 | `private bool IsClearOfGrid(Vector3 position, float halfLength) =>` | 檢查頭顯是否已離開指定尺寸下的網格顯示帶。 |
| 276 | `NearestEdgeDistance(position, halfLength) > RevealThreshold(halfLength) + 0.02f;` | 在顯示門檻以內側再留 2 公分緩衝。 |
| 278 | `private void UpdateVisibility(Vector3 position)` | 定義網格顯示和越界畫面的方法。 |
| 280 | `float left = HalfWidth + position.x;` | 計算左邊有號距離。位置的 X 越負就越靠近左邊。 |
| 281 | `float right = HalfWidth - position.x;` | 計算右邊有號距離。位置的 X 越正就越靠近右邊。 |
| 282 | `float near = extent + position.z;` | 計算負 Z 端有號距離。 |
| 283 | `float far = extent - position.z;` | 計算正 Z 端有號距離。 |
| 284 | `float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(near, far));` | 選出四邊最小的有號距離。負值表示至少跨出一邊。 |
| 285 | `float threshold = RevealThreshold(extent);` | 依當前尺寸算網格顯示門檻。 |
| 286 | `int visible = -1;` | 以 −1 表示本幀沒有要顯示的面。 |
| 287 | `if (nearest <= threshold)` | 進入顯示距離時才把索引改成有效值。 |
| 288 | `visible = nearest == left ? 0 : nearest == right ? 1 : nearest == near ? 2 : 3;` | 按照左、右、近、遠對應 0 到 3。等距時先選前面列出的那面。 |
| 289 | `for (int i = 0; i < panels.Count; i++)` | 逐一檢查四個實例。 |
| 291 | `if (panels[i].gameObject.activeSelf != (i == visible))` | 比較目前 Active 狀態和應有狀態，只有不一致時才切換。 |
| 292 | `panels[i].gameObject.SetActive(i == visible);` | 只啟用選到的面，其餘保持隱藏。 |
| 294 | `SetOutsideView(solidColorOutside && nearest < 0f);` | 當開關已開且最小距離小於零時，顯示越界純色。 |

## 越界畫面

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 297 | `private void SetOutsideView(bool outside)` | 定義相機純色切換及還原。 |
| 299 | `foreach (var entry in cameraViews)` | 巡覽初始化時保留的每個相機及設定。 |
| 301 | `Camera camera = entry.Key;` | 從字典項目取相機物件。 |
| 302 | `if (camera == null)` | 若相機已不存在就跳過。 |
| 303 | `continue;` | 繼續檢查下一台。 |
| 304 | `var original = entry.Value;` | 取得相機的原始遮罩、清除方式與顏色。 |
| 305 | `if (outside)` | 已判定越界時執行顏色覆蓋。 |
| 307 | `camera.cullingMask = 0;` | 遮罩清零，讓這台相機不繪製場景圖層。 |
| 308 | `camera.clearFlags = CameraClearFlags.SolidColor;` | 相機清除方式改成純色背景。 |
| 309 | `camera.backgroundColor = outsideColor;` | 套上 Inspector 指定顏色。 |
| 311 | `else if (outsideViewActive)` | 若現在沒越界但上次有，就執行還原。 |
| 313 | `camera.cullingMask = original.mask;` | 恢復原本要渲染的圖層。 |
| 314 | `camera.clearFlags = original.flags;` | 恢復原本的清除方式，例如天空盒。 |
| 315 | `camera.backgroundColor = original.color;` | 恢復原背景色。 |
| 318 | `outsideViewActive = outside;` | 記下本幀狀態供下一幀比較。 |

## 直線折返與待縮尺寸

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 321 | `private void TrackStraight(Vector2 position)` | 定義直線和混合斜線的折返判定。 |
| 323 | `if (Mathf.Abs(position.x) > HalfWidth)` | 頭顯的橫向位置超出半寬時不接受抵達。 |
| 324 | `return;` | 本幀結束路線進度判斷。 |
| 326 | `if (waitingAtStart)` | 若本階段剛開始，必須先走到起點。 |
| 328 | `if (position.y * direction <= -PathExtent + Tolerance)` | 把位置乘目前方向後，檢查是否已走到負端路徑附近。 |
| 329 | `waitingAtStart = false;` | 抵達後解除等待，開始接受另一端的折返。 |
| 330 | `return;` | 此幀只判起點。 |
| 333 | `if (position.y * direction >= PathExtent - Tolerance)` | 沿目前方向走進正端允許誤差內時視為到達。 |
| 335 | `direction = -direction;` | 把方向正負號反轉。 |
| 336 | `QueueShrink();` | 把這次折返登記為待套用的收縮。 |
| 337 | `UpdateLayout();` | 立即更新腳步朝向。 |
| 341 | `private void QueueShrink()` | 計算下一次收縮要使用的尺寸。 |
| 343 | `targetExtent = Mathf.Max(MinimumExtent, extent - Mathf.Max(0f, shrinkStep));` | 從半長減去每次縮減量，並限制在最小尺寸以上。 |

## 排版與模型生成

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 346 | `private void UpdateLayout()` | 定義把當前狀態套到場景物件的函式。 |
| 348 | `guideRoot.SetLocalPositionAndRotation(AreaCenter, AreaRotation);` | 讓腳步根物件套用區域中心和旋轉。 |
| 349 | `gridRoot.SetLocalPositionAndRotation(AreaCenter, AreaRotation);` | 讓網格根物件使用同一個中心和旋轉。 |
| 350 | `int stepCount = Layout(extent, HalfWidth, IsSquare, direction, PlaceStep, PlacePanel);` | 呼叫通用排版，傳入實際生成腳步和網格的函式，取得需要的腳步數量。 |
| 351 | `HideUnused(steps, footsteps != null ? stepCount : 0);` | 關掉超出數量的舊腳步。來源空白時全部關掉。 |
| 354 | `private int Layout(float length, float width, bool square, int heading,` | 定義排版輸入的半長、半寬、方形開關和前進方向。 |
| 355 | `Action<int, Vector3, float> placeStep, Action<int, Vector3, Vector3> placePanel)` | 兩個 Action 是回呼。排版只提供位置資料，呼叫方決定生成實體或畫 Gizmos。 |
| 357 | `int stepCount = 0;` | 從沒有腳步開始計數。 |
| 358 | `if (!square)` | 只有非方形階段需要排腳步。 |
| 360 | `float path = length - Mathf.Clamp(pathInset, 0f, length * 0.45f);` | 用半長扣掉端點內縮，取得腳步最遠的位置。 |
| 361 | `stepCount = Mathf.Clamp(Mathf.RoundToInt(2f * path / Mathf.Max(0.2f, footstepSpacing)) + 1, 2, 256);` | 以路徑全長除以間距估算數量。加 1 含兩端，並限制為 2 到 256。 |
| 362 | `for (int i = 0; i < stepCount; i++)` | 遍歷所有腳步索引。 |
| 364 | `float z = Mathf.Lerp(-path, path, (float)i / (stepCount - 1)) * heading;` | 以索引除以數量減 1 得到 0 到 1，再內插負端到正端位置。乘方向可把排列反轉。 |
| 365 | `placeStep(i, new Vector3(0f, 0f, z), heading > 0 ? 0f : 180f);` | 回呼接收索引、位於中線的位置和 0° 或 180° 的方向。 |
| 369 | `placePanel(0, new Vector3(-width, 0f, -length), new Vector3(-width, 0f, length));` | 供應左側網格的兩端點，索引 0。 |
| 370 | `placePanel(1, new Vector3(width, 0f, length), new Vector3(width, 0f, -length));` | 供應右側網格的兩端點，索引 1。 |
| 371 | `placePanel(2, new Vector3(width, 0f, -length), new Vector3(-width, 0f, -length));` | 供應負 Z 端網格的兩端點，索引 2。 |
| 372 | `placePanel(3, new Vector3(-width, 0f, length), new Vector3(width, 0f, length));` | 供應正 Z 端網格的兩端點，索引 3。 |
| 373 | `return stepCount;` | 回傳本次排版的腳步數目。 |
| 376 | `private void PlaceStep(int index, Vector3 position, float yaw)` | 定義更新 Play 中單一腳步的方法。 |
| 378 | `if (footsteps == null)` | 未指定腳步模型時不生成。 |
| 379 | `return;` | 直接返回。 |
| 381 | `if (index == steps.Count)` | 索引等於現有數量，代表需要補一個新實例。 |
| 382 | `steps.Add(Instantiate(footsteps, guideRoot).transform);` | 複製腳步模型當作群組子物件，保存 Transform。 |
| 384 | `Transform step = steps[index];` | 從清單取出這一個腳步。 |
| 385 | `step.localPosition = position + Vector3.up * 0.015f;` | 離地 0.015 公尺，可減少腳步和地面同高造成的閃爍。 |
| 386 | `step.localRotation = Quaternion.Euler(0f, yaw + footstepYaw, 0f);` | 把路徑方向加上腳步模型校正角，套為 Y 軸旋轉。 |
| 387 | `step.gameObject.SetActive(true);` | 啟用這個腳步。 |
| 390 | `private void PlacePanel(int index, Vector3 start, Vector3 end)` | 定義更新單面實體網格的方法。start 與 end 是本地端點。 |
| 392 | `if (index == panels.Count)` | 遇到尚未生成的面時才建立物件。 |
| 394 | `GameObject panel = new("Grid Panel");` | 建立網格面的父物件。 |
| 395 | `panel.transform.SetParent(gridRoot, false);` | 把它放在邊界網格群組底下。 |
| 396 | `panel.SetActive(false);` | 初始隱藏，之後由距離判定啟用。 |
| 397 | `GameObject mesh = new("Mesh");` | 建立實際渲染的子物件。 |
| 398 | `mesh.transform.SetParent(panel.transform, false);` | 掛到這一面底下以便與外層定位分開。 |
| 399 | `mesh.transform.localRotation = panelRotation;` | 將來源網格旋轉成寬度沿 X。 |
| 400 | `mesh.transform.localPosition = Vector3.up * (panelSize.y * 0.5f) - panelCenter;` | 抵銷模型中心偏移，再抬半個模型高度，使底部落在群組地面。 |
| 401 | `mesh.AddComponent<MeshFilter>().sharedMesh = panelMesh;` | 加入 MeshFilter 並指定共享的網格資料。 |
| 402 | `MeshRenderer renderer = mesh.AddComponent<MeshRenderer>();` | 加入 MeshRenderer 讓這個模型可被繪製。 |
| 403 | `renderer.sharedMaterials = panelMaterials;` | 連接來源材質，使四面共用同一外觀。 |
| 404 | `renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;` | 停用網格投射陰影。 |
| 405 | `panels.Add(panel.transform);` | 保存這一面以供之後移動及縮放。 |
| 408 | `Transform instance = panels[index];` | 從清單取得已存在或剛建立的那面。 |
| 409 | `Vector3 edge = end - start;` | 以終點減起點得到邊的方向與長度。 |
| 410 | `instance.localPosition = (start + end) * 0.5f;` | 以兩端平均取得邊的中點。 |
| 411 | `instance.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, edge));` | 用上方向與邊向量的叉積求板面法線，再建立旋轉。 |
| 412 | `instance.localScale = new Vector3(edge.magnitude / panelSize.x, gridHeight / panelSize.y, 1f);` | 以目標邊長和高度各除以來源寬高，得到 X 和 Y 倍率，厚度不變。 |
| 416 | `private static void HideUnused(List<Transform> objects, int used)` | 定義隱藏多餘實例的小工具。 |
| 418 | `for (int i = used; i < objects.Count; i++)` | 從仍使用數量的位置開始巡覽後面的索引。 |
| 419 | `objects[i].gameObject.SetActive(false);` | 停用每個多餘實例，以便未來重用。 |

## Scene 預覽

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 422 | `#if UNITY_EDITOR` | 開始 Editor 專用的 Gizmos 預覽。 |
| 423 | `private void OnDrawGizmos()` | Scene 視窗要求畫 Gizmos 時呼叫。 |
| 425 | `if (Application.isPlaying \|\| !enabled \|\| !showScenePreview)` | 如果正在 Play、元件沒啟用或預覽已關閉，就不要畫。 |
| 426 | `return;` | 結束這次繪圖。 |
| 428 | `Matrix4x4 world = transform.localToWorldMatrix * Matrix4x4.Rotate(AreaRotation);` | 組合控制器的世界矩陣與路線朝向，使預覽跟著物件移動及旋轉。 |
| 429 | `MeshFilter[] stepMeshes = footsteps != null ? footsteps.GetComponentsInChildren<MeshFilter>() : null;` | 有腳步來源時取出其子 MeshFilter，沒有時記為 null。 |
| 430 | `Mesh grid = WalkingGridPreview.FindPanel(gridModel);` | 透過共用的編輯器工具取得可畫的直立網格。 |
| 431 | `Matrix4x4 oldMatrix = Gizmos.matrix;` | 記住原本的 Gizmos 矩陣。 |
| 432 | `Color oldColor = Gizmos.color;` | 記住原本的 Gizmos 顏色。 |
| 433 | `float previewWidth = IsSquare ? StartingExtent : route == Route.Hybrid` | 方形預覽寬等於起始半長，其他模式分開算。 |
| 434 | `? Mathf.Min(StartingExtent, Mathf.Max(0.5f, straightWidth * 0.5f)) : HalfWidth;` | 混合斜線依該階段的起始半長限制寬度，一般直線使用 HalfWidth。 |
| 435 | `Layout(StartingExtent, previewWidth, IsSquare, 1,` | 以起始半長、寬度與方向呼叫和 Play 共用的 Layout。 |
| 436 | `(_, position, yaw) =>` | 第一個 lambda 準備畫腳步。底線代表不需要腳步索引。 |
| 438 | `if (stepMeshes == null)` | 沒有腳步 MeshFilter 時無法畫預覽模型。 |
| 439 | `return;` | 只跳過這個腳步的繪圖。 |
| 441 | `Gizmos.color = new Color(1f, 0.75f, 0.2f);` | 把腳步預覽顏色設為黃色。 |
| 442 | `Matrix4x4 step = world * Matrix4x4.TRS(position + Vector3.up * 0.015f,` | 建立腳步姿勢的矩陣，位置和執行時同樣略微離地。 |
| 443 | `Quaternion.Euler(0f, yaw + footstepYaw, 0f), footsteps.transform.localScale);` | 設定腳步朝向和來源模型縮放。 |
| 444 | `foreach (MeshFilter filter in stepMeshes)` | 逐一巡覽腳步來源的 MeshFilter。 |
| 446 | `if (filter.sharedMesh == null)` | 若此 Filter 沒有 Mesh 就不能畫。 |
| 447 | `continue;` | 略過這個 Filter。 |
| 449 | `Gizmos.matrix = step * footsteps.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;` | 把子網格先換算到腳步來源本地，然後由腳步矩陣轉到預覽世界座標。 |
| 450 | `Gizmos.DrawMesh(filter.sharedMesh);` | 繪出該子網格。 |
| 452 | `},` | 結束腳步 lambda，接下來提供網格 lambda。 |
| 453 | `(_, start, end) =>` | 第二個 lambda 接收網格兩端，底線代表不需索引。 |
| 455 | `if (grid == null)` | 找不到來源網格時不畫這面。 |
| 456 | `return;` | 結束這次網格繪圖。 |
| 458 | `Vector3 edge = end - start;` | 以終點減起點取得邊方向。 |
| 459 | `Matrix4x4 panel = world * Matrix4x4.TRS((start + end) * 0.5f,` | 準備以兩端中點為位置的預覽矩陣，並轉到世界座標。 |
| 460 | `Quaternion.LookRotation(Vector3.Cross(Vector3.up, edge)), Vector3.one);` | 使用與實體網格相同的法線計算，暫時不縮放。 |
| 461 | `WalkingGridPreview.DrawPanel(grid, panel, edge.magnitude, gridHeight, new Color(0.2f, 0.8f, 1f, 0.55f));` | 交由預覽工具縮放到邊長及高度，以半透明藍色畫出。 |
| 462 | `});` | 結束網格 lambda 及 Layout 呼叫。 |
| 463 | `Gizmos.matrix = oldMatrix;` | 還原原本 Gizmos 變換，避免影響其他腳本。 |
| 464 | `Gizmos.color = oldColor;` | 還原原本 Gizmos 顏色。 |
| 466 | `#endif` | 結束 Editor 專用預覽程式。 |
| 468 | `// Ground dimensions are in boundary-local X and Z, before applying the area's rotation.` | 尺寸使用區域未旋轉前的本地 X、Z。 |
| 469 | `public Vector2 AuthoredAreaSize => route == Route.Straight` | 公開原始版面的全寬和全長，用來計算模型縮放倍率。 |
| 470 | `? new Vector2(Mathf.Max(0.5f, straightWidth), Mathf.Max(0.5f, straightLength))` | 直線使用 Inspector 的初始寬、長，下限為 0.5 公尺。 |
| 471 | `: Vector2.one * (2f * SquareHalf);` | Square 和 Hybrid 的模型基準都是初始方形。 |
| 473 | `public (Vector3 center, Quaternion rotation, Vector2 size) CurrentArea =>` | 公開當前區域的中心、旋轉與大小。 |
| 474 | `(AreaCenter, AreaRotation, new Vector2(2f * HalfWidth, 2f * extent));` | 使用跟網格一致的中心與旋轉，並將半寬與半長加倍。 |
