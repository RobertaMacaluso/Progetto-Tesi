using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Unity.XR.CoreUtils;


#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

#if WINDOWS_UWP
using Windows.Perception.Spatial;
using HoloLensForCV;
#endif

namespace ArUcoDetectionHoloLensUnity
{
    public class ArUcoMarkerDetection : MonoBehaviour
    {
        public ArUcoDebugVisualizer debugVisualizer;
        [SerializeField] private bool _detectOnStart = false;
        public bool IsDetecting => _detecting;

        public event MarkersUpdatedHandler MarkersUpdated;
        public event MarkersUpdatedHandler AdditionalMarkersUpdated;

        [SerializeField] private MarkerPositionBehavior _markerPositionBehavior = MarkerPositionBehavior.Moving;
        [SerializeField] private bool _additionalMovingDetectionStrategy = true;

        private readonly Dictionary<int, List<Marker>> _markerObservations = new Dictionary<int, List<Marker>>();
        private readonly Dictionary<int, List<Marker>> _additionalMarkerObservations = new Dictionary<int, List<Marker>>();

        private Dictionary<int, Marker> _nextMarkerUpdate;
        private Dictionary<int, Marker> _additionalNextMarkerUpdate;

        private MarkerDetectionCompletionStrategy _detectionCompletionStrategy;
        private MarkerDetectionCompletionStrategy _additionalDetectionCompletionStrategy;

        [HideInInspector] public int RequiredObservations = 5;
        [HideInInspector] public int RequiredInlierCount = 5;
        [HideInInspector] public int MaximumMarkerSampleCount = 15;
        [HideInInspector] public float MaximumPositionDistanceStandardDeviation = 0.01f;
        [HideInInspector] public float MaximumRotationAngleStandardDeviation = 0.75f;
        [HideInInspector] public float MarkerInlierStandardDeviationThreshold = 1.5f;

        [Header("Detection Parameters")]
        public CvUtils.DeviceTypeUnity deviceType;
        public CvUtils.SensorTypeUnity sensorTypePv;
        public CvUtils.ArUcoDictionaryName arUcoDictionaryName;
        public float markerSize = 0.08f;
        public CameraCalibrationParams calibParams;

        [Header("Performance")]
        public int skipFrames = 3;

        private bool _detecting = false;
        private bool _mediaFrameSourceGroupsStarted = false;
        private bool _isProcessingFrame = false;
        private int _frameCount = 0;

#if WINDOWS_UWP
        private SensorFrameStreamer _sensorFrameStreamerPv;
        private SpatialPerception _spatialPerception;
        private HoloLensForCV.DeviceType _deviceType;
        private MediaFrameSourceGroupType _mediaFrameSourceGroup;
        private MediaFrameSourceGroup _pvMediaFrameSourceGroup;
        private SensorType _sensorType;
        private SpatialCoordinateSystem _unityCoordinateSystem;
#endif

        public MarkerPositionBehavior MarkerPositionBehaviorProperty
        {
            get => _markerPositionBehavior;
            set
            {
                if (_markerPositionBehavior != value)
                {
                    _markerPositionBehavior = value;
                    UpdateDetectionCompletionStrategy();
                }
            }
        }

        private void Awake()
        {
            UpdateDetectionCompletionStrategy();
        }


        private void Start()
        {
            if (_detectOnStart)
            {
                StartDetecting();
            }
        }

        private async void Update()
        {
            if (_detecting && !_isProcessingFrame)
            {
#if WINDOWS_UWP
                if (_pvMediaFrameSourceGroup != null && _mediaFrameSourceGroupsStarted)
                {
                    _frameCount++;

                    if (_frameCount >= skipFrames)
                    {
                        _frameCount = 0;
                        _isProcessingFrame = true;

                        MediaFrameSourceGroup frameSourceGroup = _pvMediaFrameSourceGroup;

                        try
                        {
                            if (frameSourceGroup != null)
                            {
                                var detections = await Task.Run(() =>
                                    frameSourceGroup.DetectArUcoMarkers(_sensorType));

                                UpdateArUcoDetections(detections);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("ArUco detection failed: " + ex);
                        }
                        finally
                        {
                            _isProcessingFrame = false;
                        }
                    }
                }
#endif
            }

            if (_nextMarkerUpdate != null)
            {
                MarkersUpdated?.Invoke(_nextMarkerUpdate);
                _nextMarkerUpdate = null;
            }

            if (_additionalNextMarkerUpdate != null)
            {
                AdditionalMarkersUpdated?.Invoke(_additionalNextMarkerUpdate);
                _additionalNextMarkerUpdate = null;
            }
        }

        private async void OnApplicationQuit()
        {
            await StopHoloLensMediaFrameSourceGroup();
        }
        public void ToggleDetecting()
        {
            if (_detecting)
            {
                StopDetecting();
            }
            else
            {
                StartDetecting();
            }
        }

        public async void StartDetecting()
        {
            if (_detecting)
            {
                return;
            }

            _detecting = true;
            _frameCount = 0;
            _isProcessingFrame = false;

            _markerObservations.Clear();
            _additionalMarkerObservations.Clear();

            Debug.Log("Starting ArUco marker detection");

            await StartHoloLensMediaFrameSourceGroups();
            StartCoroutine(DelayCoroutine());
        }

        public async void StopDetecting()
        {
            if (!_detecting)
            {
                return;
            }

            _detecting = false;
            _isProcessingFrame = false;

#if WINDOWS_UWP
            Debug.Log("Stopping ArUco marker detection");
            await StopHoloLensMediaFrameSourceGroup();
#else
            Debug.LogWarning("Capturing is only supported on UWP/HoloLens.");
#endif
        }

        private IEnumerator DelayCoroutine()
        {
            yield return new WaitForSeconds(2f);
            //Debug.Log("XR Origin: " + FindObjectOfType<XROrigin>().transform.position);
        }

        private async Task StartHoloLensMediaFrameSourceGroups()
        {
#if WINDOWS_UWP
            _sensorType = (SensorType)sensorTypePv;

            try
            {
                //new WaitForSeconds(4f);
                //_sensorFrameStreamerPv = new SensorFrameStreamer();
                //_sensorFrameStreamerPv.Enable(_sensorType);
                
                Debug.Log("StartHoloLensMediaFrameSourceGroups ENTER");

                await Task.Delay(5000);

                Debug.Log("Creating SensorFrameStreamer");

                _sensorFrameStreamerPv = new SensorFrameStreamer();

                Debug.Log("SensorFrameStreamer CREATED");
            }
            catch (Exception ex)
            {
                Debug.LogError("SensorFrameStreamer init failed");
                Debug.LogError(ex.ToString());

                //if (myText != null)
                debugVisualizer.SetText(ex.ToString());

                _detecting = false;
                return;
            }
            //catch (Exception ex)
            //{
            //    Debug.LogError("SensorFrameStreamer init failed: " + ex);
            //    _detecting = false;
            //    return;
            //}

            _spatialPerception = new SpatialPerception();

            _mediaFrameSourceGroup = sensorTypePv == CvUtils.SensorTypeUnity.PhotoVideo
                ? MediaFrameSourceGroupType.PhotoVideoCamera
                : MediaFrameSourceGroupType.HoloLensResearchModeSensors;

            _deviceType = (HoloLensForCV.DeviceType)deviceType;

            _pvMediaFrameSourceGroup = new MediaFrameSourceGroup(
                _mediaFrameSourceGroup,
                _spatialPerception,
                _deviceType,
                _sensorFrameStreamerPv,
                calibParams.focalLength.x,
                calibParams.focalLength.y,
                calibParams.principalPoint.x,
                calibParams.principalPoint.y,
                calibParams.radialDistortion.x,
                calibParams.radialDistortion.y,
                calibParams.radialDistortion.z,
                calibParams.tangentialDistortion.x,
                calibParams.tangentialDistortion.y,
                calibParams.imageHeight,
                calibParams.imageWidth);

            _pvMediaFrameSourceGroup.Enable(_sensorType);

            try
            {
                await _pvMediaFrameSourceGroup.StartAsync();
                _mediaFrameSourceGroupsStarted = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("MediaFrameSourceGroup StartAsync failed: " + ex);
                _detecting = false;
                return;
            }

            try
            {
                _unityCoordinateSystem =
                    Microsoft.MixedReality.OpenXR.PerceptionInterop.GetSceneCoordinateSystem(UnityEngine.Pose.identity)
                    as SpatialCoordinateSystem;

                if (_unityCoordinateSystem == null)
                {
                    throw new Exception("GetSceneCoordinateSystem returned null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to get Unity SpatialCoordinateSystem: " + ex);
                await StopHoloLensMediaFrameSourceGroup();
                _detecting = false;
                return;
            }

            try
            {
                await _pvMediaFrameSourceGroup.StartArUcoMarkerTrackerAsync(
                    markerSize,
                    (int)arUcoDictionaryName,
                    _unityCoordinateSystem);
            }
            catch (Exception ex)
            {
                Debug.LogError("StartArUcoMarkerTrackerAsync failed: " + ex);
                await StopHoloLensMediaFrameSourceGroup();
                _detecting = false;
            }
#endif
        }

#if WINDOWS_UWP
        private void UpdateArUcoDetections(IList<DetectedArUcoMarker> detections)
        {
            if (!_mediaFrameSourceGroupsStarted || _pvMediaFrameSourceGroup == null || detections == null)
            {
                return;
            }

            foreach (var detectedMarker in detections)
            {
                Matrix4x4 transformUnityWorld = GetMarkerWorldTransform(detectedMarker);

                Marker marker = new Marker(
                    detectedMarker.Id,
                    CvUtils.GetVectorFromMatrix(transformUnityWorld),
                    CvUtils.GetQuatFromMatrix(transformUnityWorld));

                if (!_markerObservations.ContainsKey(detectedMarker.Id))
                {
                    _markerObservations.Add(detectedMarker.Id, new List<Marker>());
                }

                if (_additionalDetectionCompletionStrategy != null &&
                    !_additionalMarkerObservations.ContainsKey(detectedMarker.Id))
                {
                    _additionalMarkerObservations.Add(detectedMarker.Id, new List<Marker>());
                }

                _markerObservations[detectedMarker.Id].Add(marker);

                if (_additionalDetectionCompletionStrategy != null)
                {
                    _additionalMarkerObservations[detectedMarker.Id].Add(marker);
                }

                if (_detectionCompletionStrategy != null &&
                    _markerObservations[detectedMarker.Id].Count > _detectionCompletionStrategy.MaximumMarkerSampleCount)
                {
                    _markerObservations[detectedMarker.Id].RemoveAt(0);
                }

                if (_additionalDetectionCompletionStrategy != null &&
                    _additionalMarkerObservations[detectedMarker.Id].Count > _additionalDetectionCompletionStrategy.MaximumMarkerSampleCount)
                {
                    _additionalMarkerObservations[detectedMarker.Id].RemoveAt(0);
                }
            }

            Dictionary<int, Marker> validMarkers = new Dictionary<int, Marker>();

            foreach (var observationPair in _markerObservations)
            {
                if (_detectionCompletionStrategy.TryCompleteDetection(observationPair.Value, out Marker completedMarker))
                {
                    validMarkers[completedMarker.Id] = completedMarker;
                    observationPair.Value.Clear();
                }
            }

            if (validMarkers.Count > 0)
            {
                _nextMarkerUpdate = validMarkers;
            }

            if (_additionalDetectionCompletionStrategy != null)
            {
                Dictionary<int, Marker> additionalValidMarkers = new Dictionary<int, Marker>();

                foreach (var observationPair in _additionalMarkerObservations)
                {
                    if (_additionalDetectionCompletionStrategy.TryCompleteDetection(observationPair.Value, out Marker completedMarker))
                    {
                        additionalValidMarkers[completedMarker.Id] = completedMarker;
                        observationPair.Value.Clear();
                    }
                }

                if (additionalValidMarkers.Count > 0)
                {
                    _additionalNextMarkerUpdate = additionalValidMarkers;
                }
            }
        }

        //private Matrix4x4 GetMarkerWorldTransform(DetectedArUcoMarker detectedMarker)
        //{
        //    Vector3 position = CvUtils.Vec3FromFloat3(detectedMarker.Position);
        //    /*//position = new Vector3(
        //    //    position.z,
        //    //    position.y,
        //    //    position.x
        //    //);
        //    position.z *= -1f;*/

        //    //position = new Vector3(
        //    //    position.z,
        //    //    position.y,
        //    //    position.x
        //    //);
        //    //position.x *= -1f;
        //    position.y *= -1f;
        //    //position.z *= -1f;

        //    Quaternion rotation =
        //        CvUtils.RotationQuatFromRodrigues(
        //            CvUtils.Vec3FromFloat3(detectedMarker.Rotation));

        //    Matrix4x4 cameraToWorldUnity =
        //        CvUtils.Mat4x4FromFloat4x4(detectedMarker.CameraToWorldUnity);

        //    Matrix4x4 transformUnityCamera =
        //        CvUtils.TransformInUnitySpace(position, rotation);

        //    return cameraToWorldUnity * transformUnityCamera;
        //}
        private Matrix4x4 GetMarkerWorldTransform(DetectedArUcoMarker detectedMarker) 
        { 
            Vector3 position = CvUtils.Vec3FromFloat3(detectedMarker.Position); 
            position.y *= -1f; 
            
            Quaternion rotation = 
                CvUtils.RotationQuatFromRodrigues( 
                    CvUtils.Vec3FromFloat3(detectedMarker.Rotation)); 
            
            Matrix4x4 cameraToWorldUnity = 
                CvUtils.Mat4x4FromFloat4x4(
                    detectedMarker.CameraToWorldUnity); 
            
            Matrix4x4 transformUnityCamera = 
                CvUtils.TransformInUnitySpace(position, rotation); 
            
            return cameraToWorldUnity * transformUnityCamera; 
        }
#endif

        private async Task StopHoloLensMediaFrameSourceGroup()
        {
#if WINDOWS_UWP
            if (!_mediaFrameSourceGroupsStarted || _pvMediaFrameSourceGroup == null)
            {
                return;
            }

            try
            {
                await _pvMediaFrameSourceGroup.StopAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to stop MediaFrameSourceGroup: " + ex);
            }

            _pvMediaFrameSourceGroup = null;
            _sensorFrameStreamerPv = null;
            _mediaFrameSourceGroupsStarted = false;
#endif
        }

        public delegate void MarkersUpdatedHandler(Dictionary<int, Marker> markers);

        public enum MarkerPositionBehavior
        {
            Stationary,
            Moving
        }

        [Serializable]
        public class Marker
        {
            public int Id;
            public Vector3 Position;
            public Quaternion Rotation;

            private StringBuilder _stringBuilder;

            public Marker(int id, Vector3 position, Quaternion rotation)
            {
                Id = id;
                Position = position;
                Rotation = rotation;
            }

            public override string ToString()
            {
                if (_stringBuilder == null)
                {
                    _stringBuilder = new StringBuilder();
                }
                else
                {
                    _stringBuilder.Clear();
                }

                _stringBuilder.Append(Id);
                _stringBuilder.Append(": ");
                _stringBuilder.Append(Position.x);
                _stringBuilder.Append(", ");
                _stringBuilder.Append(Position.y);
                _stringBuilder.Append(", ");
                _stringBuilder.Append(Position.z);
                _stringBuilder.Append(" ");
                _stringBuilder.Append(Rotation.x);
                _stringBuilder.Append(", ");
                _stringBuilder.Append(Rotation.y);
                _stringBuilder.Append(", ");
                _stringBuilder.Append(Rotation.z);
                _stringBuilder.Append(", ");
                _stringBuilder.Append(Rotation.w);

                return _stringBuilder.ToString();
            }
        }

        private void UpdateDetectionCompletionStrategy()
        {
            _additionalDetectionCompletionStrategy = null;

            if (MarkerPositionBehaviorProperty == MarkerPositionBehavior.Moving)
            {
                _detectionCompletionStrategy = new MovingMarkerDetectionCompletionStrategy(this);
            }
            else
            {
                _detectionCompletionStrategy = new StationaryMarkerDetectionCompletionStrategy(this);

                if (_additionalMovingDetectionStrategy)
                {
                    _additionalDetectionCompletionStrategy = new MovingMarkerDetectionCompletionStrategy(this);
                }
            }
        }

        private static void LogMessagesAboutMarker(
            string markerState,
            IReadOnlyList<Marker> allMarkers,
            IReadOnlyList<Marker> inlierMarkers,
            Marker averageMarker,
            Marker averageInlierMarker)
        {
            double positionStandardDeviation =
                StandardDeviation(allMarkers, averageMarker, marker => (marker.Position - averageMarker.Position).magnitude);

            double rotationStandardDeviation =
                StandardDeviation(allMarkers, averageMarker, marker => Quaternion.Angle(marker.Rotation, averageMarker.Rotation));

            double inlierPositionStandardDeviation =
                StandardDeviation(inlierMarkers, averageInlierMarker, marker => (marker.Position - averageInlierMarker.Position).magnitude);

            double inlierRotationStandardDeviation =
                StandardDeviation(inlierMarkers, averageInlierMarker, marker => Quaternion.Angle(marker.Rotation, averageInlierMarker.Rotation));

            Debug.Log(
                $"Calculated {markerState} marker position with {inlierMarkers.Count} markers out of {allMarkers.Count}. " +
                $"Initial std dev pos={positionStandardDeviation}, rot={rotationStandardDeviation}. " +
                $"After outliers std dev pos={inlierPositionStandardDeviation}, rot={inlierRotationStandardDeviation}. " +
                $"Final position={averageInlierMarker.Position}.");
        }

        private static Marker CalculateAverageMarker(IReadOnlyList<Marker> markers)
        {
            float count = markers.Count;
            Vector3 averagePos = Vector3.zero;
            int id = -1;
            List<Quaternion> rotations = new List<Quaternion>();

            foreach (var marker in markers)
            {
                averagePos += marker.Position / count;
                rotations.Add(marker.Rotation);
                id = marker.Id;
            }

            Quaternion averageRot = CalculateAverageQuaternion(rotations.ToArray());
            return new Marker(id, averagePos, averageRot);
        }

        private static Quaternion CalculateAverageQuaternion(Quaternion[] quaternions)
        {
            Quaternion mean = quaternions[0];

            for (int i = 1; i < quaternions.Length; i++)
            {
                float weight = 1.0f / (i + 1);
                mean = Quaternion.Slerp(mean, quaternions[i], weight);
            }

            return mean;
        }

        private static List<Marker> CalculateInlierMarkerSet(
            IReadOnlyList<Marker> allMarkers,
            Marker averageMarker,
            double markerInlierStandardDeviationThreshold)
        {
            CalculateStandardDeviations(
                allMarkers,
                averageMarker,
                out double positionStandardDeviation,
                out double rotationStandardDeviation);

            List<Marker> inliers = new List<Marker>(allMarkers.Count);

            for (int i = 0; i < allMarkers.Count; i++)
            {
                if (IsMarkerInlier(
                    allMarkers[i],
                    averageMarker,
                    positionStandardDeviation,
                    rotationStandardDeviation,
                    markerInlierStandardDeviationThreshold))
                {
                    inliers.Add(allMarkers[i]);
                }
            }

            return inliers;
        }

        private static void CalculateStandardDeviations(
            IReadOnlyList<Marker> allMarkers,
            Marker averageMarker,
            out double positionStandardDeviation,
            out double rotationStandardDeviation)
        {
            positionStandardDeviation =
                StandardDeviation(allMarkers, averageMarker, marker => (marker.Position - averageMarker.Position).magnitude);

            rotationStandardDeviation =
                StandardDeviation(allMarkers, averageMarker, marker => Quaternion.Angle(marker.Rotation, averageMarker.Rotation));
        }

        private static bool IsMarkerInlier(
            Marker candidate,
            Marker averageMarker,
            double positionStandardDeviation,
            double rotationStandardDeviation,
            double markerInlierStandardDeviationThreshold)
        {
            return
                (candidate.Position - averageMarker.Position).magnitude <
                markerInlierStandardDeviationThreshold * positionStandardDeviation
                &&
                Quaternion.Angle(candidate.Rotation, averageMarker.Rotation) <
                markerInlierStandardDeviationThreshold * rotationStandardDeviation;
        }

        private static double StandardDeviation<T>(
            IReadOnlyList<T> values,
            T meanValue,
            Func<T, double> evaluator)
        {
            double sum = 0;
            double meanValueDouble = evaluator(meanValue);

            for (int i = 0; i < values.Count; i++)
            {
                double delta = evaluator(values[i]) - meanValueDouble;
                sum += delta * delta;
            }

            return Math.Sqrt(sum / values.Count);
        }

        private abstract class MarkerDetectionCompletionStrategy
        {
            public abstract bool TryCompleteDetection(IReadOnlyList<Marker> markers, out Marker completedMarker);
            public abstract int MaximumMarkerSampleCount { get; }
        }

        private sealed class StationaryMarkerDetectionCompletionStrategy : MarkerDetectionCompletionStrategy
        {
            private readonly ArUcoMarkerDetection detector;

            public StationaryMarkerDetectionCompletionStrategy(ArUcoMarkerDetection detector)
            {
                this.detector = detector;
            }

            public override int MaximumMarkerSampleCount => detector.MaximumMarkerSampleCount;

            public override bool TryCompleteDetection(IReadOnlyList<Marker> markers, out Marker completedMarker)
            {
                if (markers.Count >= detector.RequiredObservations)
                {
                    Marker averageMarker = CalculateAverageMarker(markers);

                    List<Marker> inliers =
                        CalculateInlierMarkerSet(
                            markers,
                            averageMarker,
                            detector.MarkerInlierStandardDeviationThreshold);

                    if (inliers.Count >= detector.RequiredInlierCount)
                    {
                        Marker averageInlierMarker = CalculateAverageMarker(inliers);

                        CalculateStandardDeviations(
                            inliers,
                            averageInlierMarker,
                            out double positionStandardDeviation,
                            out double rotationStandardDeviation);

                        if (positionStandardDeviation <= detector.MaximumPositionDistanceStandardDeviation &&
                            rotationStandardDeviation <= detector.MaximumRotationAngleStandardDeviation)
                        {
                            completedMarker = averageInlierMarker;

                            LogMessagesAboutMarker(
                                "final",
                                markers,
                                inliers,
                                averageMarker,
                                averageInlierMarker);

                            return true;
                        }

                        //LogMessagesAboutMarker(
                        //    "rejected",
                        //    markers,
                        //    inliers,
                        //    averageMarker,
                        //    averageInlierMarker);
                    }
                }

                completedMarker = null;
                return false;
            }
        }

        private sealed class MovingMarkerDetectionCompletionStrategy : MarkerDetectionCompletionStrategy
        {
            private readonly ArUcoMarkerDetection detector;

            public MovingMarkerDetectionCompletionStrategy(ArUcoMarkerDetection detector)
            {
                this.detector = detector;
            }

            public override int MaximumMarkerSampleCount => detector.RequiredObservations;

            public override bool TryCompleteDetection(IReadOnlyList<Marker> markers, out Marker completedMarker)
            {
                if (markers.Count >= detector.RequiredObservations)
                {
                    completedMarker = CalculateAverageMarker(markers);
                    return true;
                }

                completedMarker = null;
                return false;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ArUcoMarkerDetection))]
        public class ArUcoMarkerDetectionEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                ArUcoMarkerDetection detector = target as ArUcoMarkerDetection;
                if (detector == null)
                {
                    return;
                }

                detector.RequiredObservations =
                    EditorGUILayout.IntField("Required Observations", detector.RequiredObservations);

                if (detector.MarkerPositionBehaviorProperty == MarkerPositionBehavior.Stationary)
                {
                    detector.RequiredInlierCount =
                        EditorGUILayout.IntField("Required Inlier Count", detector.RequiredInlierCount);

                    detector.MaximumMarkerSampleCount =
                        EditorGUILayout.IntField("Maximum Marker Sample Count", detector.MaximumMarkerSampleCount);

                    detector.MarkerInlierStandardDeviationThreshold =
                        EditorGUILayout.FloatField("Marker Inlier Standard Deviation Threshold", detector.MarkerInlierStandardDeviationThreshold);

                    detector.MaximumPositionDistanceStandardDeviation =
                        EditorGUILayout.FloatField("Maximum Position Distance Standard Deviation", detector.MaximumPositionDistanceStandardDeviation);

                    detector.MaximumRotationAngleStandardDeviation =
                        EditorGUILayout.FloatField("Maximum Rotation Angle Standard Deviation", detector.MaximumRotationAngleStandardDeviation);
                }
            }
        }
#endif
    }
}