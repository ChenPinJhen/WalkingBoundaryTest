using UnityEngine;

[DisallowMultipleComponent]
public sealed class SpotlightSequence : MonoBehaviour
{
    [SerializeField] private Transform headset;
    [Tooltip("依照要點亮的順序填入即時聚光燈。")]
    [SerializeField] private Light[] spotlights;
    [Tooltip("頭顯在地面上的投影距燈下方多近時，切換到下一盞。")]
    [Min(0.05f), SerializeField] private float arrivalRadius = 0.4f;

    private int currentIndex;

    private void OnEnable()
    {
        currentIndex = 0;
        if (headset == null || spotlights == null || spotlights.Length == 0)
        {
            Debug.LogWarning("Spotlight Sequence 需要指定 Headset 與至少一盞 Spot Light。", this);
            enabled = false;
            return;
        }

        foreach (Light spotlight in spotlights)
        {
            if (spotlight == null)
            {
                Debug.LogWarning("Spotlight Sequence 的燈光清單有空欄位。", this);
                enabled = false;
                return;
            }
        }

        for (int i = 0; i < spotlights.Length; i++)
            spotlights[i].enabled = i == currentIndex;
    }

    private void LateUpdate()
    {
        if (currentIndex >= spotlights.Length - 1)
            return;

        Light current = spotlights[currentIndex];
        Vector3 offset = headset.position - current.transform.position;
        float horizontalDistanceSquared = offset.x * offset.x + offset.z * offset.z;
        if (horizontalDistanceSquared > arrivalRadius * arrivalRadius)
            return;

        current.enabled = false;
        currentIndex++;
        spotlights[currentIndex].enabled = true;
    }

    private void OnDrawGizmosSelected()
    {
        if (spotlights == null)
            return;

        Gizmos.color = Color.yellow;
        foreach (Light spotlight in spotlights)
        {
            if (spotlight == null)
                continue;

            Vector3 center = spotlight.transform.position;
            center.y = transform.position.y;
            Gizmos.DrawWireSphere(center, arrivalRadius);
        }
    }
}
