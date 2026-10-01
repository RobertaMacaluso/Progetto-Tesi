using UnityEngine;

public class VirtualPlacementTarget : MonoBehaviour
{
    [SerializeField] private string targetId;
    [SerializeField] private BoxCollider targetCollider;

    public string TargetId => targetId;

    private void Awake()
    {
        if (targetCollider == null)
            targetCollider = GetComponent<BoxCollider>();

        if (targetCollider == null)
            Debug.LogError($"[VirtualPlacementTarget] {name}: BoxCollider mancante.");
    }

    public bool Contains(Vector3 worldPosition)
    {
        if (targetCollider == null)
            return false;

        Vector3 localPoint =
            targetCollider.transform.InverseTransformPoint(worldPosition)
            - targetCollider.center;

        Vector3 halfSize = targetCollider.size * 0.5f;

        return Mathf.Abs(localPoint.x) <= halfSize.x &&
               Mathf.Abs(localPoint.y) <= halfSize.y &&
               Mathf.Abs(localPoint.z) <= halfSize.z;
    }
}