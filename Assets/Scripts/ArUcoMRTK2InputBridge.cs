#if USE_MRTK2
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

namespace ArUcoDetectionHoloLensUnity
{
    public class ArUcoMRTK2InputBridge : MonoBehaviour, IMixedRealityPointerHandler
    {
        [SerializeField] private ArUcoMarkerDetection detector;
        [SerializeField] private bool toggleOnTap = false;

        private void Awake()
        {
            if (detector == null)
                detector = GetComponent<ArUcoMarkerDetection>();
        }

        private void OnEnable()
        {
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityPointerHandler>(this);
        }

        private void OnDisable()
        {
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityPointerHandler>(this);
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            if (detector == null) return;

            if (toggleOnTap)
                detector.ToggleDetecting();
            else
                detector.StopDetecting();

            eventData.Use();
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData) { }
        public void OnPointerDragged(MixedRealityPointerEventData eventData) { }
        public void OnPointerUp(MixedRealityPointerEventData eventData) { }
    }
}
#endif