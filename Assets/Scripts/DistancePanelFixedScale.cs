using UnityEngine;

[DefaultExecutionOrder(10000)]
public class DistancePanelFixedScale : MonoBehaviour
{
    [SerializeField]
    private Renderer indicatorRenderer;

    [SerializeField, Min(0.0001f)]
    private float panelWorldScale = 0.02f;

    private void LateUpdate()
    {
        // Interveniamo soltanto quando la freccia è nascosta.
        if (indicatorRenderer == null || indicatorRenderer.enabled)
            return;

        Transform parent = transform.parent;

        if (parent == null)
        {
            transform.localScale = Vector3.one * panelWorldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;

        // Evita divisioni per zero.
        if (Mathf.Abs(parentScale.x) < 0.00001f ||
            Mathf.Abs(parentScale.y) < 0.00001f ||
            Mathf.Abs(parentScale.z) < 0.00001f)
        {
            return;
        }

        // Compensa la scala ereditata dal genitore.
        transform.localScale = new Vector3(
            panelWorldScale / parentScale.x,
            panelWorldScale / parentScale.y,
            panelWorldScale / parentScale.z
        );
    }
}