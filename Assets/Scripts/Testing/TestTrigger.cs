using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    [SerializeField] private TestLogger testLogger;

    [Header("Reperti da separare dal magazzino")]
    [SerializeField] private Transform artifactsRoot;

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
        if (!IsPlayer(other) || testLogger == null)
        {
            return;
        }

        // Alla prima uscita dal hub, prima di iniziare il task.
        if (!DetachArtifactsOnce())
        {
            return;
        }

        Debug.Log(
            $"[TestTrigger] Player EXITED start/end zone: {other.name}"
        );

        testLogger.StartNextTask();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        Debug.Log(
            $"[TestTrigger] Player ENTERED start/end zone: {other.name}"
        );

        if (testLogger != null)
        {
            testLogger.EndCurrentTask();
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