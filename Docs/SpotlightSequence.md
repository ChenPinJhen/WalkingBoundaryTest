# SpotlightSequence.cs

掛在 SampleScene 的 VR Walking Boundary，使用 CenterEyeAnchor 追蹤頭顯。清單順序為 Spot Light、Spot Light (1) 到 Spot Light (5)。其他光源由場景本身控制。

## 程式導讀

- 第 1–4 行匯入 Unity API，限制每個 GameObject 只能掛一個這個元件。
- 第 6–12 行保存頭顯、按順序排列的燈光清單、到達半徑和目前燈的索引。SerializeField 保留 private 欄位並開放給 Inspector。
- OnEnable 先把索引設成 0，驗證引用後才逐盞設定 Light.enabled。索引為 0 的燈開啟，其餘燈關閉。
- LateUpdate 每幀確認是否已到最後一盞。若尚未完成，從頭顯位置減去目前燈位置，只用世界座標的 X 與 Z 算水平距離平方。與到達半徑平方比較，因而不需要求平方根，也不受頭部高度影響。
- 進入半徑後，程式先關閉目前的燈，索引加 1，再開啟下一盞，讓來得太早的後續燈位置不會觸發。抵達最後一盞後保留照明。
- OnDrawGizmosSelected 在控制器被選取時，於控制器所在高度繪製每盞燈的觸發範圍預覽。

燈應朝下，讓地面光斑和 XZ 觸發位置對齊。要更換順序，直接在 Inspector 的 Spotlights 清單重新排列。若兩盞燈很近，應縮小 Arrival Radius 以避免兩個觸發區重疊。
