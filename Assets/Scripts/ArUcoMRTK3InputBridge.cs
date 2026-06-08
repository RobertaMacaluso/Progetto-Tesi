using MixedReality.Toolkit;
using UnityEngine;

namespace ArUcoDetectionHoloLensUnity
{
    public class ArUcoMRTK3InputBridge : MonoBehaviour
    {
        [SerializeField] private ArUcoMarkerDetection detector;
        [SerializeField] private StatefulInteractable interactable;
        [SerializeField] private bool toggleOnClick = true;

        private void Awake()
        {
            if (detector == null)
                detector = GetComponent<ArUcoMarkerDetection>();

            if (interactable == null)
                interactable = GetComponent<StatefulInteractable>();
        }

        private void OnEnable()
        {
            if (interactable != null)
                interactable.OnClicked.AddListener(HandleClicked);
        }

        private void OnDisable()
        {
            if (interactable != null)
                interactable.OnClicked.RemoveListener(HandleClicked);
        }

        private void HandleClicked()
        {
            if (detector == null) return;

            if (toggleOnClick)
                detector.ToggleDetecting();
            else
                detector.StopDetecting();
        }
    }
}