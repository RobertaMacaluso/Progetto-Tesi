using ArUcoDetectionHoloLensUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArUcoUIController : MonoBehaviour
{
    public ArUcoMarkerDetection detector;
    public ArUcoDebugVisualizer arUcoDebugVisualizer;
    public AppManager appManager;
    public MarkersManager markersManager;

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
        Debug.Log("ARUCO SCAN STOPPED");
        detector.StopDetecting();
        
        // riposizionamento della room che dipende dal marker
        int markerID = arUcoDebugVisualizer.lastMarkerID;

        Debug.Log("Marker ID: " + markerID);
        //markerID = 2; // per debug editor  TOGLIERE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        if (markerID > 0)   // globalRoot gestita direttamente in arUcoDebugVisualizer
        {
            GameObject room = appManager.GetRoomByID(markerID);
            if (room != null)
            {
                //appManager.SetInitialTransform(room);

                GameObject marker = markersManager.GetMarkerByID(markerID);
                StorageContainerView storageElement = room.GetComponent<StorageContainerView>();

                markersManager.SetRoomByMarker(storageElement, marker);
            }
        }

        // reset lastMarkerID
        arUcoDebugVisualizer.lastMarkerID = -1;
    }
}
