using ArUcoDetectionHoloLensUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArUcoUIController : MonoBehaviour
{
    public ArUcoMarkerDetection detector;
    public ArUcoDebugVisualizer arUcoDebugVisualizer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartScan()
    {
        detector.StartDetecting();
        arUcoDebugVisualizer.detectingText.text = "Scansione marker in corso";
        Debug.Log("ARUCO SCAN STARTED");
    }

    public void StopScan()
    {
        detector.StopDetecting();
        Debug.Log("ARUCO SCAN STOPPED");
    }
}
