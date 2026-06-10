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
        [SerializeField] private TMP_Text debugText;
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
                if (debugText != null)
                    debugText.text = "No marker detected.";

                return;
            }

            foreach (var markerPair in markers)
            {
                var marker = markerPair.Value;

                if (globalRoot != null)
                {
                    globalRoot.transform.SetPositionAndRotation(
                        marker.Position,
                        marker.Rotation);

                    // Salvataggio transform marker nei PlayerPrefs
                    SaveInPlayerPrefs(marker.Position, marker.Rotation);
                }

                if (debugText != null)
                {
                    debugText.text =
                        $"Detected marker {marker.Id}\n" +
                        $"Position: {marker.Position}\n" +
                        $"Rotation: {marker.Rotation}\n \n" +
                        $"Camera player position: {cameraPlayer.transform.position}";
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
            debugText.text += "\n \n" + txt;
        }
    }
}