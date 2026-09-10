using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTrigger : MonoBehaviour
{
    [SerializeField] private TestLogger testLogger;

    private void Awake()
    {
        //testLogger = FindFirstObjectByType<TestLogger>();

        if (testLogger == null)
        {
            Debug.LogError(
                "[TestStartEndTrigger] TestLogger not found in the scene."
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        Debug.Log(
            $"[TestStartEndTrigger] Player EXITED start/end zone: {other.name}"
        );

        if (testLogger != null)
        {
            testLogger.StartNextTask();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        Debug.Log(
            $"[TestStartEndTrigger] Player ENTERED start/end zone: {other.name}"
        );

        if (testLogger != null)
        {
            testLogger.EndCurrentTask();
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }
}
