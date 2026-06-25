using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawRoomOutline : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        // Configura il LineRenderer per MRTK3
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 16; // Bastano 16 punti per fare un cubo continuo
        lineRenderer.startWidth = 0.02f; // Spessore della linea (2 centimetri)
        lineRenderer.endWidth = 0.02f;
    }

    public void UpdateRoomDimensions(Vector3 dimensioni)
    {
        float x = dimensioni.x / 2f;
        float y = dimensioni.y / 2f;
        float z = dimensioni.z / 2f;

        // Definiamo i 16 punti per tracciare i 12 spigoli senza staccare la penna
        Vector3[] puntiCubo = new Vector3[]
        {
            new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z),
            new Vector3(-x, -y, -z), new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z),
            new Vector3(-x, y, z), new Vector3(-x, -y, z), new Vector3(-x, y, z), new Vector3(-x, y, -z),
            new Vector3(x, y, -z), new Vector3(x, y, z), new Vector3(x, -y, z), new Vector3(x, -y, -z)
        };

        lineRenderer.SetPositions(puntiCubo);
    }
}