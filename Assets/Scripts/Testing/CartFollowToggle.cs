using MixedReality.Toolkit;
using UnityEngine;

[DisallowMultipleComponent]
public class CartFollowToggle : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cartTransform;
    [SerializeField] private Transform userHead;
    [SerializeField] private StatefulInteractable followToggle;

    [Header("Follow settings")]
    [SerializeField, Min(0.1f)] private float forwardDistance = 0.9f;
    [SerializeField] private float horizontalOffset = 0f;
    [SerializeField, Min(0f)] private float smoothingTime = 0.15f;
    [SerializeField] private float rotationOffsetY = 0f;

    private bool wasFollowing;
    private float fixedHeight;
    private Vector3 lastForward = Vector3.forward;
    private bool initialPoseCaptured;
    private Transform initialParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialWorldPosition;
    private Quaternion initialWorldRotation;


    private void Awake()
    {
        if (cartTransform == null)
            cartTransform = transform;
    }

    private void Start()
    {
        if (followToggle == null)
        {
            Debug.LogError(
                "[CartFollowToggle] Assegna il bottone toggle.",
                this);

            enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (followToggle == null || cartTransform == null)
            return;

        if (!followToggle.IsToggled.Active)
        {
            wasFollowing = false;
            return;
        }

        if (userHead == null)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
                return;

            userHead = mainCamera.transform;
        }

        bool justAttached = !wasFollowing;

        if (justAttached)
        {
            fixedHeight = cartTransform.position.y;
            wasFollowing = true;
        }

        Vector3 forward =
            Vector3.ProjectOnPlane(userHead.forward, Vector3.up);

        if (forward.sqrMagnitude > 0.001f)
            lastForward = forward.normalized;

        Vector3 right =
            Vector3.Cross(Vector3.up, lastForward);

        Vector3 targetPosition =
            userHead.position +
            lastForward * forwardDistance +
            right * horizontalOffset;

        targetPosition.y = fixedHeight;

        Quaternion targetRotation =
            Quaternion.LookRotation(lastForward, Vector3.up) *
            Quaternion.Euler(0f, rotationOffsetY, 0f);

        float blend = justAttached || smoothingTime <= 0f
            ? 1f
            : 1f - Mathf.Exp(-Time.deltaTime / smoothingTime);

        cartTransform.SetPositionAndRotation(
            Vector3.Lerp(
                cartTransform.position,
                targetPosition,
                blend),
            Quaternion.Slerp(
                cartTransform.rotation,
                targetRotation,
                blend));
    }

    private void OnDisable()
    {
        wasFollowing = false;
    }

    public void CaptureInitialPose()
    {
        if (initialPoseCaptured)
        {
            return;
        }

        if (cartTransform == null)
        {
            cartTransform = transform;
        }

        initialParent = cartTransform.parent;
        initialLocalPosition = cartTransform.localPosition;
        initialLocalRotation = cartTransform.localRotation;
        initialWorldPosition = cartTransform.position;
        initialWorldRotation = cartTransform.rotation;

        initialPoseCaptured = true;
    }

    public void ResetForNextTask()
    {
        // Il toggle deve risultare OFF anche visivamente.
        if (followToggle != null)
        {
            followToggle.ForceSetToggled(false);
        }

        wasFollowing = false;

        if (!initialPoseCaptured)
        {
            Debug.LogError(
                "[CartFollowToggle] Posizione iniziale non salvata.",
                this
            );
            return;
        }

        Vector3 resetPosition = initialWorldPosition;
        Quaternion resetRotation = initialWorldRotation;

        // Se il carrello aveva un parent, rispettiamo il suo
        // allineamento attuale senza cambiare la gerarchia.
        if (initialParent != null)
        {
            resetPosition =
                initialParent.TransformPoint(initialLocalPosition);
            resetRotation =
                initialParent.rotation * initialLocalRotation;
        }

        cartTransform.SetPositionAndRotation(
            resetPosition,
            resetRotation
        );

        fixedHeight = resetPosition.y;

        Debug.Log("[CartFollowToggle] Carrello riportato all'inizio.");
    }
}