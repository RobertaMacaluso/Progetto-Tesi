using MixedReality.Toolkit.SpatialManipulation;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;

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

    [Header("Menu operativo dell'app")]
    [SerializeField] private GameObject operationalMenuRoot;

    [Header("Indicazioni del magazzino per istanza")]
    [SerializeField] private GameObject instanceAIndicators;

    [Header("Pannello reperti")]
    [SerializeField] private GameObject artifactPanel;

    private ExperimentSettings loadedSettings;
    private string lastCompletedTaskKey = "";
    private bool isLoading;
    private string displayedIndicatorInstance = "A";
    private List<(TMP_Text Text, string InstanceA, string InstanceB)> indicatorLabels;

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
            taskInstructions == null || operationalMenuRoot == null ||
            instanceAIndicators == null || artifactPanel == null)
        {
            SetStatus(
                "Assegna tutti i riferimenti del pannello nell'Inspector, " +
                "compresi Operational Menu Root e le indicazioni."
            );
            return;
        }

        isLoading = true;
        testTrigger.HidePreparedTask();
        SetStatus("Lettura configurazione...");

        try
        {
            ExperimentSettings settings =
                await appManager.apiService.GetExperimentSettingsAsync();

            if (!testTrigger.CanLoadSettings)
            {
                SetStatus(
                    "Rientra nell'hub e premi nuovamente Aggiorna."
                );
                return;
            }

            if (settings == null)
            {
                SetStatus(
                    "Lettura fallita. Controlla server e connessione, " +
                    "poi ritenta."
                );
                return;
            }

            if (!settings.Validate(out string error))
            {
                SetStatus(error);
                return;
            }

            if (settings.TaskKey == lastCompletedTaskKey)
            {
                SetStatus(
                    "Questo task è già concluso. " +
                    "Aggiorna il numero del task nel database."
                );
                return;
            }

            SetStatus("Preparazione della situazione iniziale...");

            bool initialStateReady =
                await testTrigger.PrepareInitialStateAsync();

            if (!initialStateReady)
            {
                SetStatus(
                    "Preparazione iniziale fallita. " +
                    "Completa il setup e controlla che il carrello sia vuoto, " +
                    "poi premi nuovamente Aggiorna."
                );

                return;
            }

            if (!testTrigger.CanLoadSettings)
            {
                SetStatus(
                    "Rientra nell'hub e premi nuovamente Aggiorna."
                );

                return;
            }

            if (!testLogger.ApplyExperimentSettings(
                    settings.participantId,
                    settings.condition,
                    settings.taskNumber))
            {
                SetStatus(
                    "Impossibile applicare la configurazione al logger."
                );
                return;
            }

            loadedSettings = settings;

            ApplyIndicatorInstance(settings.instance);
            instanceAIndicators.SetActive(true);

            bool assistanceEnabled =
                settings.condition == "HoloLens";

            operationalMenuRoot.SetActive(assistanceEnabled);

            //AttachArtifactPanelToCart();

            if (assistanceEnabled)
            {
                appManager.OpenExperimentCatalog();
            }

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

            SetStatus(
                "Task pronto. Controlla le istruzioni sul carrello."
            );
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);

            testTrigger.HidePreparedTask();

            SetStatus(
                "Aggiornamento fallito. Controlla la Console e ritenta."
            );
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ApplyIndicatorInstance(string instance)
    {
        if (instance == displayedIndicatorInstance)
        {
            return;
        }

        if (indicatorLabels == null)
        {
            var labels = new List<(TMP_Text Text, string InstanceA, string InstanceB)>();

            foreach (TMP_Text text in instanceAIndicators.GetComponentsInChildren<TMP_Text>(true))
            {
                Transform button = text.transform.parent?.parent?.parent;

                if (text.name != "Text" || button == null ||
                    (button.name != "Button_Icon" && button.name != "Button_Icon 2"))
                {
                    continue;
                }

                Transform canvas = button.parent;

                if (canvas == null || canvas.parent == null ||
                    (canvas.name != "Canvas" && canvas.name != "Dir_B"))
                {
                    continue;
                }

                string originalText = text.text;
                string convertedText = GetInstanceBIndicatorText(originalText, canvas.parent);
                labels.Add((text, originalText, convertedText));
            }

            indicatorLabels = labels;
        }

        foreach (var label in indicatorLabels)
        {
            label.Text.text = instance == "A" ? label.InstanceA : label.InstanceB;
        }

        displayedIndicatorInstance = instance;
    }

    private static string GetInstanceBIndicatorText(string originalText, Transform sign)
    {
        int separator = originalText.IndexOf(' ');
        string level = originalText.Substring(0, separator);
        string value = originalText.Substring(separator + 1);

        switch (level)
        {
            case "Zone":
                return "Zone " + GetInstanceBZone(value);
            case "Zones":
                string[] zones = Array.ConvertAll(
                    value.Split(','), zone => GetInstanceBZone(zone.Trim()));
                return "Zones " + string.Join(", ", zones);
            case "Aisle":
                return "Aisle " + (char)('A' + int.Parse(value) - 1);
            case "Bay":
                return IsDirectBay(sign)
                    ? "Bay " + (char)('A' + int.Parse(value) - 1)
                    : "Bay " + (value[0] - 'A' + 1);
            case "Bin":
                return IsDirectBay(sign.parent)
                    ? "Bin " + (value[0] - 'A' + 1)
                    : "Bin " + (char)('A' + int.Parse(value) - 1);
            default:
                throw new InvalidOperationException(
                    $"Indicazione non supportata: {originalText}");
        }
    }

    private static bool IsDirectBay(Transform bay)
    {
        return bay != null && bay.parent != null &&
               (bay.parent.name == "Zone A" || bay.parent.name == "Zone F");
    }

    private static string GetInstanceBZone(string zone)
    {
        return zone switch
        {
            "A" => "4",
            "B" => "6",
            "C" => "1",
            "D" => "5",
            "E" => "2",
            "F" => "3",
            _ => throw new InvalidOperationException($"Zona non supportata: {zone}")
        };
    }

    public void AttachArtifactPanelToCart(bool value)
    {
        Debug.Log("Attach");
        artifactPanel.GetComponent<Follow>().enabled = !value;
        artifactPanel.GetComponent<SolverHandler>().enabled = !value;

        artifactPanel.GetComponent<ParentConstraint>().enabled = value;
        artifactPanel.GetComponent<ParentConstraint>().constraintActive = value;
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
