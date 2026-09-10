using System;
using System.Collections;
using System.IO;
using UnityEngine;
using System.Globalization;

public class TestLogger : MonoBehaviour
{
    public enum TestCondition
    {
        Traditional,
        HoloLens
    }

    [Header("Test Settings")]
    [SerializeField] private TestCondition condition = TestCondition.HoloLens;

    [SerializeField] private int currentTask = 0;

    [Header("Tracking")]
    [SerializeField] private Transform appManager;
    [SerializeField] private float trackingInterval = 0.1f;

    private string filePath;
    private StreamWriter writer;

    private Coroutine trackingCoroutine;
    private bool isTracking = false;

    public string ParticipantId { get; private set; }
    public string SessionId { get; private set; }
    public TestCondition Condition => condition;
    public int CurrentTask => currentTask;

    private const string PlayerPrefsKey = "LastParticipantId";

    private void Awake()
    {
        StartNewSession();

        if (appManager == null)
        {
            Debug.LogError(
                "[TestLogger] AppManager Transform is not assigned."
            );
        }

        if (trackingInterval <= 0f)
        {
            Debug.LogWarning(
                "[TestLogger] Tracking interval must be greater than 0. " +
                "Using 0.1 seconds."
            );

            trackingInterval = 0.1f;
        }
    }

    /// <summary>
    /// Creates a new test session and CSV file.
    /// Automatically assigns the next participant ID.
    /// </summary>
    public void StartNewSession()
    {
        // Get last participant ID
        int lastParticipantId = PlayerPrefs.GetInt(PlayerPrefsKey, 0);

        // Increment
        int newParticipantId = lastParticipantId + 1;

        // Save updated value
        PlayerPrefs.SetInt(PlayerPrefsKey, newParticipantId);
        PlayerPrefs.Save();

        ParticipantId = $"P{newParticipantId:D2}";

        // Create unique session ID
        SessionId =
            $"{ParticipantId}_{condition}_{DateTime.Now:yyyyMMdd_HHmmss}";

        // Create directory
        string directory = Path.Combine(
            Application.persistentDataPath,
            "TestLogs"
        );

        Directory.CreateDirectory(directory);

        // Create CSV path
        filePath = Path.Combine(
            directory,
            $"{SessionId}.csv"
        );

        // Create file
        writer = new StreamWriter(filePath, false);

        // CSV header
        writer.WriteLine(
            "Timestamp;ParticipantId;Condition;SessionId;Task;Event;ArtifactId;X;Y;Z;Value"
        );

        writer.Flush();

        Debug.Log($"[TestLogger] Session started: {SessionId}");
        Debug.Log($"[TestLogger] Participant: {ParticipantId}");
        Debug.Log($"[TestLogger] Condition: {condition}");
        Debug.Log($"[TestLogger] File: {filePath}");
    }

    /// <summary>
    /// Sets the task number manually.
    /// </summary>
    public void SetTask(int taskNumber)
    {
        currentTask = taskNumber;

        Debug.Log($"[TestLogger] Current task: {currentTask}");
    }

    /// <summary>
    /// Manually set the next participant ID.
    /// Example: SetParticipantId(17) means the next automatic ID will be P18.
    /// </summary>
    public void SetNextParticipantId(int participantId)
    {
        if (participantId < 0)
        {
            Debug.LogWarning(
                "[TestLogger] Participant ID cannot be negative."
            );

            return;
        }

        PlayerPrefs.SetInt(PlayerPrefsKey, participantId);
        PlayerPrefs.Save();

        Debug.Log(
            $"[TestLogger] Last participant ID set to {participantId}. " +
            $"Next session will be P{participantId + 1:D2}."
        );
    }

    /// <summary>
    /// Reset participant counter.
    /// Next session will be P01.
    /// </summary>
    public void ResetParticipantId()
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, 0);
        PlayerPrefs.Save();

        Debug.Log(
            "[TestLogger] Participant counter reset. " +
            "Next session will be P01."
        );
    }

    /// <summary>
    /// Returns the current participant number.
    /// </summary>
    public int GetCurrentParticipantNumber()
    {
        return PlayerPrefs.GetInt(PlayerPrefsKey, 0);
    }

    /// <summary>
    /// Starts the next task.
    /// Called when the Player exits the start/end zone.
    /// </summary>
    public void StartNextTask()
    {
        // Safety check: don't start a new task if one is already active.
        if (isTracking)
        {
            Debug.LogWarning(
                "[TestLogger] A task is already active. " +
                "Ignoring StartNextTask()."
            );

            return;
        }

        currentTask++;

        Debug.Log(
            $"[TestLogger] Task {currentTask} started."
        );

        WriteEvent("TaskStarted");

        StartTracking();
    }

    /// <summary>
    /// Ends the current task.
    /// Called when the Player enters the start/end zone.
    /// </summary>
    public void EndCurrentTask()
    {
        if (currentTask == 0)
        {
            Debug.LogWarning(
                "[TestLogger] EndCurrentTask called before any task started."
            );

            return;
        }

        if (!isTracking)
        {
            Debug.LogWarning(
                $"[TestLogger] Task {currentTask} is not currently being tracked."
            );

            return;
        }

        // Save one final position before stopping
        LogCurrentPosition();

        StopTracking();

        WriteEvent("TaskCompleted");

        Debug.Log(
            $"[TestLogger] Task {currentTask} ended."
        );
    }

    /// <summary>
    /// Starts collecting the AppManager position.
    /// </summary>
    private void StartTracking()
    {
        if (appManager == null)
        {
            Debug.LogError(
                "[TestLogger] Cannot start tracking: AppManager is not assigned."
            );

            return;
        }

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
        }

        isTracking = true;

        trackingCoroutine = StartCoroutine(TrackPosition());

        Debug.Log(
            $"[TestLogger] Position tracking started for Task {currentTask}."
        );
    }

    /// <summary>
    /// Stops collecting the AppManager position.
    /// </summary>
    private void StopTracking()
    {
        isTracking = false;

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }

        Debug.Log(
            $"[TestLogger] Position tracking stopped for Task {currentTask}."
        );
    }

    /// <summary>
    /// Coroutine that samples the AppManager position at fixed intervals.
    /// </summary>
    private IEnumerator TrackPosition()
    {
        while (isTracking)
        {
            LogCurrentPosition();

            yield return new WaitForSeconds(trackingInterval);
        }

        trackingCoroutine = null;
    }

    /// <summary>
    /// Logs the current AppManager position.
    /// </summary>
    private void LogCurrentPosition()
    {
        if (appManager == null)
        {
            return;
        }

        Vector3 position = appManager.position;

        WriteCsvLine(
            "Position",
            "",
            position.x,
            position.y,
            position.z,
            ""
        );
    }

    /// <summary>
    /// Writes a generic event to the CSV.
    /// </summary>
    private void WriteEvent(string eventName)
    {
        WriteCsvLine(
            eventName,
            "",
            null,
            null,
            null,
            ""
        );
    }

    /// <summary>
    /// Writes one complete CSV line.
    /// </summary>
    private void WriteCsvLine(
      string eventName,
      string artifactId,
      float? x,
      float? y,
      float? z,
      string value)
    {
        if (writer == null)
        {
            Debug.LogWarning(
                "[TestLogger] Cannot write to CSV: writer is null."
            );

            return;
        }

        string timestamp = DateTime.Now.ToString(
            "yyyy-MM-dd HH:mm:ss.fff"
        );

        string xValue = x.HasValue
            ? x.Value.ToString("F4", CultureInfo.InvariantCulture)
            : "";

        string yValue = y.HasValue
            ? y.Value.ToString("F4", CultureInfo.InvariantCulture)
            : "";

        string zValue = z.HasValue
            ? z.Value.ToString("F4", CultureInfo.InvariantCulture)
            : "";

        writer.WriteLine(
            $"{timestamp};" +
            $"{ParticipantId};" +
            $"{condition};" +
            $"{SessionId};" +
            $"{currentTask};" +
            $"{eventName};" +
            $"{artifactId};" +
            $"{xValue};" +
            $"{yValue};" +
            $"{zValue};" +
            $"{value}"
        );

        writer.Flush();
    }

    private void OnDestroy()
    {
        StopTracking();
        CloseLog();
    }

    /// <summary>
    /// Flushes and closes the CSV.
    /// </summary>
    public void CloseLog()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer.Dispose();
            writer = null;

            Debug.Log(
                $"[TestLogger] Log closed: {filePath}"
            );
        }
    }
}