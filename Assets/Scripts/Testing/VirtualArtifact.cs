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
        /*
         * Salva se il reperto si trovava già nel carrello
         * all'inizio della manipolazione.
         */
        manipulationStartedInCart = currentCart != null;

        if (manipulationStartedInCart)
        {
            DetachFromCart();
        }

        if (!hasStartedManipulation)
        {
            hasStartedManipulation = true;
            firstManipulationStart = DateTime.Now;

            string formattedTime =
                firstManipulationStart.Value.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff"
                );

            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                $"first manipulation started at {formattedTime}."
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

    public void EndManipulation()
    {
        lastManipulationEnd = DateTime.Now;

        string formattedTime =
            lastManipulationEnd.Value.ToString(
                "yyyy-MM-dd HH:mm:ss.fff"
            );

        Debug.Log(
            $"[VirtualArtifact] {artifactId}: " +
            $"manipulation ended at {formattedTime}."
        );

        VirtualCart cart = FindCartContainingArtifact();

        if (cart != null)
        {
            HandleReleaseInsideCart(cart);
        }
        else
        {
            HandleReleaseOutsideCart();
        }
    }

    private void HandleReleaseInsideCart(
        VirtualCart cart)
    {
        cart.AddArtifact(this);

        /*
         * OperationType.None identifica un reperto non previsto
         * dal task.
         *
         * L'errore viene registrato soltanto quando il reperto
         * entra nel carrello partendo dall'esterno.
         */
        if (operationType == OperationType.None)
        {
            if (!manipulationStartedInCart)
            {
                if (firstManipulationStart.HasValue &&
                    lastManipulationEnd.HasValue)
                {
                    testLogger?.LogWrongArtifactInCart(
                        artifactId,
                        firstManipulationStart.Value,
                        lastManipulationEnd.Value
                    );
                }

                Debug.LogWarning(
                    $"[VirtualArtifact] {artifactId}: " +
                    "wrong artifact placed in cart."
                );
            }
            else
            {
                Debug.Log(
                    $"[VirtualArtifact] {artifactId}: " +
                    "wrong artifact repositioned inside cart. " +
                    "No new error logged."
                );
            }

            ResetAttemptTiming();
            return;
        }

        /*
         * Non registra altri eventi se l'operazione
         * è già stata completata.
         */
        if (currentPhase == OperationPhase.Completed)
        {
            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                "operation already completed. " +
                "No new event logged."
            );

            ResetAttemptTiming();
            return;
        }

        /*
         * Il reperto è già stato caricato e sta aspettando
         * il posizionamento finale.
         *
         * Rimetterlo o spostarlo nel carrello non genera
         * un nuovo evento ArtifactLoaded.
         */
        if (currentPhase ==
            OperationPhase.WaitingForPlacement)
        {
            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                "artifact returned or repositioned in cart. " +
                "No new loading event logged."
            );

            manipulationStartedInCart = false;
            return;
        }

        if (!firstManipulationStart.HasValue ||
            !lastManipulationEnd.HasValue)
        {
            Debug.LogWarning(
                $"[VirtualArtifact] {artifactId}: " +
                "missing manipulation timestamps."
            );

            return;
        }

        if (operationType == OperationType.Picking)
        {
            testLogger?.LogArtifactPicked(
                artifactId,
                firstManipulationStart.Value,
                lastManipulationEnd.Value
            );

            currentPhase = OperationPhase.Completed;

            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                "picking completed."
            );

            ResetAttemptTiming();
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
                lastManipulationEnd.Value
            );

            currentPhase =
                OperationPhase.WaitingForPlacement;

            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                $"loaded into cart for {operationType}. " +
                "Waiting for final placement."
            );

            /*
             * Non viene azzerato firstManipulationStart:
             * il posizionamento finale conserverà il tempo
             * della prima manipolazione.
             */
            manipulationStartedInCart = false;
        }
    }

    private void HandleReleaseOutsideCart()
    {
        /*
         * Se il reperto non sta aspettando un posizionamento,
         * il rilascio fuori dal carrello non è un evento.
         *
         * Questo vale anche per un reperto sbagliato che viene
         * manipolato ma non inserito nel carrello.
         */
        if (currentPhase !=
            OperationPhase.WaitingForPlacement)
        {
            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                "released outside cart. No event logged."
            );

            ResetAttemptTiming();
            return;
        }

        if (!firstManipulationStart.HasValue ||
            !lastManipulationEnd.HasValue)
        {
            Debug.LogWarning(
                $"[VirtualArtifact] {artifactId}: " +
                "cannot evaluate placement because " +
                "manipulation timestamps are missing."
            );

            return;
        }

        actualPlacementPosition = transform.position;
        placementEvaluated = true;

        VirtualPlacementTarget actualTarget =
            FindTargetContainingArtifact();

        placementCorrect =
            placementTarget != null &&
            placementTarget.Contains(
                actualPlacementPosition
            );

        string expectedTargetId =
            GetExpectedTargetId();

        string actualTargetId =
            actualTarget != null
                ? actualTarget.TargetId
                : string.Empty;

        testLogger?.LogPlacementAttempt(
            operationType.ToString(),
            artifactId,
            expectedTargetId,
            actualTargetId,
            actualPlacementPosition,
            firstManipulationStart.Value,
            lastManipulationEnd.Value,
            placementCorrect
        );

        if (placementCorrect)
        {
            currentPhase = OperationPhase.Completed;

            Debug.Log(
                $"[VirtualArtifact] {artifactId}: " +
                $"correctly placed in target " +
                $"{expectedTargetId}."
            );

            ResetAttemptTiming();
        }
        else
        {
            Debug.LogWarning(
                $"[VirtualArtifact] {artifactId}: " +
                $"wrong placement. Expected = " +
                $"{expectedTargetId}, actual = " +
                $"{actualTargetId}."
            );

            /*
             * Rimane in WaitingForPlacement, in modo che
             * l'utente possa riprovare.
             */
            manipulationStartedInCart = false;
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
    }

    private string GetExpectedTargetId()
    {
        return placementTarget != null
            ? placementTarget.TargetId
            : string.Empty;
    }
}