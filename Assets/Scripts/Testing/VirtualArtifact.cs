using System;
using UnityEngine;

public class VirtualArtifact : MonoBehaviour
{
    public enum OperationType
    {
        None,
        Picking,
        Putaway
    }

    [Header("Artifact")]
    [SerializeField] private string artifactId;

    [Header("Putaway Target")]
    //[SerializeField] private Vector3 targetPosition;
    [SerializeField] private Vector3 targetLocalPosition;

    [SerializeField] private float placementTolerance = 0.20f;

    [Header("Operation")]
    [SerializeField] private OperationType operationType = OperationType.None;

    [SerializeField] private TestLogger testLogger;

    private VirtualCart currentCart;

    // Original parent of the artifact in the scene.
    private Transform originalParent;

    private bool hasStartedManipulation = false;

    private DateTime? firstManipulationStart;
    private DateTime? lastManipulationEnd;

    private Vector3 actualPlacementPosition;

    private bool placementEvaluated;
    private bool placementCorrect;

    // Original local scale of the artifact.
    //private Vector3 initialLocalScale;


    public string ArtifactId => artifactId;

    //public Vector3 TargetPosition => targetPosition;
    public Vector3 TargetPosition =>
    originalParent != null
        ? originalParent.TransformPoint(targetLocalPosition)
        : targetLocalPosition;

    public float PlacementTolerance => placementTolerance;

    public OperationType Operation => operationType;

    public bool IsInCart => currentCart != null;

    public VirtualCart CurrentCart => currentCart;

    public DateTime? FirstManipulationStart => firstManipulationStart;

    public DateTime? LastManipulationEnd => lastManipulationEnd;

    public Vector3 ActualPlacementPosition => actualPlacementPosition;

    public bool PlacementEvaluated => placementEvaluated;

    public bool PlacementCorrect => placementCorrect;


    private void Awake()
    {
        //initialLocalScale = transform.localScale;

        // Save the original parent.
        originalParent = transform.parent;

        if (testLogger == null)
        {
            testLogger = FindFirstObjectByType<TestLogger>();

            if (testLogger == null)
            {
                Debug.LogError(
                    $"[VirtualArtifact] {artifactId}: " +
                    "TestLogger not found."
                );
            }
        }
    }


    /// <summary>
    /// Defines whether the artifact is currently used for
    /// picking or putaway.
    /// </summary>
    //public void SetOperation(OperationType operation)
    //{
    //    operationType = operation;

    //    Debug.Log(
    //        $"[VirtualArtifact] {artifactId}: " +
    //        $"operation = {operation}"
    //    );
    //}


    /// <summary>
    /// Called by MRTK3 ObjectManipulator when manipulation starts.
    /// The first start timestamp is preserved.
    /// </summary>
    public void BeginManipulation()
    {
        if (currentCart != null)
        {
            if (operationType == OperationType.Putaway)
            {
                // Picking the artifact back out of the cart
                // is needed only for Putaway.
                DetachFromCart();
            }
            else
            {
                Debug.Log(
                    $"[VirtualArtifact] {artifactId}: " +
                    "artifact is already in cart. " +
                    "Ignoring manipulation for Picking."
                );

                return;
            }
        }

        if (!hasStartedManipulation)
        {
            hasStartedManipulation = true;

            firstManipulationStart = DateTime.Now;

            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                $"FIRST manipulation start = " +
                $"{firstManipulationStart.Value:yyyy-MM-dd HH:mm:ss.fff}"
            );
        }
        else
        {
            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                "manipulation started again."
            );
        }
    }


    /// <summary>
    /// Called by MRTK3 ObjectManipulator when manipulation ends.
    /// The timestamp is always updated.
    /// </summary>
    public void EndManipulation()
    {
        lastManipulationEnd = DateTime.Now;

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"manipulation end = " +
            $"{lastManipulationEnd.Value:yyyy-MM-dd HH:mm:ss.fff}"
        );

        VirtualCart[] carts = FindObjectsByType<VirtualCart>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (VirtualCart cart in carts)
        {
            if (cart.IsArtifactInsideTrigger(this))
            {
                cart.AddArtifact(this);

                // Picking is completed when the artifact
                // is released inside the cart.
                if (operationType == OperationType.Picking &&
                    firstManipulationStart.HasValue &&
                    lastManipulationEnd.HasValue)
                {
                    if (testLogger != null)
                    {
                        testLogger.LogArtifactPicked(
                            artifactId,
                            firstManipulationStart.Value,
                            lastManipulationEnd.Value
                        );
                    }
                }

                Debug.Log(
                    $"[VirtualArtifact] {artifactId}: " +
                    $"released inside cart during {operationType}."
                );

                return;
            }
        }

        // Released outside the cart.
        // For Putaway this means the final placement.
        if (operationType == OperationType.Putaway)
        {
            EvaluatePlacement();

            if (testLogger != null &&
                firstManipulationStart.HasValue &&
                lastManipulationEnd.HasValue)
            {
                testLogger.LogArtifactPutaway(
                    artifactId,
                    actualPlacementPosition,
                    firstManipulationStart.Value,
                    lastManipulationEnd.Value,
                    GetPlacementDistance(),
                    placementCorrect
                );
            }
        }
    }


    /// <summary>
    /// Makes the artifact a child of the cart while preserving
    /// its exact world position, rotation and scale.
    /// </summary>
    public void AttachToCart(VirtualCart cart)
    {
        if (cart == null)
        {
            Debug.LogWarning(
                $"[VirtualArtifact] {artifactId}: " +
                "cannot attach to null cart."
            );

            return;
        }

        if (currentCart == cart)
            return;

        // Save current world transform BEFORE changing parent.
        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;
        Vector3 worldScale = transform.lossyScale;

        currentCart = cart;

        // Parent to the cart while preserving world position/rotation.
        transform.SetParent(
            cart.transform,
            true
        );

        // Restore exact world position and rotation.
        transform.position = worldPosition;
        transform.rotation = worldRotation;

        // Restore scale explicitly
        //transform.localScale = initialLocalScale;

        ///*
        // * Compensate for the cart's scale so that the artifact
        // * keeps the same world size after becoming its child.
        // */
        //Vector3 parentLossyScale = cart.transform.lossyScale;

        //if (Mathf.Abs(parentLossyScale.x) > Mathf.Epsilon &&
        //    Mathf.Abs(parentLossyScale.y) > Mathf.Epsilon &&
        //    Mathf.Abs(parentLossyScale.z) > Mathf.Epsilon)
        //{
        //    transform.localScale = new Vector3(
        //        worldScale.x / parentLossyScale.x,
        //        worldScale.y / parentLossyScale.y,
        //        worldScale.z / parentLossyScale.z
        //    );
        //}

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"attached to cart {cart.name}."
        );
    }


    /// <summary>
    /// Removes the artifact from the cart and restores its
    /// original parent while preserving its world transform.
    /// </summary>
    public void DetachFromCart()
    {
        if (currentCart == null)
            return;

        VirtualCart previousCart = currentCart;

        // Save current world transform BEFORE changing parent.
        Vector3 worldPosition = transform.position;
        Quaternion worldRotation = transform.rotation;
        Vector3 worldScale = transform.lossyScale;

        previousCart.RemoveArtifact(this);

        // Return to the original parent (Magazzino).
        transform.SetParent(
            originalParent,
            true
        );

        // Restore exact world position and rotation.
        transform.position = worldPosition;
        transform.rotation = worldRotation;

        // Restore scale explicitly
        //transform.localScale = initialLocalScale;

        /*
        // * Compensate for the original parent's scale so the artifact
        // * keeps the same world size after leaving the cart.
        // */
        //if (originalParent != null)
        //{
        //    Vector3 parentLossyScale =
        //        originalParent.lossyScale;

        //    if (Mathf.Abs(parentLossyScale.x) > Mathf.Epsilon &&
        //        Mathf.Abs(parentLossyScale.y) > Mathf.Epsilon &&
        //        Mathf.Abs(parentLossyScale.z) > Mathf.Epsilon)
        //    {
        //        transform.localScale = new Vector3(
        //            worldScale.x / parentLossyScale.x,
        //            worldScale.y / parentLossyScale.y,
        //            worldScale.z / parentLossyScale.z
        //        );
        //    }
        //}
        //else
        //{
        //    // Safety case: artifact has no original parent.
        //    transform.localScale = worldScale;
        //}

        currentCart = null;

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"detached from cart {previousCart.name}."
        );
    }

    public bool IsInsideAnyCart()
    {
        VirtualCart[] carts = FindObjectsByType<VirtualCart>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (VirtualCart cart in carts)
        {
            if (cart.IsArtifactInsideTrigger(this))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Evaluates the final putaway position.
    /// </summary>
    //public void EvaluatePlacement()
    //{
    //    actualPlacementPosition = transform.position;

    //    float distance = Vector3.Distance(
    //        actualPlacementPosition,
    //        targetPosition
    //    );

    //    placementEvaluated = true;

    //    placementCorrect =
    //        distance <= placementTolerance;

    //    Debug.Log(
    //        $"[VirtualArtifact] {artifactId}: " +
    //        $"placement distance = {distance:F4} m, " +
    //        $"tolerance = {placementTolerance:F4} m, " +
    //        $"correct = {placementCorrect}"
    //    );
    //}

    public void EvaluatePlacement()
    {
        actualPlacementPosition = transform.position;

        Vector3 targetWorldPosition = TargetPosition;

        float distance = Vector3.Distance(
            actualPlacementPosition,
            targetWorldPosition
        );

        placementEvaluated = true;
        placementCorrect =
            distance <= placementTolerance;

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"actual = {actualPlacementPosition}, " +
            $"target = {targetWorldPosition}, " +
            $"distance = {distance:F4} m, " +
            $"tolerance = {placementTolerance:F4} m, " +
            $"correct = {placementCorrect}"
        );
    }

    //public float GetPlacementDistance()
    //{
    //    return Vector3.Distance(
    //        transform.position,
    //        targetPosition
    //    );
    //}

    public float GetPlacementDistance()
    {
        return Vector3.Distance(
            transform.position,
            TargetPosition
        );
    }

    /// <summary>
    /// Clears experimental data.
    /// </summary>
    public void ResetTestData()
    {
        hasStartedManipulation = false;

        firstManipulationStart = null;
        lastManipulationEnd = null;

        actualPlacementPosition = Vector3.zero;

        placementEvaluated = false;
        placementCorrect = false;
    }
}