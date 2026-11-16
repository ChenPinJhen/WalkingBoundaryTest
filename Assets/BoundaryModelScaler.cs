using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class BoundaryModelScaler : MonoBehaviour
{
    [SerializeField] private WalkingBoundary boundary;
    [Tooltip("關閉時保留房間高度，只讓地面範圍跟著網格限縮。")]
    [SerializeField] private bool scaleHeight;

    private Transform scaleFrame;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private int originalSiblingIndex;

    private void LateUpdate()
    {
        if (boundary == null || !boundary.isActiveAndEnabled)
        {
            RestoreModel();
            return;
        }

        var area = boundary.CurrentArea;
        if (scaleFrame == null)
            AttachModel();

        Vector2 ratio = area.size / boundary.AuthoredAreaSize;
        scaleFrame.SetLocalPositionAndRotation(area.center, area.rotation);
        scaleFrame.localScale = new Vector3(ratio.x,
            scaleHeight ? Mathf.Min(ratio.x, ratio.y) : 1f, ratio.y);
    }

    private void AttachModel()
    {
        originalParent = transform.parent;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        originalSiblingIndex = transform.GetSiblingIndex();

        scaleFrame = new GameObject("Boundary Model Scale").transform;
        scaleFrame.SetParent(boundary.transform, false);
        transform.SetParent(scaleFrame, true);
    }

    private void OnDisable()
    {
        RestoreModel();
    }

    private void RestoreModel()
    {
        if (scaleFrame == null)
            return;

        Transform previousFrame = scaleFrame;
        scaleFrame = null;
        transform.SetParent(originalParent, false);
        transform.SetLocalPositionAndRotation(originalPosition, originalRotation);
        transform.localScale = originalScale;
        transform.SetSiblingIndex(originalSiblingIndex);
        Destroy(previousFrame.gameObject);
    }
}
