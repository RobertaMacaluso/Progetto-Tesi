using Microsoft.MixedReality.WorldLocking.Core;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ArUcoDetectionHoloLensUnity
{
    public class ArUcoDebugVisualizer : MonoBehaviour
    {
        [SerializeField] private ArUcoMarkerDetection detector;
        [SerializeField] private Camera cameraPlayer;
        [SerializeField] public TMP_Text detectingText;
        [SerializeField] private GameObject globalRoot;
        [SerializeField] private float markerSize = 0.08f;
        [SerializeField] private bool useAdditionalMarkers = false;


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
                if (globalRoot != null)
                {
                    globalRoot.transform.SetPositionAndRotation(
                        marker.Position,
                        marker.Rotation);

                    //SpacePin spacePin = globalRoot.GetComponent<SpacePin>();
                    //Pose markerPose = new Pose(marker.Position, marker.Rotation);

                    //spacePin.SetFrozenPose(markerPose);

                    // Salvataggio transform marker nei PlayerPrefs
                    SaveInPlayerPrefs(marker.Position, marker.Rotation);

                    Debug.Log("Update spacePin");
                    // space pin WLT
                    //spacePin.transform.SetParent(globalRoot.transform);
                    //spacePin.transform.localPosition = Vector3.zero;
                    //spacePin.transform.localRotation = Quaternion.identity;
                }

                if (detectingText != null)
                {
                    //detectingText.text =
                    //    $"Detected marker {marker.Id}\n" +
                    //    $"Position: {marker.Position}\n" +
                    //    $"Rotation: {marker.Rotation}\n \n" +
                    //    $"Camera player position: {cameraPlayer.transform.position}";

                    detectingText.text = "Marker trovato!\nContinua la scansione per qualche secondo per una migliore precisione";
                }

                break;
            }
        }

        public void SaveInPlayerPrefs(Vector3 markerPos, Quaternion markerRot)
        {
            string positionLocal = markerPos.x.ToString(CultureInfo.InvariantCulture) + "_" +
                markerPos.y.ToString(CultureInfo.InvariantCulture) + "_" +
                markerPos.z.ToString(CultureInfo.InvariantCulture);

            string rotationLocal = markerRot.x.ToString(CultureInfo.InvariantCulture) + "_" +
                markerRot.y.ToString(CultureInfo.InvariantCulture) + "_" +
                markerRot.z.ToString(CultureInfo.InvariantCulture) + "_" +
                markerRot.w.ToString(CultureInfo.InvariantCulture);

            string transformLocal = positionLocal + "/" + rotationLocal;

            PlayerPrefs.SetString(globalRoot.name, transformLocal);
            Debug.Log("SaveGlobalRoot");
        }

        public void SetText(string txt)
        {
            detectingText.text += "\n \n" + txt;
        }
    }
}