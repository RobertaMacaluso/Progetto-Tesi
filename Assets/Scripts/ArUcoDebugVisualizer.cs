using Microsoft.MixedReality.WorldLocking.Core;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace ArUcoDetectionHoloLensUnity
{
    public class ArUcoDebugVisualizer : MonoBehaviour
    {
        [SerializeField] private MarkersManager markersManager;
        [SerializeField] private ArUcoMarkerDetection detector;
        [SerializeField] private Camera cameraPlayer;
        [SerializeField] public TMP_Text detectingText;
        [SerializeField] private GameObject globalRoot;
        [SerializeField] private float markerSize = 0.08f;
        [SerializeField] private bool useAdditionalMarkers = false;

        public int lastMarkerID { get; set; } = -1;


        private void Awake()
        {
            if (detector == null)
                detector = GetComponent<ArUcoMarkerDetection>();

            if (globalRoot != null)
                globalRoot.transform.localScale = new Vector3(markerSize, markerSize, markerSize);
        }

        private void OnEnable()
        {
            if (detector == null)
                return;

            if (useAdditionalMarkers)
                detector.AdditionalMarkersUpdated += OnMarkersUpdated;
            else
                detector.MarkersUpdated += OnMarkersUpdated;
        }

        private void OnDisable()
        {
            if (detector == null)
                return;

            if (useAdditionalMarkers)
                detector.AdditionalMarkersUpdated -= OnMarkersUpdated;
            else
                detector.MarkersUpdated -= OnMarkersUpdated;
        }

        private void OnMarkersUpdated(Dictionary<int, ArUcoMarkerDetection.Marker> markers)
        {
            if (markers == null || markers.Count == 0)
            {
                if (detectingText != null)
                    detectingText.text = "Nessun marker trovato.";

                return;
            }

            foreach (var markerPair in markers)
            {
                var marker = markerPair.Value;
                //int d = marker.Id;
                if (marker.Id == 0)
                {
                    lastMarkerID = 0;

                    if ( globalRoot != null)
                    {
                        ManageUpdates(globalRoot, marker);
                    }
                }
                else
                {
                    lastMarkerID = marker.Id;

                    GameObject markerObj = markersManager.GetMarkerByID(marker.Id);
                    if (markerObj != null)
                    {
                        ManageUpdates(markerObj, marker);
                    }
                }

                //SaveInPlayerPrefs(marker.Position, marker.Rotation, marker.Id);

                if (detectingText != null)
                {
                    //detectingText.text =
                    //    $"Detected marker {marker.Id}\n" +
                    //    $"Position: {marker.Position}\n" +
                    //    $"Rotation: {marker.Rotation}\n \n" +
                    //    $"Camera player position: {cameraPlayer.transform.position}";

                    detectingText.text = "Trovato Marker " + marker.Id.ToString() + "!\nContinua la scansione per qualche secondo per una migliore precisione";
                }

                break;
            }
        }

        public void ManageUpdates(GameObject gameObject, ArUcoMarkerDetection.Marker marker)
        {
            //float threshold = (gameObject == globalRoot) ? 0.1f : 0.05f;
            float threshold = 0.03f;

            // Se non è mai stato salvato nei PlayerPrefs
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(gameObject.name)))
            {
                UpdateMarker(gameObject, marker, true);
                return;
            }

            // CONTROLLO CRITICO: La Root si era mossa?
            if (markersManager.markersNeedingHardReset.Contains(marker.Id))
            {
                Debug.Log($"[WLT] Marker {marker.Id} reinquadrato dopo movimento della Root. Forzo HARD UPDATE per evitare lo scivolamento.");

                // Eseguiamo l'Hard Update (Caso A): riallinea transform, resetta modellazione e salva
                UpdateMarker(gameObject, marker, true);

                // Rimuoviamo il marker dal set così i successivi frame torneranno a fare Soft Update fluidi
                markersManager.markersNeedingHardReset.Remove(marker.Id);
                return;
            }

            SpacePin pin = gameObject.GetComponent<SpacePin>();

            if (pin != null)
            {
                // 1. Recuperiamo la trasformazione da Locked Space a Pinned/Frozen Space
                Pose pinnedFromLocked = pin.AlignmentManager.PinnedFromLocked;

                // 2. Trasformiamo la posizione fisica memorizzata (pin.LockedPose.position) nello spazio globale corrente
                Vector3 currentPhysicalAnchorPos = pinnedFromLocked.position + (pinnedFromLocked.rotation * pin.LockedPose.position);

                float distance = Vector3.Distance(currentPhysicalAnchorPos, marker.Position);

                if (distance > threshold)
                {
                    Debug.Log($"Marker spostato fisicamente! Distanza: {distance}");
                    UpdateMarker(gameObject, marker, true);

                    if (gameObject ==  globalRoot)
                        markersManager.UpdateAllMarkersFromGlobalRoot();
                }
                else
                {
                    Debug.Log($"Micro-correzione WLT applicata. Distanza: {distance}");
                    // Passiamo false: aggiorna solo lo SpacePin, non il Transform/Save
                    UpdateMarker(gameObject, marker, false);
                }
            }
        }

        public void UpdateMarker(GameObject obj, ArUcoMarkerDetection.Marker marker, bool updateTransform)
        {
            SpacePin spacePin = obj.GetComponent<SpacePin>();
            if (spacePin == null)
            {
                Debug.LogError("Nessuno SpacePin trovato su " + obj.name);
                return;
            }

            // Creiamo la posa fisica letta dal visore in questo istante
            Pose physicalPose = new Pose(marker.Position, marker.Rotation);

            // CASO A: Il marker è stato spostato fisicamente sul muro
            if (updateTransform)
            {
                // 1. Spostiamo l'oggetto di Unity nella nuova posizione reale
                obj.transform.SetPositionAndRotation(marker.Position, marker.Rotation);

                // 2. Diciamo al WLT: "Guarda che ho cambiato la sua posizione teorica (virtuale)"
                spacePin.ResetModelingPose();

                // 3. Salviamo nei PlayerPrefs (ora calcolerà l'offset relativo alla globalRoot in automatico!)
                int id = marker.Id; // Assicurati che marker.Id sia l'int corretto
                SaveInPlayerPrefs(marker.Position, marker.Rotation, id);

                // 4. Diciamo al WLT dove si trova il marker nella realtà
                spacePin.SetFrozenPose(physicalPose);

                // 5. FORZIAMO il ricalcolo della mappa elastica (solo perché il marker si è spostato di posizione)
                WorldLockingManager.GetInstance().AlignmentManager.SendAlignmentAnchors();

                // 6. Salviamo la sessione WLT su disco (operazione pesante, fatta solo all'occorrenza)
                WorldLockingManager.GetInstance().Save();

                Debug.Log($"[WLT] HARD UPDATE: Marker {obj.name} spostato. Transform, WLT e DB aggiornati.");
            }
            // CASO B: Il marker è fermo, stiamo solo correggendo il microscopico drift/rumore del visore
            else
            {
                // Diciamo SOLO al WLT dove si trova il marker in questo millesimo di secondo.
                // Il WLT integrerà questo dato nel suo algoritmo di smoothing in background senza bloccare il frame.
                spacePin.SetFrozenPose(physicalPose);

                Debug.Log($"[WLT] SOFT UPDATE: Micro-correzione drift applicata a {obj.name}");
            }
        }

        public void SaveInPlayerPrefs(Vector3 markerPos, Quaternion markerRot, int id)
        {
            // Di base teniamo i valori passati (es. per la globalRoot)
            Vector3 posToSave = markerPos;
            Quaternion rotToSave = markerRot;

            // SE è un marker secondario (id != 0), lo salviamo RELATIVO alla globalRoot
            if (id != 0 && globalRoot != null)
            {
                // Sottraiamo la posizione della globalRoot e annulliamo la sua rotazione
                posToSave = Quaternion.Inverse(globalRoot.transform.rotation) * (markerPos - globalRoot.transform.position);

                // Calcoliamo la rotazione relativa rispetto alla globalRoot
                rotToSave = Quaternion.Inverse(globalRoot.transform.rotation) * markerRot;
            }

            // Da qui in poi la tua logica di formattazione stringa rimane IDENTICA, 
            // ma usa posToSave e rotToSave al posto dei parametri originali.
            string positionLocal = posToSave.x.ToString(CultureInfo.InvariantCulture) + "_" +
                posToSave.y.ToString(CultureInfo.InvariantCulture) + "_" +
                posToSave.z.ToString(CultureInfo.InvariantCulture);

            string rotationLocal = rotToSave.x.ToString(CultureInfo.InvariantCulture) + "_" +
                rotToSave.y.ToString(CultureInfo.InvariantCulture) + "_" +
                rotToSave.z.ToString(CultureInfo.InvariantCulture) + "_" +
                rotToSave.w.ToString(CultureInfo.InvariantCulture);

            string transformLocal = positionLocal + "/" + rotationLocal;

            if (id == 0)
            {
                PlayerPrefs.SetString(globalRoot.name, transformLocal);
                Debug.Log("SaveGlobalRoot (Posizione Assoluta)");
            }
            else
            {
                PlayerPrefs.SetString("Marker_" + id.ToString(), transformLocal);
                Debug.Log("Salvataggio Marker_" + id.ToString() + " (Relativo a GlobalRoot)");
            }
        }

        public void SetText(string txt)
        {
            detectingText.text += "\n \n" + txt;
        }
    }
}