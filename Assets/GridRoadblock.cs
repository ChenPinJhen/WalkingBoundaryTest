using UnityEngine;

[DisallowMultipleComponent]
public sealed class GridRoadblock : MonoBehaviour
{
    [Header("網格與觸發範圍")]
    [SerializeField] private GameObject gridModel;
    [Tooltip("可指定頭顯追蹤點。留空時優先使用 Meta Camera Rig 的 CenterEyeAnchor。")]
    [SerializeField] private Transform headsetOverride;
    [Min(0.1f), SerializeField] private float width = 1.1f;
    [Min(0.1f), SerializeField] private float height = 1.8f;
    [Tooltip("距網格正面或背面的觸發距離。頭顯也要在網格寬度內。")]
    [Min(0.05f), SerializeField] private float triggerDistance = 0.85f;

    [Header("觸發順序")]
    [Tooltip("指定場景中的前一個路障。它觸發後，觀眾走進這個路障的範圍才會顯示。")]
    [SerializeField] private GridRoadblock previousRoadblock;
    [Tooltip("這個路障觸發時，通知前一個路障開始倒下。")]
    [SerializeField] private bool releasePreviousOnTrigger;

    [Header("倒下與消失")]
    [Tooltip("從首次觸發到開始倒下的秒數。觀眾離開後仍會繼續計時。")]
    [Min(0f), SerializeField] private float fallDelay = 3f;
    [Tooltip("倒下動畫的秒數。倒平後立即消失。")]
    [Min(0f), SerializeField] private float fallDuration = 0.65f;
    [Tooltip("等待後一個路障觸發後才倒下。後一個路障須開啟 Release Previous On Trigger。")]
    [SerializeField] private bool waitForRelease;

#if UNITY_EDITOR
    [SerializeField] private bool showScenePreview = true;

    private void OnDrawGizmos()
    {
        if (Application.isPlaying || !enabled || !showScenePreview)
            return;

        Mesh mesh = WalkingGridPreview.FindPanel(gridModel);
        if (mesh == null)
            return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;
        WalkingGridPreview.DrawPanel(mesh, transform.localToWorldMatrix, width, height, new Color(1f, 0.55f, 0.2f, 0.8f));
        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }
#endif

    private Transform headset;
    private Transform pivot;
    private float triggeredAt;
    private float releasedAt;
    private float fallDirection;
    private int triggerFrame;
    private bool wasInsideWhileLocked;
    private bool triggered;
    private bool finished;

    private void OnEnable()
    {
        headset = headsetOverride;
        triggered = finished = false;
        wasInsideWhileLocked = false;
        releasedAt = float.PositiveInfinity;
        if (pivot != null)
        {
            pivot.localRotation = Quaternion.identity;
            pivot.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (pivot != null)
            pivot.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (finished)
            return;

        if (!triggered)
        {
            if (headset == null)
                headset = FindHeadset();
            if (headset == null)
                return;

            Vector3 localHead = transform.InverseTransformPoint(headset.position);
            bool inside = Mathf.Abs(localHead.x) <= width * 0.5f &&
                          Mathf.Abs(localHead.z) <= triggerDistance;
            if (previousRoadblock != null &&
                (!previousRoadblock.triggered || previousRoadblock.triggerFrame == Time.frameCount))
            {
                wasInsideWhileLocked = inside;
                return;
            }

            if (!inside)
            {
                wasInsideWhileLocked = false;
                return;
            }

            if (wasInsideWhileLocked)
                return;

            if (pivot == null && !CreateGrid())
            {
                Debug.LogError("GridRoadblock 需要一個含直立網格面的 GLB 或 Prefab。", this);
                finished = true;
                return;
            }

            // Topple away from the side the visitor approached.
            fallDirection = localHead.z > 0f ? -1f : 1f;
            triggeredAt = Time.time;
            triggerFrame = Time.frameCount;
            triggered = true;
            pivot.gameObject.SetActive(true);
            if (releasePreviousOnTrigger && previousRoadblock != null)
                previousRoadblock.releasedAt = Mathf.Min(previousRoadblock.releasedAt, triggeredAt);
        }

        float timedFallAt = waitForRelease ? float.PositiveInfinity : triggeredAt + Mathf.Max(0f, fallDelay);
        float fallTime = Time.time - Mathf.Min(timedFallAt, releasedAt);
        if (fallTime < 0f)
            return;

        float progress = fallDuration > 0f ? Mathf.Clamp01(fallTime / fallDuration) : 1f;
        pivot.localRotation = Quaternion.Euler(fallDirection * 90f * progress * progress, 0f, 0f);
        if (progress >= 1f)
        {
            pivot.gameObject.SetActive(false);
            finished = true;
        }
    }

    private static Transform FindHeadset()
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
            return rig.centerEyeAnchor;

        Camera main = Camera.main;
        return main != null ? main.transform : null;
    }

    private bool CreateGrid()
    {
        if (gridModel == null)
            return false;

        foreach (MeshFilter source in gridModel.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = source.sharedMesh;
            if (mesh == null || !source.TryGetComponent(out MeshRenderer sourceRenderer))
                continue;

            Bounds bounds = mesh.bounds;
            bool widthAlongZ = bounds.size.z > bounds.size.x;
            float meshWidth = widthAlongZ ? bounds.size.z : bounds.size.x;
            if (meshWidth < 0.001f || bounds.size.y < 0.001f)
                continue;

            pivot = new GameObject("Grid Fall Pivot").transform;
            pivot.SetParent(transform, false);
            pivot.gameObject.SetActive(false);

            Transform panel = new GameObject("Grid Panel").transform;
            panel.SetParent(pivot, false);
            panel.localScale = new Vector3(width / meshWidth, height / bounds.size.y, 1f);

            Transform model = new GameObject("Mesh").transform;
            model.SetParent(panel, false);
            model.localRotation = widthAlongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
            model.localPosition = Vector3.up * bounds.extents.y - model.localRotation * bounds.center;
            model.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            model.gameObject.AddComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Vector3.up * height * 0.5f, new Vector3(width, height, 0.03f));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, 0.02f, triggerDistance * 2f));
    }
}

#if UNITY_EDITOR
internal static class WalkingGridPreview
{
    internal static Mesh FindPanel(GameObject model)
    {
        if (model == null)
            return null;

        foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh != null && filter.TryGetComponent(out MeshRenderer _) &&
                mesh.bounds.size.y >= 0.001f && Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.z) >= 0.001f)
                return mesh;
        }
        return null;
    }

    internal static void DrawPanel(Mesh mesh, Matrix4x4 panel, float width, float height, Color color)
    {
        Bounds bounds = mesh.bounds;
        bool widthAlongZ = bounds.size.z > bounds.size.x;
        float meshWidth = widthAlongZ ? bounds.size.z : bounds.size.x;
        Quaternion rotation = widthAlongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        Vector3 position = Vector3.up * bounds.extents.y - rotation * bounds.center;
        Gizmos.matrix = panel * Matrix4x4.Scale(new Vector3(width / meshWidth, height / bounds.size.y, 1f))
            * Matrix4x4.TRS(position, rotation, Vector3.one);
        Gizmos.color = color;
        Gizmos.DrawMesh(mesh);
    }
}
#endif
