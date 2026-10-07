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
}