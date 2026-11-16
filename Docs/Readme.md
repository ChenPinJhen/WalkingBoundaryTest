# Readme.cs 逐行說明

Unity 範本說明資產的資料結構。原始碼位於 [Readme.cs](../Assets/TutorialInfo/Scripts/Readme.cs)。

行號以目前專案原始碼為準。每列對應一個有內容的原始碼行。空白行用來分段，單獨的 `{` 與 `}` 分別開啟和結束區塊，所以略過。跨行運算式按原始碼順序逐行拆開。

## 教學頁如何顯示

Readme 是資產中的資料格式。ReadmeEditor 讀取同一資產的 title 與 sections，將文字排進 Inspector。它不參與頭顯位置、網格或收縮計算。

## 資料結構

| 行 | C# 原始碼 | 中文解釋 |
| ---: | --- | --- |
| 1 | `using System;` | 匯入 System，讓內部 Section 可以使用 Serializable。 |
| 2 | `using UnityEngine;` | 匯入 ScriptableObject 與 Texture2D。 |
| 4 | `public class Readme : ScriptableObject` | 定義可存為 Unity 資產的資料類別，供範本的說明頁讀取。 |
| 6 | `public Texture2D icon;` | 保存說明頁的圖示貼圖。 |
| 7 | `public string title;` | 保存說明頁標題。 |
| 8 | `public Section[] sections;` | 保存一組依順序顯示的內容段落。 |
| 9 | `public bool loadedLayout;` | 記錄是否曾經為這個說明資產載入教學版面。 |
| 11 | `[Serializable]` | 讓 Unity 能把 Section 欄位序列化為 Readme 資產的一部分。 |
| 12 | `public class Section` | 定義每一段資料的型別。 |
| 14 | `public string heading, text, linkText, url;` | 同時宣告段落標題、本文、連結文字和連結網址四個字串。 |
