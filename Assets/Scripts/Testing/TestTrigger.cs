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

    public bool CanLoadSettings =>
        playerInHub && testLogger != null && !testLogger.IsTaskActive &&
        !isStartingTask && !isRestoring && resetSucceeded;

    private void Awake()
    {
        HidePreparedTask();
    }

    private async void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        playerInHub = false;

        if (testLogger == null || testLogger.IsTaskActive ||
            isStartingTask || isRestoring || !resetSucceeded || !taskPrepared)
        {
            return;
        }

        if (taskStateReset == null || taskPanel == null ||
            !taskPanel.activeInHierarchy)
        {
            Debug.LogError("[TestTrigger] Assegna reset e pannello.", this);
            return;
        }

        isStartingTask = true;

        try
        {
            // Conserva l'acquisizione iniziale già verificata.
            bool success = await taskStateReset.InitializeAsync(
                artifactsRoot, virtualCart, cartFollowToggle);

            if (!success || playerInHub || testLogger.IsTaskActive ||
                !taskPrepared || !taskPanel.activeInHierarchy)
            {
                return;
            }

            testLogger.StartConfiguredTask();

            if (testLogger.IsTaskActive && settingsPanel != null)
            {
                settingsPanel.NotifyTaskStarted();
            }
        }
        finally
        {
            isStartingTask = false;
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
        if (!CanLoadSettings || taskPanel == null)
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
}
