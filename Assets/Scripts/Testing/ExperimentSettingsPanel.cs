using System;
using TMPro;
using UnityEngine;

public class ExperimentSettingsPanel : MonoBehaviour
{
    [SerializeField] private AppManager appManager;
    [SerializeField] private TestLogger testLogger;
    [SerializeField] private TestTrigger testTrigger;

    [Header("Riepilogo nell'hub")]
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject refreshButton;

    [Header("Istruzioni sul carrello")]
    [SerializeField] private TMP_Text taskTitle;
    [SerializeField] private TMP_Text taskInstructions;

    private ExperimentSettings loadedSettings;
    private string lastCompletedTaskKey = "";
    private bool isLoading;

    private void Start()
    {
        if (summaryText != null)
        {
            summaryText.richText = false;
            summaryText.text = "Configurazione non caricata.";
        }

        if (taskInstructions != null)
        {
            taskInstructions.richText = false;
        }

        SetStatus("Completa il setup, poi premi Aggiorna configurazione.");
    }

    private void Update()
    {
        // Aggiorna solo la visibilità del pulsante, senza richieste al server.
        if (refreshButton != null)
        {
            bool canRefresh = !isLoading && testTrigger != null &&
                              testTrigger.CanLoadSettings;

            if (refreshButton.activeSelf != canRefresh)
            {
                refreshButton.SetActive(canRefresh);
            }
        }
    }

    public async void RefreshSettings()
    {
        if (isLoading || testTrigger == null ||
            !testTrigger.CanLoadSettings)
        {
            return;
        }

        if (appManager == null || appManager.apiService == null ||
            testLogger == null || summaryText == null ||
            taskInstructions == null)
        {
            SetStatus("Assegna tutti i riferimenti del pannello nell'Inspector.");
            return;
        }

        isLoading = true;
        testTrigger.HidePreparedTask();
        SetStatus("Lettura configurazione...");

        try
        {
            ExperimentSettings settings =
                await appManager.apiService.GetExperimentSettingsAsync();

            // Non applicare una lettura se nel frattempo l'utente è uscito.
            if (!testTrigger.CanLoadSettings)
            {
                SetStatus("Rientra nell'hub e premi nuovamente Aggiorna.");
                return;
            }

            if (settings == null)
            {
                SetStatus("Lettura fallita. Controlla server e connessione, poi ritenta.");
                return;
            }

            if (!settings.Validate(out string error))
            {
                SetStatus(error);
                return;
            }

            if (settings.TaskKey == lastCompletedTaskKey)
            {
                SetStatus("Questo task è già concluso. Aggiorna il numero del task nel database.");
                return;
            }

            if (!testLogger.ApplyExperimentSettings(
                    settings.participantId, settings.condition, settings.taskNumber))
            {
                SetStatus("Impossibile applicare la configurazione al logger.");
                return;
            }

            loadedSettings = settings;

            summaryText.text =
                $"Partecipante: {settings.participantId}\n" +
                $"Condizione: {settings.condition}\n" +
                $"Istanza: {settings.instance}\n" +
                $"Task: {settings.taskNumber}";

            if (taskTitle != null)
            {
                taskTitle.text = $"Task {settings.taskNumber}";
            }

            taskInstructions.text = settings.taskText;
            testTrigger.ShowPreparedTask();
            SetStatus("Task pronto. Controlla le istruzioni sul carrello.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            testTrigger.HidePreparedTask();
            SetStatus("Aggiornamento fallito. Controlla la Console e ritenta.");
        }
        finally
        {
            isLoading = false;
        }
    }

    public void NotifyTaskStarted()
    {
        SetStatus("Task in corso.");
    }

    public void NotifyTaskCompleted()
    {
        if (loadedSettings != null)
        {
            lastCompletedTaskKey = loadedSettings.TaskKey;
        }

        SetStatus("Ripristino in corso...");
    }

    public void NotifyResetFinished(bool success)
    {
        SetStatus(success
            ? "Reset completato. Aggiorna il database e premi Aggiorna configurazione."
            : "Reset fallito. Controlla la Console: la nuova prova resta bloccata.");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
