# ReadmeEditor.cs 逐行說明

Unity 範本說明資產的 Inspector。原始碼位於 [ReadmeEditor.cs](../Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs)。

行號以目前專案原始碼為準。每列對應一個有內容的原始碼行。空白行用來分段，單獨的 `{` 與 `}` 分別開啟和結束區塊，所以略過。跨行運算式按原始碼順序逐行拆開。

## 執行範圍

這支是 Unity URP 專案範本附的編輯器工具。Undo、階段切換和 VR 路線的 Inspector 則由 WalkingBoundaryEditor 處理。LoadLayout 透過反射依名稱呼叫 Unity 的版面方法，因此該方法名稱或簽章如果在版本升級後變動，這段需要重新確認。

## Editor 載入

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 1 | `using System.Collections;` | 匯入非泛型集合命名空間。此檔沒有直接使用。 |
| 2 | `using System.Collections.Generic;` | 匯入泛型集合命名空間。此檔沒有直接使用。 |
| 3 | `using UnityEngine;` | 匯入 GUI、顏色及 Unity 基本型別。 |
| 4 | `using UnityEditor;` | 匯入 Inspector、資產資料庫與編輯器 GUI API。 |
| 5 | `using System;` | 匯入 System。此檔沒有直接使用其型別。 |
| 6 | `using System.IO;` | 匯入檔案路徑及目錄操作。 |
| 7 | `using System.Reflection;` | 匯入反射用的 BindingFlags。 |
| 9 | `[CustomEditor(typeof(Readme))]` | 指定這個自訂 Inspector 負責 Readme 資產。 |
| 10 | `[InitializeOnLoad]` | 讓 Unity 在載入 Editor 組件時初始化此類別。 |
| 11 | `public class ReadmeEditor : Editor` | 定義繼承 Editor 的範本說明頁介面。 |
| 13 | `static string s_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";` | 設定本次 Editor 工作階段使用的布林值鍵名。 |
| 15 | `static string s_ReadmeSourceDirectory = "Assets/TutorialInfo";` | 設定範本教學資料夾路徑。 |
| 17 | `const float k_Space = 16f;` | 定義重複使用的間隔尺寸 16。 |
| 19 | `static ReadmeEditor()` | Unity 載入此類別時執行靜態建構子。 |
| 21 | `EditorApplication.delayCall += SelectReadmeAutomatically;` | 註冊延後呼叫，等待 Editor 可處理資產選取。 |

## 移除教學資產

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 24 | `static void RemoveTutorial()` | 定義移除範本教學資產的方法。 |
| 26 | `if (EditorUtility.DisplayDialog("Remove Readme Assets",` | 彈出確認視窗，使用第一個字串當標題。 |
| 28 | `$"All contents under {s_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",` | 把教學資料夾位置插進確認文字。 |
| 29 | `"Proceed",` | 設定確定按鈕名稱。 |
| 30 | `"Cancel"))` | 設定取消按鈕名稱，只有確認後才進入分支。 |
| 32 | `if (Directory.Exists(s_ReadmeSourceDirectory))` | 檢查範本資料夾是否存在。 |
| 34 | `FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory);` | 刪除資料夾及其中內容。 |
| 35 | `FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory + ".meta");` | 刪除對應的 Unity 資產中繼檔。 |
| 37 | `else` | 資料夾找不到時走另一個分支。 |
| 39 | `Debug.Log($"Could not find the Readme folder at {s_ReadmeSourceDirectory}");` | 將找不到的路徑寫到 Console。 |
| 42 | `var readmeAsset = SelectReadme();` | 呼叫 SelectReadme 尋找說明資產。 |
| 43 | `if (readmeAsset != null)` | 檢查資產是否有效。 |
| 45 | `var path = AssetDatabase.GetAssetPath(readmeAsset);` | 讀取資產在 Unity 專案內的路徑。 |
| 46 | `FileUtil.DeleteFileOrDirectory(path + ".meta");` | 刪除該資產的中繼檔。 |
| 47 | `FileUtil.DeleteFileOrDirectory(path);` | 刪除說明資產本身。 |
| 50 | `AssetDatabase.Refresh();` | 通知 Unity 重掃資產。 |

## 自行選取與載入布局

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 54 | `static void SelectReadmeAutomatically()` | 定義每個 Editor 工作階段自動開啟說明一次的流程。 |
| 56 | `if (!SessionState.GetBool(s_ShowedReadmeSessionStateName, false))` | 只在對應 SessionState 鍵還沒設為 true 時執行。 |
| 58 | `var readme = SelectReadme();` | 搜尋並選取 Readme。 |
| 59 | `SessionState.SetBool(s_ShowedReadmeSessionStateName, true);` | 不論搜尋結果如何，當前工作階段都已嘗試過。 |
| 61 | `if (readme && !readme.loadedLayout)` | 找到有效資產且尚未載入版面時才繼續。 |
| 63 | `LoadLayout();` | 載入範本提供的視窗布局。 |
| 64 | `readme.loadedLayout = true;` | 將此 Readme 的版面載入旗標設為 true。 |
| 69 | `static void LoadLayout()` | 定義透過反射開啟範本版面的函式。 |
| 71 | `var assembly = typeof(EditorApplication).Assembly;` | 取出 EditorApplication 型別所屬的組件。 |
| 72 | `var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);` | 從該組件找 UnityEditor.WindowLayout 型別，true 表示找不到就拋例外。 |
| 73 | `var method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public \| BindingFlags.Static);` | 取得名稱吻合的公開靜態方法。 |
| 74 | `method.Invoke(null, new object[] { Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"), false });` | 對靜態方法傳入 null 作為物件，並傳入版面檔完整路徑和布林參數。 |

## 搜尋資產

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 77 | `static Readme SelectReadme()` | 定義搜尋、選取並回傳 Readme 資產的函式。 |
| 79 | `var ids = AssetDatabase.FindAssets("Readme t:Readme");` | 以名稱和 t:Readme 型別篩選器取得資產 GUID。 |
| 80 | `if (ids.Length == 1)` | 只有剛好找到一份時才自動選取。 |
| 82 | `var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));` | 把 GUID 換成路徑，再載入該路徑的主要資產。 |
| 84 | `Selection.objects = new UnityEngine.Object[] { readmeObject };` | 設定 Unity 的選取物件，使 Inspector 轉到這份資產。 |
| 86 | `return (Readme)readmeObject;` | 把一般 Unity Object 轉成 Readme 型別並回傳。 |
| 88 | `else` | 若找到零份或多份則走備援分支。 |
| 90 | `Debug.Log("Couldn't find a readme");` | 將未能挑出說明資產寫到 Console。 |
| 91 | `return null;` | 回傳 null，讓呼叫端跳過後續操作。 |

## 畫標題

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 95 | `protected override void OnHeaderGUI()` | 覆寫 Inspector 的資產標題區。 |
| 97 | `var readme = (Readme)target;` | 將目前目標轉型為 Readme。 |
| 98 | `Init();` | 確保 GUI 樣式已建立。 |
| 100 | `var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);` | 依 Inspector 寬度計算圖示尺寸，上限為 128。 |
| 102 | `GUILayout.BeginHorizontal("In BigTitle");` | 開始水平標題排版，使用 Unity 的 In BigTitle 樣式。 |
| 104 | `if (readme.icon != null)` | 有圖示才加入圖示。 |
| 106 | `GUILayout.Space(k_Space);` | 在圖示前加入間隔。 |
| 107 | `GUILayout.Label(readme.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));` | 以固定寬高繪製圖示。 |
| 109 | `GUILayout.Space(k_Space);` | 在圖示區和文字區之間加間隔。 |
| 110 | `GUILayout.BeginVertical();` | 開始標題文字的垂直排版。 |
| 113 | `GUILayout.FlexibleSpace();` | 在標題上方填充可伸縮空間。 |
| 114 | `GUILayout.Label(readme.title, TitleStyle);` | 以標題樣式畫出 Readme 的 title。 |
| 115 | `GUILayout.FlexibleSpace();` | 在標題下方填充可伸縮空間。 |
| 117 | `GUILayout.EndVertical();` | 結束垂直排版。 |
| 118 | `GUILayout.FlexibleSpace();` | 填充標題區剩餘的水平空間。 |
| 120 | `GUILayout.EndHorizontal();` | 結束水平排版。 |

## 畫內容

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 123 | `public override void OnInspectorGUI()` | 覆寫 Inspector 的內容區。 |
| 125 | `var readme = (Readme)target;` | 取得目前選中的 Readme。 |
| 126 | `Init();` | 確保 GUI 樣式可用。 |
| 128 | `foreach (var section in readme.sections)` | 依資產陣列順序處理每段。 |
| 130 | `if (!string.IsNullOrEmpty(section.heading))` | 段落有標題才繪製。 |
| 132 | `GUILayout.Label(section.heading, HeadingStyle);` | 用 HeadingStyle 顯示標題。 |
| 135 | `if (!string.IsNullOrEmpty(section.text))` | 段落有本文才繪製。 |
| 137 | `GUILayout.Label(section.text, BodyStyle);` | 用 BodyStyle 顯示本文。 |
| 140 | `if (!string.IsNullOrEmpty(section.linkText))` | 有連結文字時才建立連結。 |
| 142 | `if (LinkLabel(new GUIContent(section.linkText)))` | 建立 GUIContent 並檢查連結按鈕是否被點擊。 |
| 144 | `Application.OpenURL(section.url);` | 交給系統開啟該段存的 URL。 |
| 148 | `GUILayout.Space(k_Space);` | 各段之間加固定空間。 |
| 151 | `if (GUILayout.Button("Remove Readme Assets", ButtonStyle))` | 建立移除教學資產按鈕並檢查點擊。 |
| 153 | `RemoveTutorial();` | 按下才呼叫移除流程，該流程另外跳出確認對話框。 |

## 介面樣式

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 157 | `bool m_Initialized;` | 保存樣式是否已初始化。 |
| 159 | `GUIStyle LinkStyle` | 宣告供這個類別讀取連結樣式的屬性。 |
| 161 | `get { return m_LinkStyle; }` | 回傳對應的樣式欄位。 |
| 164 | `[SerializeField]` | 讓 Unity 序列化下一行的樣式欄位。 |
| 165 | `GUIStyle m_LinkStyle;` | 保存連結樣式的實體。 |
| 167 | `GUIStyle TitleStyle` | 宣告標題樣式屬性。 |
| 169 | `get { return m_TitleStyle; }` | 回傳標題樣式。 |
| 172 | `[SerializeField]` | 允許 Unity 序列化下一行欄位。 |
| 173 | `GUIStyle m_TitleStyle;` | 保存主標題樣式。 |
| 175 | `GUIStyle HeadingStyle` | 宣告段落標題樣式屬性。 |
| 177 | `get { return m_HeadingStyle; }` | 回傳段落標題樣式。 |
| 180 | `[SerializeField]` | 允許 Unity 序列化下一行欄位。 |
| 181 | `GUIStyle m_HeadingStyle;` | 保存段落標題樣式。 |
| 183 | `GUIStyle BodyStyle` | 宣告本文樣式屬性。 |
| 185 | `get { return m_BodyStyle; }` | 回傳本文樣式。 |
| 188 | `[SerializeField]` | 允許 Unity 序列化下一行欄位。 |
| 189 | `GUIStyle m_BodyStyle;` | 保存本文樣式。 |
| 191 | `GUIStyle ButtonStyle` | 宣告按鈕樣式屬性。 |
| 193 | `get { return m_ButtonStyle; }` | 回傳按鈕樣式。 |
| 196 | `[SerializeField]` | 允許 Unity 序列化下一行欄位。 |
| 197 | `GUIStyle m_ButtonStyle;` | 保存移除按鈕樣式。 |

## 初始化樣式

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 199 | `void Init()` | 定義延遲建立所有 GUI 樣式的函式。 |
| 201 | `if (m_Initialized)` | 若已初始化直接跳過。 |
| 202 | `return;` | 回到呼叫端，避免重複配置樣式。 |
| 203 | `m_BodyStyle = new GUIStyle(EditorStyles.label);` | 複製 Unity 的標籤樣式當本文基礎。 |
| 204 | `m_BodyStyle.wordWrap = true;` | 允許本文依寬度自動換行。 |
| 205 | `m_BodyStyle.fontSize = 14;` | 設定本文字級為 14。 |
| 206 | `m_BodyStyle.richText = true;` | 允許解譯富文字標記。 |
| 208 | `m_TitleStyle = new GUIStyle(m_BodyStyle);` | 複製本文樣式製作主標題。 |
| 209 | `m_TitleStyle.fontSize = 26;` | 加大主標題字級到 26。 |
| 211 | `m_HeadingStyle = new GUIStyle(m_BodyStyle);` | 複製本文樣式製作段落標題。 |
| 212 | `m_HeadingStyle.fontStyle = FontStyle.Bold;` | 將段落標題套粗體。 |
| 213 | `m_HeadingStyle.fontSize = 18;` | 設定段落標題字級為 18。 |
| 215 | `m_LinkStyle = new GUIStyle(m_BodyStyle);` | 複製本文樣式製作連結文字。 |
| 216 | `m_LinkStyle.wordWrap = false;` | 關閉連結的自動換行。 |
| 218 | `// Match selection color which works nicely for both light and dark skins` | 原註解說明連結色是為了深色和淺色 Editor 主題都清楚。 |
| 219 | `m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);` | 用 0x00、0x78、0xDA 除以 255 轉成 Unity 的 0 到 1 RGB，Alpha 為 1。 |
| 220 | `m_LinkStyle.stretchWidth = false;` | 讓連結控件維持所需寬度。 |
| 222 | `m_ButtonStyle = new GUIStyle(EditorStyles.miniButton);` | 以 Unity 的迷你按鈕樣式建立本按鈕樣式。 |
| 223 | `m_ButtonStyle.fontStyle = FontStyle.Bold;` | 套用粗體文字。 |
| 225 | `m_Initialized = true;` | 設定初始化完成旗標。 |

## 可點擊連結

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 228 | `bool LinkLabel(GUIContent label, params GUILayoutOption[] options)` | 定義呈現可點擊連結的函式，params 接收零到多個排版選項。 |
| 230 | `var position = GUILayoutUtility.GetRect(label, LinkStyle, options);` | 依內容、樣式和排版選項取得畫面上的矩形。 |
| 232 | `Handles.BeginGUI();` | 進入 GUI 座標系繪製輔助線。 |
| 233 | `Handles.color = LinkStyle.normal.textColor;` | 把線色設為連結文字色。 |
| 234 | `Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));` | 沿矩形底邊畫一條線當底線。 |
| 235 | `Handles.color = Color.white;` | 將 Handles 顏色設回白色。 |
| 236 | `Handles.EndGUI();` | 結束 GUI 繪製區塊。 |
| 238 | `EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);` | 讓游標移入矩形時顯示連結手勢。 |
| 240 | `return GUI.Button(position, label, LinkStyle);` | 在矩形內建立按鈕，將這一幀是否點擊回傳。 |
