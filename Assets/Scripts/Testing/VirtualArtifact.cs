using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System;
using UnityEngine;

public class VirtualArtifact : MonoBehaviour
{
    public enum OperationType
    {
        None,
        Picking,
        Putaway,
        Return,
        Transfer
    }

    private enum OperationPhase
    {
        WaitingForCart,
        WaitingForPlacement,
        Completed
    }

    [Header("Artifact")]
    [SerializeField] private string artifactId;

    [Header("Operation")]
    [SerializeField]
    private OperationType operationType = OperationType.None;

    [Header("Placement Target")]
    [SerializeField]
    private VirtualPlacementTarget placementTarget;

    [Header("Logging")]
    [SerializeField]
    private TestLogger testLogger;

    private VirtualCart currentCart;
    private Transform originalParent;

    private OperationPhase currentPhase =
        OperationPhase.WaitingForCart;

    private bool hasStartedManipulation;
    private bool manipulationStartedInCart;

    private DateTime? firstManipulationStart;
    private DateTime? lastManipulationEnd;

    private Vector3 actualPlacementPosition;
    private bool placementEvaluated;
    private bool placementCorrect;

    private VirtualCart cartAtManipulationStart;

    private bool manipulationStartedDuringTask;
    private int manipulationTaskNumber;
    private string manipulationSessionId;

    public string ArtifactId => artifactId;
    public OperationType Operation => operationType;

    public VirtualPlacementTarget PlacementTarget =>
        placementTarget;

    public bool IsInCart => currentCart != null;
    public VirtualCart CurrentCart => currentCart;

    public DateTime? FirstManipulationStart =>
        firstManipulationStart;

    public DateTime? LastManipulationEnd =>
        lastManipulationEnd;

    public Vector3 ActualPlacementPosition =>
        actualPlacementPosition;

    public bool PlacementEvaluated =>
        placementEvaluated;

    public bool PlacementCorrect =>
        placementCorrect;

    private void Awake()
    {
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

        ResetOperationPhase();
    }

    public void ConfigureForTask(
        OperationType operation,
        VirtualPlacementTarget target)
    {
        operationType = operation;
        placementTarget = target;

        ResetTestData();

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"configured for {operationType}. " +
            $"Target = {GetExpectedTargetId()}."
        );
    }

    public void SetOperation(OperationType operation)
    {
        operationType = operation;

        ResetTestData();

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"operation = {operationType}."
        );
    }

    public void SetPlacementTarget(
        VirtualPlacementTarget target)
    {
        placementTarget = target;

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"placement target = {GetExpectedTargetId()}."
        );
    }

    public void BeginManipulation()
    {
        // Evita che un eventuale Begin duplicato
        // sovrascriva l'inizio della presa in corso.
        if (hasStartedManipulation)
            return;

        hasStartedManipulation = true;

        firstManipulationStart = DateTime.Now;
        lastManipulationEnd = null;

        cartAtManipulationStart = currentCart != null
            ? currentCart
            : FindCartContainingArtifact();

        manipulationStartedInCart =
            cartAtManipulationStart != null;

        manipulationStartedDuringTask =
            testLogger != null && testLogger.IsTaskActive;

        manipulationTaskNumber = manipulationStartedDuringTask
            ? testLogger.CurrentTask
            : -1;

        manipulationSessionId = manipulationStartedDuringTask
            ? testLogger.SessionId
            : null;

        if (currentCart != null)
        {
            DetachFromCart();
        }
    }
    public void EndManipulation()
    {
        if (!hasStartedManipulation ||
            !firstManipulationStart.HasValue)
        {
            return;
        }

        lastManipulationEnd = DateTime.Now;

        VirtualCart releaseCart =
            FindCartContainingArtifact();

        bool sameCart =
            cartAtManipulationStart != null &&
            releaseCart == cartAtManipulationStart;

        bool sameExperimentalTask =
            manipulationStartedDuringTask &&
            testLogger != null &&
            testLogger.IsTaskActive &&
            testLogger.CurrentTask == manipulationTaskNumber &&
            testLogger.SessionId == manipulationSessionId;

        // Una presa iniziata e finita nello stesso carrello
        // non genera né Manipulation né nuovi eventi operativi.
        if (sameCart)
        {
            releaseCart.AddArtifact(this);
            ResetAttemptTiming();
            return;
        }

        // Evita di attribuire una presa iniziata fuori dal test
        // o nel task precedente al task attualmente attivo.
        if (!sameExperimentalTask)
        {
            if (releaseCart != null)
            {
                releaseCart.AddArtifact(this);
            }

            ResetAttemptTiming();
            return;
        }

        testLogger.LogManipulation(
            operationType.ToString(),
            artifactId,
            firstManipulationStart.Value,
            lastManipulationEnd.Value,
            transform.position);

        if (releaseCart != null)
        {
            HandleReleaseInsideCart(releaseCart);
        }
        else
        {
            HandleReleaseOutsideCart();
        }

        // Ogni nuova presa avrà timestamp propri.
        ResetAttemptTiming();
    }

    private void HandleReleaseInsideCart(VirtualCart cart)
    {
        cart.AddArtifact(this);

        if (!firstManipulationStart.HasValue ||
            !lastManipulationEnd.HasValue)
        {
            return;
        }

        if (operationType == OperationType.None)
        {
            if (!manipulationStartedInCart)
            {
                testLogger?.LogWrongArtifactInCart(
                    artifactId,
                    firstManipulationStart.Value,
                    lastManipulationEnd.Value);
            }

            return;
        }

        // Evita duplicati di picking o caricamento.
        if (currentPhase == OperationPhase.Completed ||
            currentPhase == OperationPhase.WaitingForPlacement)
        {
            return;
        }

        if (operationType == OperationType.Picking)
        {
            testLogger?.LogArtifactPicked(
                artifactId,
                firstManipulationStart.Value,
                lastManipulationEnd.Value);

            currentPhase = OperationPhase.Completed;
            return;
        }

        if (operationType == OperationType.Putaway ||
            operationType == OperationType.Return ||
            operationType == OperationType.Transfer)
        {
            testLogger?.LogArtifactLoaded(
                operationType.ToString(),
                artifactId,
                firstManipulationStart.Value,
                lastManipulationEnd.Value);

            currentPhase = OperationPhase.WaitingForPlacement;
        }
    }

    private void HandleReleaseOutsideCart()
    {
        // La singola manipolazione è già stata registrata.
        // Un tentativo di deposito viene valutato soltanto
        // dopo il caricamento nel carrello.
        if (currentPhase != OperationPhase.WaitingForPlacement)
            return;

        if (!firstManipulationStart.HasValue ||
            !lastManipulationEnd.HasValue)
        {
            return;
        }

        actualPlacementPosition = transform.position;
        placementEvaluated = true;

        placementCorrect =
            placementTarget != null &&
            placementTarget.Contains(actualPlacementPosition);

        // Se il target atteso contiene il reperto, lo usiamo
        // anche come target effettivo, evitando incoerenze
        // in presenza di volumi sovrapposti.
        VirtualPlacementTarget actualTarget = placementCorrect
            ? placementTarget
            : FindTargetContainingArtifact();

        string actualTargetId = actualTarget != null
            ? actualTarget.TargetId
            : string.Empty;

        testLogger?.LogPlacementAttempt(
            operationType.ToString(),
            artifactId,
            GetExpectedTargetId(),
            actualTargetId,
            actualPlacementPosition,
            firstManipulationStart.Value,
            lastManipulationEnd.Value,
            placementCorrect);

        if (placementCorrect)
        {
            currentPhase = OperationPhase.Completed;
        }
    }
    private VirtualCart FindCartContainingArtifact()
    {
        VirtualCart[] carts =
            FindObjectsByType<VirtualCart>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (VirtualCart cart in carts)
        {
            if (cart.IsArtifactInsideTrigger(this))
            {
                return cart;
            }
        }

        return null;
    }

    private VirtualPlacementTarget
        FindTargetContainingArtifact()
    {
        VirtualPlacementTarget[] targets =
            FindObjectsByType<VirtualPlacementTarget>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        foreach (VirtualPlacementTarget target in targets)
        {
            if (target.Contains(transform.position))
            {
                return target;
            }
        }

        return null;
    }

    public void AttachToCart(VirtualCart cart)
    {
        if (cart == null)
        {
            Debug.LogWarning(
                $"[VirtualArtifact] {artifactId}: " +
                "cannot attach to a null cart."
            );

            return;
        }

        if (currentCart == cart)
        {
            return;
        }

        if (currentCart != null &&
            currentCart != cart)
        {
            currentCart.RemoveArtifact(this);
        }

        currentCart = cart;

        transform.SetParent(
            cart.transform,
            true
        );

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            "attached to cart."
        );
    }

    public void DetachFromWarehouse()
    {
        // Non tocchiamo i reperti spenti, anche tramite un parent.
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        // Alla successiva uscita dal carrello deve tornare alla radice.
        originalParent = null;

        if (currentCart != null)
        {
            return;
        }

        // Conserviamo le dimensioni globali prima del distacco.
        Vector3 worldScale = transform.lossyScale;

        transform.SetParent(null, true);

        // Senza parent, scala locale e globale coincidono.
        transform.localScale = worldScale;

        ObjectManipulator manipulator =
            GetComponent<ObjectManipulator>();

        if (manipulator == null)
        {
            return;
        }

        // I reperti devono soltanto muoversi e ruotare.
        manipulator.AllowedManipulations =
            TransformFlags.Move | TransformFlags.Rotate;

        // MRTK applica i vincoli di scala anche se Scale non è consentito.
        // Disabilitiamo solo quei vincoli, non il GameObject.
        TransformConstraint[] constraints =
            manipulator.GetComponents<TransformConstraint>();

        foreach (TransformConstraint constraint in constraints)
        {
            if ((constraint.ConstraintType & TransformFlags.Scale) != 0)
            {
                constraint.enabled = false;
            }
        }

        // Aggiorniamo il riferimento dei vincoli rimasti dopo il distacco.
        ConstraintManager manager = manipulator.ConstraintsManager;

        if (manager != null)
        {
            Transform host = manipulator.HostTransform;

            manager.Setup(
                new MixedRealityTransform(
                    host.position,
                    host.rotation,
                    host.localScale
                )
            );
        }
    }
    public void DetachFromCart()
    {
        if (currentCart == null)
        {
            return;
        }

        VirtualCart previousCart = currentCart;
        currentCart = null;

        previousCart.RemoveArtifact(this);

        transform.SetParent(
            originalParent,
            true
        );

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            "detached from cart."
        );
    }

    public void ResetTestData()
    {
        if (currentCart != null)
        {
            DetachFromCart();
        }

        ResetAttemptTiming();

        actualPlacementPosition = Vector3.zero;
        placementEvaluated = false;
        placementCorrect = false;

        ResetOperationPhase();

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            "test data reset."
        );
    }

    private void ResetOperationPhase()
    {
        currentPhase =
            operationType == OperationType.None
                ? OperationPhase.Completed
                : OperationPhase.WaitingForCart;
    }

    private void ResetAttemptTiming()
    {
        hasStartedManipulation = false;
        manipulationStartedInCart = false;

        firstManipulationStart = null;
        lastManipulationEnd = null;

        cartAtManipulationStart = null;

        manipulationStartedDuringTask = false;
        manipulationTaskNumber = -1;
        manipulationSessionId = null;
    }
    private string GetExpectedTargetId()
    {
        return placementTarget != null
            ? placementTarget.TargetId
            : string.Empty;
    }
}