using Microsoft.MixedReality.WorldLocking.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR.OpenXR.Input;

public class MarkersManager : MonoBehaviour
{
    Dictionary<int, GameObject> markersCreated = new Dictionary<int, GameObject>();
    // Traccia quali marker devono forzare un Hard Update alla prossima inquadratura
    public HashSet<int> markersNeedingHardReset = new HashSet<int>();

    [SerializeField] private AppManager appManager;
    [SerializeField] private GameObject cubePlaceHolder;
    [SerializeField] private GameObject globalRoot;

    // Start is called before the first frame update
    void Start()
    {
        //PlayerPrefs.SetString(globalRoot.name, "1_3_2/0_0_0_1");
        //PlayerPrefs.SetString("Marker_1", "3_3_3/45_45_45_1");
        //PlayerPrefs.DeleteKey("Marker_1");
        //PlayerPrefs.DeleteKey(globalRoot.name);
        //PlayerPrefs.SetString("Marker_1", "0_0_0/0_0_0_1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnMarkers(StorageContainerView storageELement)
    {
        int markerId = storageELement.data.markerId;
        GameObject newMarker = new GameObject("Marker_" + markerId.ToString());
        newMarker.transform.SetParent(this.gameObject.transform, false);
        newMarker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        markersCreated.Add(markerId, newMarker);

        string pose = PlayerPrefs.GetString(newMarker.name);
        if (!string.IsNullOrEmpty(pose))
        {
            Debug.Log("PlayerPrefs di Marker_" + markerId.ToString());

            if (globalRoot != null)
            {
                SetTransformMarker(newMarker, pose);
            }
            else
            {
                Debug.LogError("Impossibile caricare il marker relativo: globalRoot è null!");
            }
        }

        if (!string.IsNullOrEmpty(storageELement.data.worldTransform))
        {
            SetRoomByMarker(storageELement, newMarker);
        }
        else
        {
            storageELement.transform.SetPositionAndRotation(newMarker.transform.position, newMarker.transform.rotation);
        }

        // --- INTEGRAZIONE WORLD LOCKING TOOLS (WLT) ---
        // PASSO B: Aggiungiamo dinamicamente i componenti del WLT sul marker appena nato
        SpacePin spacePin = newMarker.AddComponent<SpacePin>();

        // Consigliato per aiutare il WLT a bloccare correttamente anche la rotazione (YAW) sul muro
        newMarker.AddComponent<Orienter>();

        // PASSO C: Notifichiamo il WLT che il posizionamento teorico (Modeling Pose) è completo
        if (spacePin != null)
        {
            spacePin.ResetModelingPose();
            Debug.Log($"[WLT] SpacePin inizializzato correttamente per {newMarker.name}");
        }
        // ----------------------------------------------

        // cubo per debug
        GameObject cube = Instantiate(cubePlaceHolder, Vector3.zero, Quaternion.identity, newMarker.transform);
        cube.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cube.name = "CubeMarker_" + markerId.ToString();
    }


    //private void SetTransformMarker(GameObject marker, string pose)
    //{
    //    string[] transform = pose.Split('/');

    //    string[] pos = transform[0].Split('_');
    //    string[] rot = transform[1].Split('_');

    //    Debug.Log("Rot_X = " + rot[0].ToString());

    //    marker.transform.SetPositionAndRotation(
    //        new Vector3(
    //            float.Parse(pos[0], CultureInfo.InvariantCulture),
    //            float.Parse(pos[1], CultureInfo.InvariantCulture),
    //            float.Parse(pos[2], CultureInfo.InvariantCulture)),
    //        new Quaternion(
    //            float.Parse(rot[0], CultureInfo.InvariantCulture),
    //            float.Parse(rot[1], CultureInfo.InvariantCulture),
    //            float.Parse(rot[2], CultureInfo.InvariantCulture),
    //            float.Parse(rot[3], CultureInfo.InvariantCulture)));
    //}

    private void SetTransformMarker(GameObject marker, string pose)
    {
        string[] transformParts = pose.Split('/');

        string[] posParts = transformParts[0].Split('_');
        string[] rotParts = transformParts[1].Split('_');

        // 1. Leggiamo i dati come posizioni LOCALI (relative alla globalRoot)
        Vector3 localPos = new Vector3(
            float.Parse(posParts[0], CultureInfo.InvariantCulture),
            float.Parse(posParts[1], CultureInfo.InvariantCulture),
            float.Parse(posParts[2], CultureInfo.InvariantCulture));

        Quaternion localRot = new Quaternion(
            float.Parse(rotParts[0], CultureInfo.InvariantCulture),
            float.Parse(rotParts[1], CultureInfo.InvariantCulture),
            float.Parse(rotParts[2], CultureInfo.InvariantCulture),
            float.Parse(rotParts[3], CultureInfo.InvariantCulture));

        // 2. CONVERSIONE: Trasformiamo da Spazio Locale della Root a Spazio del Mondo di Unity
        Vector3 worldPos = globalRoot.transform.TransformPoint(localPos);
        Quaternion worldRot = globalRoot.transform.rotation * localRot;

        // 3. Applichiamo la transform assoluta nel mondo
        marker.transform.SetPositionAndRotation(worldPos, worldRot);
    }

    public void SetRoomByMarker(StorageContainerView storageELement, GameObject marker)
    {
        if (string.IsNullOrEmpty(storageELement.data.worldTransform))
        {
            storageELement.gameObject.transform.
                SetPositionAndRotation(marker.transform.position, marker.transform.rotation);

            return;
        }

        string[] transform = storageELement.data.worldTransform.Split('/');

        string[] pos = transform[0].Split('_');
        string[] rot = transform[1].Split('_');

        Vector3 localPos = new Vector3(
                float.Parse(pos[0], CultureInfo.InvariantCulture),
                float.Parse(pos[1], CultureInfo.InvariantCulture),
                float.Parse(pos[2], CultureInfo.InvariantCulture));

        Quaternion localRot = new Quaternion(
            float.Parse(rot[0], CultureInfo.InvariantCulture),
            float.Parse(rot[1], CultureInfo.InvariantCulture),
            float.Parse(rot[2], CultureInfo.InvariantCulture),
            float.Parse(rot[3], CultureInfo.InvariantCulture));

        storageELement.transform.position = marker.transform.TransformPoint(localPos);
        storageELement.transform.rotation = marker.transform.rotation * localRot;
    }

    public void UpdateAllMarkersFromGlobalRoot()
    {
        if (globalRoot == null)
        {
            UnityEngine.Debug.LogError("Impossibile aggiornare: Global Root (Marker 0) non assegnata.");
            return;
        }

        // 1. CICLO SU TUTTI I MARKER SECONDARI ATTIVI
        foreach (var kvp in markersCreated)
        {
            int markerId = kvp.Key;
            GameObject markerObj = kvp.Value;


            // 2. RECUPERO DELLA STRINGA POSE ORIGINARIA
            // Recupera la stringa "pos_x_y_z/rot_x_y_z_w" salvata per questo specifico marker
            string savedPoseString = PlayerPrefs.GetString(markerObj.name);

            if (string.IsNullOrEmpty(savedPoseString))
            {
                UnityEngine.Debug.LogWarning($"Nessuna posa salvata trovata per il Marker ID: {markerId}");
                continue;
            }

            // 3. AGGIORNAMENTO TRASFORM DEL MARKER (Usa la tua funzione)
            SetTransformMarker(markerObj, savedPoseString);

            // 4. NOTIFICA A WORLD LOCKING TOOLS (WLT)
            // Diciamo allo SpacePin del marker che la sua posizione fisica nel mondo reale è cambiata
            SpacePin markerSpacePin = markerObj.GetComponent<SpacePin>();
            if (markerSpacePin != null)
            {
                markerSpacePin.ResetModelingPose();
            }

            // 5. SEGNALIAMO che questo marker è stato spostato virtualmente e richiede un Hard Update reale appena visto
            markersNeedingHardReset.Add(markerId);

            // 6. AGGIORNAMENTO DELLE ROOM ASSOCIATE A QUESTO MARKER
            // Troviamo tutte le room/elementi agganciati a questo specifico marker
            GameObject room = appManager.GetRoomByID(markerId); // <--- Sostituisci con la tua logica di filtri

            if (room != null)
            {
                SetRoomByMarker(room.GetComponent<StorageContainerView>(), markerObj);
            }
        }

        UnityEngine.Debug.Log("Riallineamento completo della stanza terminato con successo in base alla nuova Global Root.");
    }



    public GameObject GetMarkerByID(int id)
    {
        if (markersCreated.ContainsKey(id))
            return markersCreated[id];
        else
            return null;
    }    
}
