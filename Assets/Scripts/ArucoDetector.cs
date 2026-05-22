//using OpenCvSharp;
//using OpenCvSharp.Aruco;
//using System.Globalization;
//using UnityEngine;
//using static UnityEngine.XR.ARSubsystems.XRCpuImage;

//public class ArucoDetector : MonoBehaviour
//{
//    WebCamTexture webcam;
//    public WorldOriginBootstrap worldOriginBootstrap;
//    public APIService apiService;
//    public Transform globalRoot;

//    private Dictionary dictionary;
//    private DetectorParameters parameters;
//    private Mat cameraMatrix;
//    private Mat distCoeffs;

//    void Awake()
//    {
//        apiService = new APIService();
//    }

//    void Start()
//    {
//        webcam = new WebCamTexture();
//        webcam.Play();

//        dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.Dict6X6_250);

//        parameters = new DetectorParameters();

//        // calibrazione temporanea camera
//        cameraMatrix = new Mat(3, 3, MatType.CV_64FC1);

//        cameraMatrix.Set<double>(0, 0, webcam.width);
//        cameraMatrix.Set<double>(0, 1, 0);
//        cameraMatrix.Set<double>(0, 2, webcam.width / 2);

//        cameraMatrix.Set<double>(1, 0, 0);
//        cameraMatrix.Set<double>(1, 1, webcam.width);
//        cameraMatrix.Set<double>(1, 2, webcam.height / 2);

//        cameraMatrix.Set<double>(2, 0, 0);
//        cameraMatrix.Set<double>(2, 1, 0);
//        cameraMatrix.Set<double>(2, 2, 1);

//        distCoeffs = new Mat(1, 5, MatType.CV_64FC1, Scalar.All(0));
//    }

//    async void Update()
//    {
//        if (!webcam.didUpdateThisFrame)
//            return;

//        if (webcam.width < 100)
//            return;

//        Color32[] pixels = webcam.GetPixels32();

//        byte[] gray = new byte[pixels.Length];

//        for (int i = 0; i < pixels.Length; i++)
//        {
//            gray[i] =
//                (byte)(
//                    (pixels[i].r +
//                     pixels[i].g +
//                     pixels[i].b) / 3
//                );
//        }

//        Mat mat = new Mat(
//            webcam.height,
//            webcam.width,
//            MatType.CV_8UC1
//        );

//        mat.SetArray(gray);

//        //Debug.Log(webcam.width + " x " + webcam.height);


//        Cv2.Flip(mat, mat, FlipMode.Y);

//        // 🔥 ARUCO DETECTION
//        Point2f[][] corners;
//        int[] ids;
//        Point2f[][] rejected;

//        CvAruco.DetectMarkers(
//            mat,
//            dictionary,
//            out corners,
//            out ids,
//            parameters,
//            out rejected
//        );

//        //Debug.Log("rejected: " + rejected.Length);

//        if (ids != null && ids.Length > 0)
//        {
//            Mat rvecs = new Mat();
//            Mat tvecs = new Mat();

//            CvAruco.EstimatePoseSingleMarkers(
//                corners,
//                0.17f,
//                cameraMatrix,
//                distCoeffs,
//                rvecs,
//                tvecs
//            );

//            for (int i = 0; i < ids.Length; i++)
//            {
//                Vec3d rvec = rvecs.Get<Vec3d>(i);
//                Vec3d tvec = tvecs.Get<Vec3d>(i);

//                //Debug.Log("MARKER FOUND: " + ids[i]);
//                //Debug.Log("POSITION: " + tvec);
//                //Debug.Log("ROTATION: " + rvec);

//                // POSIZIONE
//                Vector3 markerPos = new Vector3((float)tvec.Item0, (float)tvec.Item1, (float)tvec.Item2);
//                //globalRoot.localPosition = markerPos;

//                // ROTAZIONE
//                Mat rmat = new Mat();
//                Cv2.Rodrigues(rvec, rmat);

//                Matrix4x4 m = new Matrix4x4();

//                m.m00 = (float)rmat.At<double>(0, 0);
//                m.m01 = (float)rmat.At<double>(0, 1);
//                m.m02 = (float)rmat.At<double>(0, 2);
//                m.m03 = 0;

//                m.m10 = (float)rmat.At<double>(1, 0);
//                m.m11 = (float)rmat.At<double>(1, 1);
//                m.m12 = (float)rmat.At<double>(1, 2);
//                m.m13 = 0;

//                m.m20 = (float)rmat.At<double>(2, 0);
//                m.m21 = (float)rmat.At<double>(2, 1);
//                m.m22 = (float)rmat.At<double>(2, 2);
//                m.m23 = 0;

//                m.m30 = 0;
//                m.m31 = 0;
//                m.m32 = 0;
//                m.m33 = 1;

//                Quaternion rot = QuaternionFromMatrix(m);
//                //globalRoot.localRotation = rot;

//                // ASSEGNAZIONE VALORI A GLOBAL ROOT
//                globalRoot.position = -markerPos;
//                globalRoot.rotation = Quaternion.Inverse(rot);

//                if (!worldOriginBootstrap.GetHasOrigin())
//                {
//                    WorldOrigin dto = new WorldOrigin
//                    {
//                        markerId = ids[i].ToString(),
//                        position = Serialize(globalRoot.position),
//                        rotation = Serialize(globalRoot.rotation)
//                    };

//                    await apiService.SetWorldOrigin(dto);
//                    worldOriginBootstrap.SetHasOrigin(true);
//                }
//            }
//        }
//    }

//    Quaternion QuaternionFromMatrix(Matrix4x4 m)
//    {
//        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
//    }

//    string Serialize(Vector3 v)
//    {
//        return
//            v.x.ToString(CultureInfo.InvariantCulture) + "_" +
//            v.y.ToString(CultureInfo.InvariantCulture) + "_" +
//            v.z.ToString(CultureInfo.InvariantCulture);
//    }

//    string Serialize(Quaternion q)
//    {
//        return
//            q.x.ToString(CultureInfo.InvariantCulture) + "_" +
//            q.y.ToString(CultureInfo.InvariantCulture) + "_" +
//            q.z.ToString(CultureInfo.InvariantCulture) + "_" +
//            q.w.ToString(CultureInfo.InvariantCulture);
//    }
//}