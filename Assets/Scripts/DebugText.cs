using MixedReality.Toolkit.SpatialManipulation;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugText : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro textMeshPro = default;

    public GameObject debugPanel;
    // Start is called before the first frame update
    void Start()
    {
        textMeshPro.text = "test test";
        Application.logMessageReceived += HandleLog;
        //Application.logMessageReceived += HandleLog;
        //Application.logMessageReceivedThreaded += HandleLog;
        debugPanel.GetComponent<Follow>().enabled = false;
        debugPanel.GetComponent<SolverHandler>().enabled = false;
    }

    void Update()
    {
        while (logQueue.TryDequeue(out string log))
        {
            textMeshPro.text += "\n" + log;
        }
    }


    public void HandleLog(string logString, string stackTrace, LogType type)
    {
        textMeshPro.text += logString + "\n";

    }

    private ConcurrentQueue<string> logQueue = new();

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLogThreads;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLogThreads;
    }

    void HandleLogThreads(string logString, string stackTrace, LogType type)
    {
        logQueue.Enqueue(logString);
    }

    public void ResetText()
    {
        textMeshPro.text = "test test\n";
    }

    public void ManualDebug(string msg)
    {
        textMeshPro.text += msg + "\n";
    }

}
