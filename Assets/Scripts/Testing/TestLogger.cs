using System;
using System.IO;
using UnityEngine;

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

    private string filePath;
    private StreamWriter writer;

    public string ParticipantId { get; private set; }
    public string SessionId { get; private set; }
    public TestCondition Condition => condition;
    public int CurrentTask => currentTask;

    private const string PlayerPrefsKey = "LastParticipantId";

    private void Awake()
    {
        StartNewSession();
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
            "Timestamp,ParticipantId,Condition,SessionId,Task,Event,ArtifactId,X,Y,Z,Value"
        );

        writer.Flush();

        Debug.Log($"[TestLogger] Session started: {SessionId}");
        Debug.Log($"[TestLogger] Participant: {ParticipantId}");
        Debug.Log($"[TestLogger] Condition: {condition}");
        Debug.Log($"[TestLogger] File: {filePath}");
    }

    /// <summary>
    /// Sets the task number.
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
            Debug.LogWarning("[TestLogger] Participant ID cannot be negative.");
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

    private void OnDestroy()
    {
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

            Debug.Log($"[TestLogger] Log closed: {filePath}");
        }
    }
}