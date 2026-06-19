using Microsoft.MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

public class PanelPositionLock : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;

    private Follow follow;

    private Vector3 horizontalDirection;
    private float distance;
    private float verticalOffset;

    private bool positionLocked = false;
    private bool wasLockedBeforeManipulation = false;

    void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        follow = GetComponent<Follow>();
        if (follow == null)
            follow = GetComponentInChildren<Follow>(true);
    }

    // 🔒 LOCK ORBITALE
    public void EnablePositionLock()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (follow != null)
            follow.enabled = false;

        Transform cam = targetCamera.transform;

        Vector3 toPanel = transform.position - cam.position;

        horizontalDirection = new Vector3(toPanel.x, 0f, toPanel.z);

        if (horizontalDirection.sqrMagnitude > 0.0001f)
            horizontalDirection.Normalize();

        distance = new Vector2(toPanel.x, toPanel.z).magnitude;

        // 🔥 FIX IMPORTANTE: salva offset verticale corretto
        verticalOffset = transform.position.y - cam.position.y;

        positionLocked = true;
    }

    // 🔓 FOLLOW MRTK
    public void EnableFollow()
    {
        positionLocked = false;

        if (follow != null)
            follow.enabled = true;
    }

    void LateUpdate()
    {
        if (!positionLocked || targetCamera == null)
            return;

        Transform cam = targetCamera.transform;

        Vector3 newPos =
            cam.position + horizontalDirection * distance;

        newPos.y = cam.position.y + verticalOffset;

        transform.position = newPos;

        // 🔥 ROTAZIONE (billboard completo)
        Vector3 dir = cam.position - transform.position;

        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // FIX mesh MRTK invertito
            transform.rotation = rot * Quaternion.Euler(0f, 180f, 0f);
        }
    }

    // 📌 STATO
    public bool IsPositionLocked()
    {
        return positionLocked;
    }

    // 🎮 MANIPULATION START
    public void OnManipulationStarted()
    {
        wasLockedBeforeManipulation = positionLocked;

        if (!positionLocked)
            return;

        EnableFollow();
    }

    // 🎮 MANIPULATION END
    public void OnManipulationEnded()
    {
        if (!wasLockedBeforeManipulation)
            return;

        EnablePositionLock();
    }
}