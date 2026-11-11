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
    public enum Route { Circle, Straight }

    [Header("路線與模型")]
    [SerializeField] private Route route;
    [Tooltip("指定 XR 頭顯追蹤點，例如 Meta Camera Rig 的 CenterEyeAnchor。")]
    [SerializeField] private Transform headset;
    [SerializeField] private GameObject footsteps;
    [Tooltip("拖入網格 GLB 或 Prefab。使用第一個有 MeshRenderer 的網格面。")]
    [SerializeField] private GameObject gridModel;

    [Header("起始行走範圍（公尺）")]
    [Min(0.5f), SerializeField] private float circleDiameter = 5f;
    [Min(0.5f), SerializeField] private float straightLength = 7f;
    [Min(0.5f), SerializeField] private float straightWidth = 2f;

    [Header("收縮與顯示（公尺）")]
    [Tooltip("每圈縮掉的半徑，或每次折返時直線兩端各往中點移動的距離。")]
    [Min(0f), SerializeField] private float shrinkStep = 0.25f;
    [Tooltip("圓圈的最小直徑，或直線的最小全長。")]
    [Min(0.5f), SerializeField] private float minimumSize = 1f;
    [Min(0f), SerializeField] private float revealDistance = 0.6f;
    [Min(0f), SerializeField] private float shrinkSeconds = 0.65f;
    [Min(0.1f), SerializeField] private float gridHeight = 2f;

    [Header("腳步")]
    [Min(0.2f), SerializeField] private float footstepSpacing = 0.55f;
    [Min(0f), SerializeField] private float pathInset = 0.25f;
    [Min(0.05f), SerializeField] private float arrivalTolerance = 0.2f;
    [Min(0.05f), SerializeField] private float circlePathTolerance = 0.35f;
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

    private const float FullTurn = 360f;
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
    private float transitionStart;
    private float transitionTime;
    private float previousAngle;
    private float walkedAngle;
    private bool lapStarted;
    private bool waitingAtStart;
    private int direction;
    private Route activeRoute;

    private void OnEnable()
    {
        if (headset == null || footsteps == null || gridModel == null)
        {
            Debug.LogError("WalkingBoundary 需要頭顯、腳步模型和有 MeshRenderer 的網格面。", this);
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
        gridRoot.gameObject.SetActive(false);
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
        extent = targetExtent = StartingExtent;
        direction = 1;
        waitingAtStart = true;
        lapStarted = false;
        walkedAngle = 0f;
        UpdateLayout();
    }

    private float StartingExtent => Mathf.Max(0.25f, (route == Route.Circle ? circleDiameter : straightLength) * 0.5f);
    private float HalfWidth => Mathf.Max(0.25f, straightWidth * 0.5f);
    private float PathExtent => GetPathExtent(extent);
    private float GetPathExtent(float boundaryExtent) =>
        boundaryExtent - Mathf.Clamp(pathInset, 0f, boundaryExtent * 0.45f);
    private float Tolerance => Mathf.Clamp(arrivalTolerance, 0.05f, PathExtent * 0.45f);

    private void LateUpdate()
    {
        if (route != activeRoute)
            ResetRoute();

        Vector3 local = transform.InverseTransformPoint(headset.position);
        Vector2 position = new(local.x, local.z);

        if (extent != targetExtent)
        {
            transitionTime += Time.deltaTime;
            float fraction = shrinkSeconds > 0f ? Mathf.Clamp01(transitionTime / shrinkSeconds) : 1f;
            extent = Mathf.Lerp(transitionStart, targetExtent, Mathf.SmoothStep(0f, 1f, fraction));
            UpdateLayout();
        }
        else if (route == Route.Circle)
        {
            TrackCircle(position);
        }
        else
        {
            TrackStraight(position);
        }

        float distance = route == Route.Circle
            ? extent - position.magnitude
            : Mathf.Min(HalfWidth - Mathf.Abs(position.x), extent - Mathf.Abs(position.y));
        bool showGrid = distance <= revealDistance;
        if (gridRoot.gameObject.activeSelf != showGrid)
            gridRoot.gameObject.SetActive(showGrid);
    }

    private void TrackCircle(Vector2 position)
    {
        float angle = Mathf.Atan2(position.x, position.y) * Mathf.Rad2Deg;
        float tolerance = Mathf.Clamp(circlePathTolerance, 0.05f, PathExtent * 0.65f);
        bool onPath = Mathf.Abs(position.magnitude - PathExtent) <= tolerance;
        if (!onPath)
        {
            lapStarted = false;
            return;
        }

        if (!lapStarted)
        {
            lapStarted = true;
            previousAngle = angle;
            walkedAngle = 0f;
            return;
        }

        float delta = Mathf.DeltaAngle(previousAngle, angle);
        previousAngle = angle;
        if (Mathf.Abs(delta) > 35f)
        {
            lapStarted = false;
            return;
        }

        walkedAngle += delta;
        if (walkedAngle >= FullTurn - 0.01f)
        {
            lapStarted = false;
            BeginShrink();
        }
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
            BeginShrink();
            UpdateLayout();
        }
    }

    private void BeginShrink()
    {
        float minimumExtent = Mathf.Clamp(minimumSize * 0.5f, 0.25f, extent);
        targetExtent = Mathf.Max(minimumExtent, extent - Mathf.Max(0f, shrinkStep));
        transitionStart = extent;
        transitionTime = 0f;
        if (shrinkSeconds <= 0f)
        {
            extent = targetExtent;
            UpdateLayout();
        }
    }

    private void UpdateLayout()
    {
        var (stepCount, panelCount) = Layout(extent, direction, PlaceStep, PlacePanel);
        HideUnused(steps, stepCount);
        HideUnused(panels, panelCount);
    }

    private (int steps, int panels) Layout(float extent, int direction,
        Action<int, Vector3, float> placeStep, Action<int, Vector3, Vector3> placePanel)
    {
        float pathExtent = GetPathExtent(extent);
        int stepCount;
        int panelCount;
        if (route == Route.Circle)
        {
            float circumference = 2f * Mathf.PI * pathExtent;
            stepCount = Mathf.Clamp(Mathf.RoundToInt(circumference / Mathf.Max(0.2f, footstepSpacing)), 4, 256);
            panelCount = Mathf.Clamp(Mathf.CeilToInt(2f * Mathf.PI * extent / 0.8f), 16, 64);
            for (int i = 0; i < stepCount; i++)
            {
                float angle = FullTurn * i / stepCount;
                placeStep(i, CirclePoint(pathExtent, angle), angle + 90f);
            }
            for (int i = 0; i < panelCount; i++)
            {
                float angle = FullTurn * i / panelCount;
                placePanel(i, CirclePoint(extent, angle), CirclePoint(extent, angle + FullTurn / panelCount));
            }
        }
        else
        {
            stepCount = Mathf.Clamp(Mathf.RoundToInt(2f * pathExtent / Mathf.Max(0.2f, footstepSpacing)) + 1, 2, 256);
            for (int i = 0; i < stepCount; i++)
            {
                float z = Mathf.Lerp(-pathExtent, pathExtent, (float)i / (stepCount - 1)) * direction;
                placeStep(i, new Vector3(0f, 0f, z), direction > 0 ? 0f : 180f);
            }

            int sideCount = Mathf.Clamp(Mathf.CeilToInt(2f * extent / 2.5f), 1, 32);
            for (int i = 0; i < sideCount; i++)
            {
                float from = Mathf.Lerp(-extent, extent, (float)i / sideCount);
                float to = Mathf.Lerp(-extent, extent, (float)(i + 1) / sideCount);
                placePanel(2 * i, new Vector3(-HalfWidth, 0f, from), new Vector3(-HalfWidth, 0f, to));
                placePanel(2 * i + 1, new Vector3(HalfWidth, 0f, to), new Vector3(HalfWidth, 0f, from));
            }
            placePanel(2 * sideCount, new Vector3(HalfWidth, 0f, -extent), new Vector3(-HalfWidth, 0f, -extent));
            placePanel(2 * sideCount + 1, new Vector3(-HalfWidth, 0f, extent), new Vector3(HalfWidth, 0f, extent));
            panelCount = 2 * sideCount + 2;
        }
        return (stepCount, panelCount);
    }

    private static Vector3 CirclePoint(float radius, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radians) * radius, 0f, Mathf.Cos(radians) * radius);
    }

    private void PlaceStep(int index, Vector3 position, float yaw)
    {
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
        instance.gameObject.SetActive(true);
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

        Matrix4x4 world = transform.localToWorldMatrix;
        MeshFilter[] stepMeshes = footsteps != null ? footsteps.GetComponentsInChildren<MeshFilter>() : null;
        Mesh grid = WalkingGridPreview.FindPanel(gridModel);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Color oldColor = Gizmos.color;
        Layout(StartingExtent, 1,
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        float radius = Application.isPlaying ? extent : StartingExtent;
        if (route == Route.Straight)
        {
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f * HalfWidth, 0f, 2f * radius));
            float start = radius - Mathf.Clamp(pathInset, 0f, radius * 0.45f);
            Gizmos.DrawWireSphere(Vector3.back * start, 0.12f);
            return;
        }

        const int segments = 64;
        for (int i = 0; i < segments; i++)
            Gizmos.DrawLine(CirclePoint(radius, FullTurn * i / segments),
                CirclePoint(radius, FullTurn * (i + 1) / segments));
    }
}
