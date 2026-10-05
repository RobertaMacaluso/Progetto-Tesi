using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class TestLogger : MonoBehaviour
{
    public enum TestCondition
    {
        Traditional,
        HoloLens
    }

    [Header("Test settings")]
    [SerializeField]
    private TestCondition condition =
        TestCondition.HoloLens;

    [SerializeField]
    private int currentTask = 0;

    [Header("Participant")]
    [SerializeField]
    private bool assignParticipantAutomatically = true;

    [SerializeField]
    private string participantIdOverride = "P01";

    [Header("Position tracking")]
    [SerializeField]
    private Transform appManager;

    [SerializeField]
    private float trackingInterval = 0.1f;

    private string filePath;
    private StreamWriter writer;

    private Coroutine trackingCoroutine;
    private bool isTaskActive;

    private string appSequenceId = "";
    private int appSequenceTask = -1;

    private readonly List<TaskItem> loggedAppItems = new List<TaskItem>();

    public string ParticipantId { get; private set; }

    public string SessionId { get; private set; }

    public TestCondition Condition => condition;

    public int CurrentTask => currentTask;

    public bool IsTaskActive => isTaskActive;

    public string FilePath => filePath;

    private const string PlayerPrefsKey =
        "LastParticipantId";

    private void Awake()
    {
        if (trackingInterval <= 0f)
        {
            Debug.LogWarning(
                "[TestLogger] Tracking interval must be " +
                "greater than zero. Using 0.1 seconds."
            );

            trackingInterval = 0.1f;
        }

        if (appManager == null)
        {
            Debug.LogWarning(
                "[TestLogger] The Transform used for position " +
                "tracking has not been assigned."
            );
        }

        StartNewSession();
    }

    /// <summary>
    /// Creates a new logging session.
    /// </summary>
    public void StartNewSession()
    {
        isTaskActive = false;
        StopPositionTracking();
        CloseLog();

        appSequenceId = "";
        appSequenceTask = -1;
        loggedAppItems.Clear();

        ParticipantId = ResolveParticipantId();

        SessionId =
            $"{ParticipantId}_{condition}_" +
            $"{DateTime.Now:yyyyMMdd_HHmmss_fff}";

        string directory = Path.Combine(
            Application.persistentDataPath,
            "TestLogs");

        Directory.CreateDirectory(directory);

        filePath = Path.Combine(
            directory,
            $"{SessionId}.csv");

        FileStream fileStream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read);

        writer = new StreamWriter(fileStream)
        {
            AutoFlush = true
        };

        writer.WriteLine(
            "Timestamp;" +
            "ParticipantId;" +
            "Condition;" +
            "SessionId;" +
            "Task;" +
            "Event;" +
            "Operation;" +
            "ArtifactId;" +
            "ExpectedTargetId;" +
            "ActualTargetId;" +
            "X;" +
            "Y;" +
            "Z;" +
            "FirstManipulationStart;" +
            "LastManipulationEnd;" +
            "Correct;" +
            "AppSequenceId;" +
            "AppItemIndex;" +
            "AppPhase;" +
            "SourceContainerId;" +
            "DestinationContainerId;" +
            "TransferId");

        Debug.Log(
            $"[TestLogger] Session started: {SessionId}");

        Debug.Log(
            $"[TestLogger] Log file: {filePath}");
    }
    private string ResolveParticipantId()
    {
        if (!assignParticipantAutomatically)
        {
            string manualId =
                participantIdOverride != null
                    ? participantIdOverride.Trim()
                    : "";

            if (string.IsNullOrEmpty(manualId))
            {
                Debug.LogWarning(
                    "[TestLogger] Manual participant ID is empty. " +
                    "Using P00."
                );

                return "P00";
            }

            /*
             * Permette di inserire sia "3" sia "P03".
             */
            if (int.TryParse(
                manualId,
                out int numericId))
            {
                return $"P{numericId:D2}";
            }

            return manualId;
        }

        int lastParticipantId =
            PlayerPrefs.GetInt(
                PlayerPrefsKey,
                0
            );

        int newParticipantId =
            lastParticipantId + 1;

        PlayerPrefs.SetInt(
            PlayerPrefsKey,
            newParticipantId
        );

        PlayerPrefs.Save();

        return $"P{newParticipantId:D2}";
    }

    /// <summary>
    /// Sets the current task number.
    /// StartNextTask increments this value by one.
    /// </summary>
    public void SetTask(int taskNumber)
    {
        if (isTaskActive)
        {
            Debug.LogWarning(
                "[TestLogger] Cannot change the task number " +
                "while a task is active."
            );

            return;
        }

        currentTask =
            Mathf.Max(0, taskNumber);

        Debug.Log(
            $"[TestLogger] Current task set to {currentTask}."
        );
    }

    /// <summary>
    /// Sets the value used by the automatic participant counter.
    /// For example, passing 17 makes the next automatic ID P18.
    /// </summary>
    public void SetNextParticipantId(
        int participantId)
    {
        if (participantId < 0)
        {
            Debug.LogWarning(
                "[TestLogger] Participant ID cannot be negative."
            );

            return;
        }

        PlayerPrefs.SetInt(
            PlayerPrefsKey,
            participantId
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"[TestLogger] Last participant ID set to " +
            $"{participantId}. Next automatic ID: " +
            $"P{participantId + 1:D2}."
        );
    }

    public void ResetParticipantId()
    {
        PlayerPrefs.SetInt(
            PlayerPrefsKey,
            0
        );

        PlayerPrefs.Save();

        Debug.Log(
            "[TestLogger] Participant counter reset. " +
            "Next automatic ID: P01."
        );
    }

    public int GetCurrentParticipantNumber()
    {
        return PlayerPrefs.GetInt(
            PlayerPrefsKey,
            0
        );
    }

    /// <summary>
    /// Starts a task when the participant exits the hub.
    /// </summary>
    public void StartNextTask()
    {
        if (isTaskActive)
        {
            Debug.LogWarning(
                "[TestLogger] A task is already active. " +
                "StartNextTask ignored."
            );

            return;
        }

        currentTask++;
        isTaskActive = true;

        WriteEvent("TaskStarted");

        LogCurrentPosition();
        StartPositionTracking();

        Debug.Log(
            $"[TestLogger] Task {currentTask} started."
        );
    }

    /// <summary>
    /// Ends the task when the participant enters the hub.
    /// </summary>
    public void EndCurrentTask()
    {
        if (currentTask == 0)
        {
            Debug.LogWarning(
                "[TestLogger] EndCurrentTask called before " +
                "the first task was started."
            );

            return;
        }

        if (!isTaskActive)
        {
            Debug.LogWarning(
                $"[TestLogger] Task {currentTask} is not active."
            );

            return;
        }

        LogCurrentPosition();
        StopPositionTracking();

        WriteEvent("TaskCompleted");

        isTaskActive = false;

        Debug.Log(
            $"[TestLogger] Task {currentTask} completed."
        );
    }

    private void StartPositionTracking()
    {
        if (appManager == null)
        {
            Debug.LogWarning(
                "[TestLogger] Position tracking was not started " +
                "because the tracked Transform is missing."
            );

            return;
        }

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
        }

        trackingCoroutine =
            StartCoroutine(
                TrackPosition()
            );
    }

    private void StopPositionTracking()
    {
        if (trackingCoroutine == null)
        {
            return;
        }

        StopCoroutine(trackingCoroutine);
        trackingCoroutine = null;
    }

    private IEnumerator TrackPosition()
    {
        WaitForSeconds wait =
            new WaitForSeconds(trackingInterval);

        while (isTaskActive)
        {
            // La prima posizione viene già registrata
            // da StartNextTask().
            yield return wait;

            if (isTaskActive)
            {
                LogCurrentPosition();
            }
        }

        trackingCoroutine = null;
    }

    private void LogCurrentPosition()
    {
        if (appManager == null ||
            !isTaskActive)
        {
            return;
        }

        Vector3 position =
            appManager.position;

        WriteCsvLine(
            eventName: "Position",
            operation: "",
            artifactId: "",
            expectedTargetId: "",
            actualTargetId: "",
            x: position.x,
            y: position.y,
            z: position.z,
            firstManipulationStart: null,
            lastManipulationEnd: null,
            correct: null
        );
    }

    private void WriteEvent(
        string eventName)
    {
        WriteCsvLine(
            eventName: eventName,
            operation: "",
            artifactId: "",
            expectedTargetId: "",
            actualTargetId: "",
            x: null,
            y: null,
            z: null,
            firstManipulationStart: null,
            lastManipulationEnd: null,
            correct: null
        );
    }

    /// <summary>
    /// Records the completion of a picking operation.
    /// </summary>
    public void LogArtifactPicked(
        string artifactId,
        DateTime firstManipulationStart,
        DateTime lastManipulationEnd)
    {
        if (!CanLogArtifactEvent())
        {
            return;
        }

        WriteCsvLine(
            eventName: "ArtifactPicked",
            operation: "Picking",
            artifactId: artifactId,
            expectedTargetId: "Cart",
            actualTargetId: "Cart",
            x: null,
            y: null,
            z: null,
            firstManipulationStart:
                firstManipulationStart,
            lastManipulationEnd:
                lastManipulationEnd,
            correct: true
        );

        Debug.Log(
            $"[TestLogger] Picking completed: {artifactId}."
        );
    }

    /// <summary>
    /// Records a correct artifact loaded into the cart before
    /// put-away, return or transfer.
    /// </summary>
    public void LogArtifactLoaded(
        string operation,
        string artifactId,
        DateTime firstManipulationStart,
        DateTime lastManipulationEnd)
    {
        if (!CanLogArtifactEvent())
        {
            return;
        }

        WriteCsvLine(
            eventName: "ArtifactLoaded",
            operation: operation,
            artifactId: artifactId,
            expectedTargetId: "Cart",
            actualTargetId: "Cart",
            x: null,
            y: null,
            z: null,
            firstManipulationStart:
                firstManipulationStart,
            lastManipulationEnd:
                lastManipulationEnd,
            correct: true
        );

        Debug.Log(
            $"[TestLogger] Artifact {artifactId} loaded " +
            $"for {operation}."
        );
    }

    /// <summary>
    /// Records a distractor or other incorrect artifact
    /// released inside the cart.
    /// </summary>
    public void LogWrongArtifactInCart(
        string artifactId,
        DateTime firstManipulationStart,
        DateTime lastManipulationEnd)
    {
        if (!CanLogArtifactEvent())
        {
            return;
        }

        WriteCsvLine(
            eventName: "WrongArtifactInCart",
            operation: "None",
            artifactId: artifactId,
            expectedTargetId: "",
            actualTargetId: "Cart",
            x: null,
            y: null,
            z: null,
            firstManipulationStart:
                firstManipulationStart,
            lastManipulationEnd:
                lastManipulationEnd,
            correct: false
        );

        Debug.LogWarning(
            $"[TestLogger] Wrong artifact placed in cart: " +
            $"{artifactId}."
        );
    }

    /// <summary>
    /// Records a placement attempt for put-away,
    /// return or transfer.
    /// </summary>
    public void LogPlacementAttempt(
        string operation,
        string artifactId,
        string expectedTargetId,
        string actualTargetId,
        Vector3 actualPosition,
        DateTime firstManipulationStart,
        DateTime lastManipulationEnd,
        bool correct)
    {
        if (!CanLogArtifactEvent())
        {
            return;
        }

        WriteCsvLine(
            eventName: "PlacementAttempt",
            operation: operation,
            artifactId: artifactId,
            expectedTargetId: expectedTargetId,
            actualTargetId: actualTargetId,
            x: actualPosition.x,
            y: actualPosition.y,
            z: actualPosition.z,
            firstManipulationStart:
                firstManipulationStart,
            lastManipulationEnd:
                lastManipulationEnd,
            correct: correct
        );

        Debug.Log(
            $"[TestLogger] Placement attempt: " +
            $"operation = {operation}, " +
            $"artifact = {artifactId}, " +
            $"expected = {expectedTargetId}, " +
            $"actual = {actualTargetId}, " +
            $"correct = {correct}."
        );
    }

    public void LogManipulation(
    string operation,
    string artifactId,
    DateTime manipulationStart,
    DateTime manipulationEnd,
    Vector3 releasePosition)
    {
        if (!CanLogArtifactEvent())
            return;

        WriteCsvLine(
            eventName: "Manipulation",
            operation: operation,
            artifactId: artifactId,
            expectedTargetId: "",
            actualTargetId: "",
            x: releasePosition.x,
            y: releasePosition.y,
            z: releasePosition.z,
            firstManipulationStart: manipulationStart,
            lastManipulationEnd: manipulationEnd,
            correct: null);
    }

    public void LogAppSequence(WarehouseTask task)
    {
        if (condition != TestCondition.HoloLens ||
            !CanLogArtifactEvent() ||
            task == null)
        {
            return;
        }

        appSequenceId = Guid.NewGuid().ToString("N");
        appSequenceTask = currentTask;
        loggedAppItems.Clear();

        WriteCsvLine(
            eventName: "AppSequenceStarted",
            operation: "",
            artifactId: "",
            expectedTargetId: "",
            actualTargetId: "",
            x: null,
            y: null,
            z: null,
            firstManipulationStart: null,
            lastManipulationEnd: null,
            correct: null,
            sequenceId: appSequenceId);

        foreach (TaskItem item in task.TaskItems)
        {
            if (item == null ||
                item.Artifact == null ||
                item.Operation == TaskOperation.ReturnHub)
            {
                continue;
            }

            loggedAppItems.Add(item);

            WriteAppItem(
                "AppSequenceItem",
                item,
                appSequenceId,
                loggedAppItems.Count);
        }
    }

    public void LogAppConfirmation(TaskItem item)
    {
        if (condition != TestCondition.HoloLens ||
            !CanLogArtifactEvent() ||
            item == null ||
            item.Artifact == null ||
            item.Operation == TaskOperation.ReturnHub)
        {
            return;
        }

        string eventName;

        if (item.Operation == TaskOperation.Pick)
        {
            eventName = "AppPickConfirmed";
        }
        else if (item.Operation == TaskOperation.Deposit)
        {
            eventName = "AppDepositConfirmed";
        }
        else
        {
            return;
        }

        int index = appSequenceTask == currentTask
            ? loggedAppItems.IndexOf(item)
            : -1;

        // Conserva comunque la conferma se manca lo snapshot,
        // senza associarla erroneamente a una vecchia sequenza.
        string sequenceId = index >= 0
            ? appSequenceId
            : "";

        int? itemIndex = index >= 0
            ? (int?)(index + 1)
            : null;

        if (index < 0)
        {
            Debug.LogWarning(
                "[TestLogger] App confirmation without a matching " +
                "sequence snapshot.");
        }

        WriteAppItem(
            eventName,
            item,
            sequenceId,
            itemIndex);
    }

    private void WriteAppItem(
        string eventName,
        TaskItem item,
        string sequenceId,
        int? itemIndex)
    {
        WriteCsvLine(
            eventName: eventName,
            operation: item.ExperimentOperation.ToString(),
            artifactId: item.Artifact.id.ToString(
                CultureInfo.InvariantCulture),
            expectedTargetId: "",
            actualTargetId: "",
            x: null,
            y: null,
            z: null,
            firstManipulationStart: null,
            lastManipulationEnd: null,
            correct: null,
            sequenceId: sequenceId,
            itemIndex: itemIndex,
            appPhase: item.Operation.ToString(),
            sourceContainerId: item.SourceContainerId >= 0
                ? (int?)item.SourceContainerId
                : null,
            destinationContainerId: item.DestinationContainerId >= 0
                ? (int?)item.DestinationContainerId
                : null,
            transferId: item.TransferId);
    }

    private bool CanLogArtifactEvent()
    {
        if (writer == null)
        {
            Debug.LogWarning(
                "[TestLogger] Cannot record the event: " +
                "the log file is not open."
            );

            return false;
        }

        if (!isTaskActive)
        {
            Debug.Log(
                "[TestLogger] Artifact event ignored because " +
                "no experimental task is active."
            );

            return false;
        }

        return true;
    }

    private void WriteCsvLine(
        string eventName,
        string operation,
        string artifactId,
        string expectedTargetId,
        string actualTargetId,
        float? x,
        float? y,
        float? z,
        DateTime? firstManipulationStart,
        DateTime? lastManipulationEnd,
        bool? correct,
        string sequenceId = "",
        int? itemIndex = null,
        string appPhase = "",
        int? sourceContainerId = null,
        int? destinationContainerId = null,
        string transferId = "")
    {
        if (writer == null)
        {
            Debug.LogWarning(
                "[TestLogger] Cannot write to CSV: writer is null.");
            return;
        }

        string[] values =
        {
        FormatTimestamp(DateTime.Now),
        ParticipantId,
        condition.ToString(),
        SessionId,
        currentTask.ToString(CultureInfo.InvariantCulture),
        eventName,
        operation,
        artifactId,
        expectedTargetId,
        actualTargetId,
        FormatFloat(x),
        FormatFloat(y),
        FormatFloat(z),

        firstManipulationStart.HasValue
            ? FormatTimestamp(firstManipulationStart.Value)
            : "",

        lastManipulationEnd.HasValue
            ? FormatTimestamp(lastManipulationEnd.Value)
            : "",

        correct.HasValue
            ? (correct.Value ? "true" : "false")
            : "",

        sequenceId,

        itemIndex.HasValue
            ? itemIndex.Value.ToString(CultureInfo.InvariantCulture)
            : "",

        appPhase,

        sourceContainerId.HasValue
            ? sourceContainerId.Value.ToString(
                CultureInfo.InvariantCulture)
            : "",

        destinationContainerId.HasValue
            ? destinationContainerId.Value.ToString(
                CultureInfo.InvariantCulture)
            : "",

        transferId
    };

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = EscapeCsv(values[i]);
        }

        writer.WriteLine(string.Join(";", values));
    }
    private static string FormatTimestamp(
        DateTime value)
    {
        return value.ToString(
            "yyyy-MM-dd HH:mm:ss.fff",
            CultureInfo.InvariantCulture
        );
    }

    private static string FormatFloat(
        float? value)
    {
        return value.HasValue
            ? value.Value.ToString(
                "F4",
                CultureInfo.InvariantCulture
            )
            : "";
    }

    private static string EscapeCsv(
        string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        bool requiresQuotes =
            value.Contains(";") ||
            value.Contains("\"") ||
            value.Contains("\n") ||
            value.Contains("\r");

        if (!requiresQuotes)
        {
            return value;
        }

        return "\"" +
               value.Replace("\"", "\"\"") +
               "\"";
    }

    private void OnDestroy()
    {
        isTaskActive = false;
        StopPositionTracking();
        CloseLog();
    }

    public void CloseLog()
    {
        if (writer == null)
        {
            return;
        }

        writer.Flush();
        writer.Close();
        writer.Dispose();
        writer = null;

        Debug.Log(
            $"[TestLogger] Log closed: {filePath}"
        );
    }
}