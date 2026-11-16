using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.XR;
#endif

[DisallowMultipleComponent]
public sealed class WalkingBoundary : MonoBehaviour
{
    public enum Route { Square = 0, Straight = 1, Hybrid = 2 }

    [Header("路線與模型")]
    [SerializeField] private Route route;
    [Tooltip("指定 XR 頭顯追蹤點，例如 Meta Camera Rig 的 CenterEyeAnchor。")]
    [SerializeField] private Transform headset;
    [SerializeField] private GameObject footsteps;
    [Tooltip("拖入網格 GLB 或 Prefab。使用第一個有 MeshRenderer 的網格面。")]
    [SerializeField] private GameObject gridModel;

    [Header("起始行走範圍（公尺）")]
    [UnityEngine.Serialization.FormerlySerializedAs("circleDiameter")]
    [Min(1f), SerializeField] private float squareSize = 5f;
    [Min(0.5f), SerializeField] private float straightLength = 7f;
    [Min(0.5f), SerializeField] private float straightWidth = 2f;

    [Header("收縮與顯示（公尺）")]
    [Tooltip("方形每次觸發時各邊的內縮量，或直線每次折返時每端縮短的距離。")]
    [Min(0f), SerializeField] private float shrinkStep = 0.25f;
    [Tooltip("第二種直線路線的最小全長。方形和混合路線的最終區域是 1 × 1 公尺。")]
    [Min(0.5f), SerializeField] private float minimumSize = 1f;
    [Min(0f), SerializeField] private float revealDistance = 0.6f;
    [Min(0.1f), SerializeField] private float gridHeight = 2f;

    [Header("越界畫面")]
    [SerializeField] private bool solidColorOutside = true;
    [SerializeField] private Color outsideColor = Color.white;

    [SerializeField, HideInInspector] private bool hybridDiagonal;

    [Header("腳步")]
    [Min(0.2f), SerializeField] private float footstepSpacing = 0.55f;
    [Min(0f), SerializeField] private float pathInset = 0.25f;
    [Min(0.05f), SerializeField] private float arrivalTolerance = 0.2f;
    [SerializeField] private float footstepYaw;

#if UNITY_EDITOR
    [Header("電腦測試")]
    [Tooltip("在 Unity Editor 沒有啟用 XR 顯示時，以方向鍵模擬走動。")]
    [SerializeField] private bool simulateWithArrowKeys = true;
    [Min(0.1f), SerializeField] private float simulationSpeed = 1.5f;
    [SerializeField] private bool showScenePreview = true;
    private Transform simulatedRig;
    private Vector3 simulatedOffset;
#endif

    private readonly List<Transform> steps = new();
    private readonly List<Transform> panels = new();
    private Transform guideRoot;
    private Transform gridRoot;
    private Mesh panelMesh;
    private Material[] panelMaterials;
    private Quaternion panelRotation;
    private Vector3 panelCenter;
    private Vector3 panelSize;
    private float extent;
    private float targetExtent;
    private bool waitingAtStart;
    private bool activeHybridDiagonal;
    private bool outsideViewActive;
    private readonly Dictionary<Camera, (int mask, CameraClearFlags flags, Color color)> cameraViews = new();
    private int direction;
    private Route activeRoute;

    private void OnEnable()
    {
        if (headset == null || gridModel == null)
        {
            Debug.LogError("WalkingBoundary 需要頭顯和有 MeshRenderer 的網格面。", this);
            enabled = false;
            return;
        }

        foreach (MeshFilter filter in gridModel.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer renderer))
                continue;

            panelMesh = filter.sharedMesh;
            panelMaterials = renderer.sharedMaterials;
            break;
        }

        if (panelMesh == null)
        {
            Debug.LogError("WalkingBoundary 找不到可用的網格面。", this);
            enabled = false;
            return;
        }

        Bounds bounds = panelMesh.bounds;
        bool widthAlongZ = bounds.size.z > bounds.size.x;
        panelRotation = widthAlongZ ? Quaternion.Euler(0f, 90f, 0f) : Quaternion.identity;
        panelSize = widthAlongZ ? new Vector3(bounds.size.z, bounds.size.y, bounds.size.x) : bounds.size;
        panelCenter = panelRotation * bounds.center;
        if (panelSize.x < 0.001f || panelSize.y < 0.001f)
        {
            Debug.LogError("WalkingBoundary 的網格模型需要直立的網格面。", this);
            enabled = false;
            return;
        }

        guideRoot = CreateRoot("Footstep Guide");
        gridRoot = CreateRoot("Boundary Grid");
        OVRCameraRig cameraRig = headset.GetComponentInParent<OVRCameraRig>();
        Camera[] cameras = cameraRig != null ? cameraRig.GetComponentsInChildren<Camera>(true)
            : headset.GetComponentsInChildren<Camera>(true);
        foreach (Camera camera in cameras)
            cameraViews.Add(camera, (camera.cullingMask, camera.clearFlags, camera.backgroundColor));
        ResetRoute();
#if UNITY_EDITOR
        OVRCameraRig rig = headset.GetComponentInParent<OVRCameraRig>();
        simulatedRig = rig != null ? rig.transform : headset;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        ResetSimulatedOffset();
#endif
        SetOutsideView(false);
        cameraViews.Clear();
        ReleaseRoot(guideRoot);
        ReleaseRoot(gridRoot);
        steps.Clear();
        panels.Clear();
        panelMesh = null;
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (!simulateWithArrowKeys || XRSettings.isDeviceActive)
        {
            ResetSimulatedOffset();
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || simulatedRig == null)
            return;

        float x = (keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                  (keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float z = (keyboard.upArrowKey.isPressed ? 1f : 0f) -
                  (keyboard.downArrowKey.isPressed ? 1f : 0f);
        Vector3 input = new(x, 0f, z);
        if (input == Vector3.zero)
            return;

        Quaternion routeYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 movement = routeYaw * input.normalized * (simulationSpeed * Time.deltaTime);
        simulatedRig.position += movement;
        simulatedOffset += movement;
    }

    private void ResetSimulatedOffset()
    {
        if (simulatedRig != null && simulatedOffset != Vector3.zero)
            simulatedRig.position -= simulatedOffset;
        simulatedOffset = Vector3.zero;
    }
#endif

    private static void ReleaseRoot(Transform root)
    {
        if (root == null)
            return;

        root.gameObject.SetActive(false);
        Destroy(root.gameObject);
    }

    private Transform CreateRoot(string objectName)
    {
        Transform root = new GameObject(objectName).transform;
        root.SetParent(transform, false);
        return root;
    }

    private void ResetRoute()
    {
        activeRoute = route;
        activeHybridDiagonal = hybridDiagonal;
        extent = targetExtent = StartingExtent;
        direction = 1;
        waitingAtStart = true;
        UpdateLayout();
    }

    private bool IsSquare => route == Route.Square || route == Route.Hybrid && !hybridDiagonal;
    private float SquareHalf => Mathf.Max(0.5f, squareSize * 0.5f);
    private float StartingExtent => IsSquare ? SquareHalf : route == Route.Hybrid
        ? Mathf.Max(0.5f, (SquareHalf - 0.5f) * Mathf.Sqrt(2f)) : Mathf.Max(0.25f, straightLength * 0.5f);
    private float HalfWidth => GetHalfWidth(extent);
    private float GetHalfWidth(float halfLength) => IsSquare ? halfLength : route == Route.Hybrid
        ? Mathf.Min(halfLength, Mathf.Max(0.5f, straightWidth * 0.5f)) : Mathf.Max(0.25f, straightWidth * 0.5f);
    private float MinimumExtent => route == Route.Straight
        ? Mathf.Clamp(minimumSize * 0.5f, 0.25f, StartingExtent) : 0.5f;
    private Vector3 AreaCenter => GetAreaCenter(extent);
    private Vector3 GetAreaCenter(float halfLength) => route == Route.Hybrid && IsSquare
        ? new Vector3(halfLength - SquareHalf, 0f, SquareHalf - halfLength) : Vector3.zero;
    private Quaternion AreaRotation => route == Route.Hybrid && hybridDiagonal
        ? Quaternion.Euler(0f, 135f, 0f) : Quaternion.identity;
    private float PathExtent => extent - Mathf.Clamp(pathInset, 0f, extent * 0.45f);
    private float Tolerance => Mathf.Clamp(arrivalTolerance, 0.05f, PathExtent * 0.45f);

    public void SetHybridDiagonal(bool diagonal)
    {
        if (route != Route.Hybrid || hybridDiagonal == diagonal)
            return;
        hybridDiagonal = diagonal;
        if (Application.isPlaying && gridRoot != null)
            ResetRoute();
    }

    private void LateUpdate()
    {
        if (route != activeRoute || hybridDiagonal != activeHybridDiagonal)
            ResetRoute();

        Vector3 headPosition = transform.InverseTransformPoint(headset.position);
        Vector3 local = ToAreaPosition(headPosition, extent);
        if (extent != targetExtent)
        {
            if (IsClearOfGrid(local, extent) &&
                IsClearOfGrid(ToAreaPosition(headPosition, targetExtent), targetExtent))
            {
                // Both layouts are hidden from this position. Apply the new size before rendering.
                HideUnused(panels, 0);
                extent = targetExtent;
                UpdateLayout();
                local = ToAreaPosition(headPosition, extent);
            }
        }
        else if (route == Route.Hybrid && IsSquare && extent <= MinimumExtent)
        {
            SetHybridDiagonal(true);
            local = ToAreaPosition(headPosition, extent);
        }
        else if (IsSquare)
        {
            if (NearestEdgeDistance(local, extent) <= RevealThreshold(extent))
                QueueShrink();
        }
        else
        {
            TrackStraight(new Vector2(local.x, local.z));
        }

        UpdateVisibility(local);
    }

    private Vector3 ToAreaPosition(Vector3 position, float halfLength) =>
        Quaternion.Inverse(AreaRotation) * (position - GetAreaCenter(halfLength));

    private float NearestEdgeDistance(Vector3 position, float halfLength) =>
        Mathf.Min(GetHalfWidth(halfLength) - Mathf.Abs(position.x), halfLength - Mathf.Abs(position.z));

    private float RevealThreshold(float halfLength) =>
        Mathf.Min(revealDistance, 0.85f * Mathf.Min(GetHalfWidth(halfLength), halfLength));

    private bool IsClearOfGrid(Vector3 position, float halfLength) =>
        NearestEdgeDistance(position, halfLength) > RevealThreshold(halfLength) + 0.02f;

    private void UpdateVisibility(Vector3 position)
    {
        float left = HalfWidth + position.x;
        float right = HalfWidth - position.x;
        float near = extent + position.z;
        float far = extent - position.z;
        float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(near, far));
        float threshold = RevealThreshold(extent);
        int visible = -1;
        if (nearest <= threshold)
            visible = nearest == left ? 0 : nearest == right ? 1 : nearest == near ? 2 : 3;
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i].gameObject.activeSelf != (i == visible))
                panels[i].gameObject.SetActive(i == visible);
        }
        SetOutsideView(solidColorOutside && nearest < 0f);
    }

    private void SetOutsideView(bool outside)
    {
        foreach (var entry in cameraViews)
        {
            Camera camera = entry.Key;
            if (camera == null)
                continue;
            var original = entry.Value;
            if (outside)
            {
                camera.cullingMask = 0;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = outsideColor;
            }
            else if (outsideViewActive)
            {
                camera.cullingMask = original.mask;
                camera.clearFlags = original.flags;
                camera.backgroundColor = original.color;
            }
        }
        outsideViewActive = outside;
    }

    private void TrackStraight(Vector2 position)
    {
        if (Mathf.Abs(position.x) > HalfWidth)
            return;

        if (waitingAtStart)
        {
            if (position.y * direction <= -PathExtent + Tolerance)
                waitingAtStart = false;
            return;
        }

        if (position.y * direction >= PathExtent - Tolerance)
        {
            direction = -direction;
            QueueShrink();
            UpdateLayout();
        }
    }

    private void QueueShrink()
    {
        targetExtent = Mathf.Max(MinimumExtent, extent - Mathf.Max(0f, shrinkStep));
    }

    private void UpdateLayout()
    {
        guideRoot.SetLocalPositionAndRotation(AreaCenter, AreaRotation);
        gridRoot.SetLocalPositionAndRotation(AreaCenter, AreaRotation);
        int stepCount = Layout(extent, HalfWidth, IsSquare, direction, PlaceStep, PlacePanel);
        HideUnused(steps, footsteps != null ? stepCount : 0);
    }

    private int Layout(float length, float width, bool square, int heading,
        Action<int, Vector3, float> placeStep, Action<int, Vector3, Vector3> placePanel)
    {
        int stepCount = 0;
        if (!square)
        {
            float path = length - Mathf.Clamp(pathInset, 0f, length * 0.45f);
            stepCount = Mathf.Clamp(Mathf.RoundToInt(2f * path / Mathf.Max(0.2f, footstepSpacing)) + 1, 2, 256);
            for (int i = 0; i < stepCount; i++)
            {
                float z = Mathf.Lerp(-path, path, (float)i / (stepCount - 1)) * heading;
                placeStep(i, new Vector3(0f, 0f, z), heading > 0 ? 0f : 180f);
            }
        }

        placePanel(0, new Vector3(-width, 0f, -length), new Vector3(-width, 0f, length));
        placePanel(1, new Vector3(width, 0f, length), new Vector3(width, 0f, -length));
        placePanel(2, new Vector3(width, 0f, -length), new Vector3(-width, 0f, -length));
        placePanel(3, new Vector3(-width, 0f, length), new Vector3(width, 0f, length));
        return stepCount;
    }

    private void PlaceStep(int index, Vector3 position, float yaw)
    {
        if (footsteps == null)
            return;

        if (index == steps.Count)
            steps.Add(Instantiate(footsteps, guideRoot).transform);

        Transform step = steps[index];
        step.localPosition = position + Vector3.up * 0.015f;
        step.localRotation = Quaternion.Euler(0f, yaw + footstepYaw, 0f);
        step.gameObject.SetActive(true);
    }

    private void PlacePanel(int index, Vector3 start, Vector3 end)
    {
        if (index == panels.Count)
        {
            GameObject panel = new("Grid Panel");
            panel.transform.SetParent(gridRoot, false);
            panel.SetActive(false);
            GameObject mesh = new("Mesh");
            mesh.transform.SetParent(panel.transform, false);
            mesh.transform.localRotation = panelRotation;
            mesh.transform.localPosition = Vector3.up * (panelSize.y * 0.5f) - panelCenter;
            mesh.AddComponent<MeshFilter>().sharedMesh = panelMesh;
            MeshRenderer renderer = mesh.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = panelMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            panels.Add(panel.transform);
        }

        Transform instance = panels[index];
        Vector3 edge = end - start;
        instance.localPosition = (start + end) * 0.5f;
        instance.localRotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, edge));
        instance.localScale = new Vector3(edge.magnitude / panelSize.x, gridHeight / panelSize.y, 1f);

    }

    private static void HideUnused(List<Transform> objects, int used)
    {
        for (int i = used; i < objects.Count; i++)
            objects[i].gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying || !enabled || !showScenePreview)
            return;

        Matrix4x4 world = transform.localToWorldMatrix * Matrix4x4.Rotate(AreaRotation);
        MeshFilter[] stepMeshes = footsteps != null ? footsteps.GetComponentsInChildren<MeshFilter>() : null;
        Mesh grid = WalkingGridPreview.FindPanel(gridModel);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;
        float previewWidth = IsSquare ? StartingExtent : route == Route.Hybrid
            ? Mathf.Min(StartingExtent, Mathf.Max(0.5f, straightWidth * 0.5f)) : HalfWidth;
        Layout(StartingExtent, previewWidth, IsSquare, 1,
            (_, position, yaw) =>
            {
                if (stepMeshes == null)
                    return;

                Gizmos.color = new Color(1f, 0.75f, 0.2f);
                Matrix4x4 step = world * Matrix4x4.TRS(position + Vector3.up * 0.015f,
                    Quaternion.Euler(0f, yaw + footstepYaw, 0f), footsteps.transform.localScale);
                foreach (MeshFilter filter in stepMeshes)
                {
                    if (filter.sharedMesh == null)
                        continue;

                    Gizmos.matrix = step * footsteps.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    Gizmos.DrawMesh(filter.sharedMesh);
                }
            },
            (_, start, end) =>
            {
                if (grid == null)
                    return;

                Vector3 edge = end - start;
                Matrix4x4 panel = world * Matrix4x4.TRS((start + end) * 0.5f,
                    Quaternion.LookRotation(Vector3.Cross(Vector3.up, edge)), Vector3.one);
                WalkingGridPreview.DrawPanel(grid, panel, edge.magnitude, gridHeight, new Color(0.2f, 0.8f, 1f, 0.55f));
            });
        Gizmos.matrix = oldMatrix;
        Gizmos.color = oldColor;
    }
#endif

    // Ground dimensions are in boundary-local X and Z, before applying the area's rotation.
    public Vector2 AuthoredAreaSize => route == Route.Straight
        ? new Vector2(Mathf.Max(0.5f, straightWidth), Mathf.Max(0.5f, straightLength))
        : Vector2.one * (2f * SquareHalf);

    public (Vector3 center, Quaternion rotation, Vector2 size) CurrentArea =>
        (AreaCenter, AreaRotation, new Vector2(2f * HalfWidth, 2f * extent));
}
