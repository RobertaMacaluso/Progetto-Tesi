using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ArUcoDetectionHoloLensUnity
{
    public class ArUcoDebugVisualizer : MonoBehaviour
    {
        [SerializeField] private ArUcoMarkerDetection detector;
        [SerializeField] private TMP_Text debugText;
        [SerializeField] private GameObject markerGo;
        [SerializeField] private float markerSize = 0.08f;
        [SerializeField] private bool useAdditionalMarkers = false;

        private void Awake()
        {
            if (detector == null)
                detector = GetComponent<ArUcoMarkerDetection>();

            if (markerGo != null)
                markerGo.transform.localScale = new Vector3(markerSize, markerSize, markerSize);
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

                if (markerGo != null)
                {
                    markerGo.transform.SetPositionAndRotation(
                        marker.Position,
                        marker.Rotation);
                }

                if (debugText != null)
                {
                    debugText.text =
                        $"Detected marker {marker.Id}\n" +
                        $"Position: {marker.Position}\n" +
                        $"Rotation: {marker.Rotation}";
                }

                break;
            }
        }

        public void SetText(string txt)
        {
            debugText.text += txt;
        }
    }
}