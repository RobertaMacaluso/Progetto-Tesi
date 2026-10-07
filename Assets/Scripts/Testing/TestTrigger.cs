using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    [SerializeField] private TestLogger testLogger;

    [Header("Reperti da separare dal magazzino")]
    [SerializeField] private Transform artifactsRoot;
    [Header("Reset carrello tra i task")]
    [SerializeField] private VirtualCart virtualCart;
    [SerializeField] private CartFollowToggle cartFollowToggle;

    private bool artifactsDetached;

    private void Awake()
    {
        if (testLogger == null)
        {
            Debug.LogError(
                "[TestTrigger] Assegna il TestLogger nell'Inspector.",
                this
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (testLogger == null || testLogger.IsTaskActive)
        {
            return;
        }

        if (virtualCart == null || cartFollowToggle == null)
        {
            Debug.LogError(
                "[TestTrigger] Assegna Virtual Cart e Cart Follow Toggle.",
                this
            );
            return;
        }

        if (!DetachArtifactsOnce())
        {
            return;
        }

        cartFollowToggle.CaptureInitialPose();

        Debug.Log(
            $"[TestTrigger] Player uscito dall'hub: {other.name}"
        );

        testLogger.StartNextTask();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        // L'ingresso iniziale nell'hub non è la fine di un task.
        // Evita anche reset ripetuti quando il task è già concluso.
        if (testLogger == null || !testLogger.IsTaskActive)
        {
            return;
        }

        Debug.Log(
            $"[TestTrigger] Player rientrato nell'hub: {other.name}"
        );

        // Prima: PlacementFinal e TaskCompleted, con i reperti
        // ancora nelle posizioni raggiunte durante il task.
        testLogger.EndCurrentTask();

        // Dopo: pulizia per la prova successiva.
        if (virtualCart != null)
        {
            virtualCart.ClearCartAndDeactivateArtifacts();
        }

        if (cartFollowToggle != null)
        {
            cartFollowToggle.ResetForNextTask();
        }
    }
    private bool DetachArtifactsOnce()
    {
        if (artifactsDetached)
        {
            return true;
        }

        if (artifactsRoot == null)
        {
            Debug.LogError(
                "[TestTrigger] Assegna l'empty dei reperti ad Artifacts Root.",
                this
            );

            return false;
        }

        // false: esclude i reperti sotto GameObject disattivati.
        VirtualArtifact[] artifacts =
            artifactsRoot.GetComponentsInChildren<VirtualArtifact>(false);

        int detachedCount = 0;

        foreach (VirtualArtifact artifact in artifacts)
        {
            // Protegge anche il caso dell'empty radice spento.
            if (!artifact.gameObject.activeInHierarchy)
            {
                continue;
            }

            artifact.DetachFromWarehouse();
            detachedCount++;
        }

        artifactsDetached = true;

        Debug.Log(
            $"[TestTrigger] Staccati dal magazzino {detachedCount} reperti attivi."
        );

        return true;
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }
}