using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    [SerializeField] private TestLogger testLogger;
    [SerializeField] private Transform artifactsRoot;
    [SerializeField] private VirtualCart virtualCart;
    [SerializeField] private CartFollowToggle cartFollowToggle;
    [SerializeField] private TaskStateReset taskStateReset;
    [SerializeField] private GameObject taskPanel;
    [SerializeField] private ExperimentSettingsPanel settingsPanel;

    private bool playerInHub = true;
    private bool isStartingTask;
    private bool isRestoring;
    private bool resetSucceeded = true;
    private bool taskPrepared;

    private bool initialStateReady;

    public bool CanLoadSettings =>
        playerInHub && testLogger != null && !testLogger.IsTaskActive &&
        !isStartingTask && !isRestoring && resetSucceeded;

    private void Awake()
    {
        HidePreparedTask();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInHub = false;

        if (testLogger == null ||
            testLogger.IsTaskActive ||
            isStartingTask ||
            isRestoring ||
            !resetSucceeded ||
            !taskPrepared)
        {
            return;
        }

        if (!initialStateReady ||
            virtualCart == null ||
            taskPanel == null ||
            !taskPanel.activeInHierarchy)
        {
            Debug.LogError(
                "[TestTrigger] Situazione iniziale o task non pronti. " +
                "Premi Aggiorna nell'hub prima della prova.",
                this
            );

            return;
        }

        testLogger.StartConfiguredTask();

        if (!testLogger.IsTaskActive)
        {
            return;
        }

        // Registra soltanto i riferimenti per la valutazione finale.
        // Non scrive eventi di caricamento nel CSV.
        testLogger.RegisterPreloadedCartArtifacts(virtualCart);

        if (settingsPanel != null)
        {
            settingsPanel.NotifyTaskStarted();
        }
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

        // Mantiene i finali prima della chiusura e del ripristino.
        testLogger.EndCurrentTask();
        HidePreparedTask();

        if (settingsPanel != null)
        {
            settingsPanel.NotifyTaskCompleted();
        }

        isRestoring = true;
        resetSucceeded = false;

        try
        {
            resetSucceeded = await taskStateReset.RestoreInitialStateAsync();
        }
        finally
        {
            isRestoring = false;

            if (settingsPanel != null)
            {
                settingsPanel.NotifyResetFinished(resetSucceeded);
            }
        }
    }

    public void HidePreparedTask()
    {
        taskPrepared = false;

        if (taskPanel != null)
        {
            taskPanel.SetActive(false);
        }
    }

    public void ShowPreparedTask()
    {
        if (!CanLoadSettings ||
            !initialStateReady ||
            taskPanel == null)
        {
            return;
        }

        taskPrepared = true;
        taskPanel.SetActive(true);
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }

    public async System.Threading.Tasks.Task<bool> PrepareInitialStateAsync()
    {
        if (!CanLoadSettings)
        {
            return false;
        }

        // La situazione iniziale viene acquisita una sola volta.
        if (initialStateReady)
        {
            return true;
        }

        if (taskStateReset == null)
        {
            Debug.LogError(
                "[TestTrigger] Assegna Task State Reset.",
                this
            );

            return false;
        }

        isStartingTask = true;

        try
        {
            initialStateReady =
                await taskStateReset.InitializeAsync(
                    artifactsRoot,
                    virtualCart,
                    cartFollowToggle
                );

            return initialStateReady;
        }
        finally
        {
            isStartingTask = false;
        }
    }
}
