using UnityEngine;
using UnityEngine.Events;

public class TestTrigger : MonoBehaviour
{
    [SerializeField] private TestLogger testLogger;
    [SerializeField] private Transform artifactsRoot;
    [SerializeField] private VirtualCart virtualCart;
    [SerializeField] private CartFollowToggle cartFollowToggle;
    [SerializeField] private TaskStateReset taskStateReset;

    [Header("Pannello con le istruzioni del task sul carrello")]
    [SerializeField] private GameObject taskPanel;

    [Header("Preparazione del task successivo")]
    [SerializeField] private UnityEvent onTaskPreparationRequested = new();

    private bool playerInHub = true;

    private async void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInHub = false;

        if (testLogger == null || testLogger.IsTaskActive)
        {
            return;
        }

        if (taskStateReset == null || taskPanel == null)
        {
            Debug.LogError("[TestTrigger] Assegna reset e pannello.", this);
            return;
        }

        // Il pannello visibile è il segnale che il task è pronto.
        if (!taskPanel.activeInHierarchy)
        {
            return;
        }

        // Salva lo stato soltanto alla prima uscita.
        // Nei task successivi InitializeAsync restituisce subito true.
        bool success = await taskStateReset.InitializeAsync(
            artifactsRoot, virtualCart, cartFollowToggle);

        if (!success || playerInHub || testLogger.IsTaskActive ||
            !taskPanel.activeInHierarchy)
        {
            return;
        }

        testLogger.StartNextTask();
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInHub = true;

        if (testLogger == null || !testLogger.IsTaskActive)
        {
            return;
        }

        // Prima registra lo stato finale raggiunto dall'utente.
        testLogger.EndCurrentTask();

        // Il task precedente non deve sembrare il nuovo task pronto.
        taskPanel.SetActive(false);

        bool success = await taskStateReset.RestoreInitialStateAsync();

        if (!success)
        {
            return;
        }

        // Qui collegheremo il caricamento del task dal database.
        // Il pannello resta nascosto finché il caricatore non chiama
        // ShowPreparedTask().
        onTaskPreparationRequested.Invoke();
    }

    // Chiamare dopo aver impostato davvero le istruzioni, le operazioni
    // e i target del task successivo.
    public void ShowPreparedTask()
    {
        if (testLogger == null || testLogger.IsTaskActive || taskPanel == null)
        {
            return;
        }

        taskPanel.SetActive(true);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }
}
