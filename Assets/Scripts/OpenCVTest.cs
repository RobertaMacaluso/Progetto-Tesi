using OpenCvSharp;
using OpenCvSharp.Aruco;
using UnityEngine;

public class OpenCVTest : MonoBehaviour
{
    WebCamTexture webcam;
    public Transform markerCube;

    private Dictionary dictionary;
    private DetectorParameters parameters;
    private Mat cameraMatrix;
    private Mat distCoeffs;

    void Start()
    {
        //Mat mat = new Mat(
        //    100,
        //    100,
        //    MatType.CV_8UC3
        //);

        //Debug.Log("OpenCV OK: " + mat.Width);

        webcam = new WebCamTexture();
        webcam.Play();

        dictionary = CvAruco.GetPredefinedDictionary(PredefinedDictionaryType.Dict6X6_250);
        parameters = new DetectorParameters();

        // calibrazione temporanea camera
        cameraMatrix = new Mat(3, 3, MatType.CV_64FC1);

        cameraMatrix.Set<double>(0, 0, webcam.width);
        cameraMatrix.Set<double>(0, 1, 0);
        cameraMatrix.Set<double>(0, 2, webcam.width / 2);

        cameraMatrix.Set<double>(1, 0, 0);
        cameraMatrix.Set<double>(1, 1, webcam.width);
        cameraMatrix.Set<double>(1, 2, webcam.height / 2);

        cameraMatrix.Set<double>(2, 0, 0);
        cameraMatrix.Set<double>(2, 1, 0);
        cameraMatrix.Set<double>(2, 2, 1);

        distCoeffs = new Mat(1, 5, MatType.CV_64FC1, Scalar.All(0));

        Debug.Log("ARUCO STARTED");
    }

    void Update()
    {
        // Aspetta che webcam sia pronta
        if (webcam.width <= 16)
            return;

        Color32[] pixels = webcam.GetPixels32();

        byte[] gray = new byte[pixels.Length];

        for (int i = 0; i < pixels.Length; i++)
        {
            gray[i] =
                (byte)(
                    (pixels[i].r +
                     pixels[i].g +
                     pixels[i].b) / 3
                );
        }

        Mat mat = new Mat(
            webcam.height,
            webcam.width,
            MatType.CV_8UC1
        );

        mat.SetArray(gray);

        // Flip immagine
        Cv2.Flip(mat, mat, FlipMode.Y);

        // Detect marker
        Point2f[][] corners;
        int[] ids;
        Point2f[][] rejected;

        CvAruco.DetectMarkers(
            mat,
            dictionary,
            out corners,
            out ids,
            parameters,
            out rejected
        );

        // Se trova marker
        //if (ids != null && ids.Length > 0)
        //{
        //    foreach (int id in ids)
        //    {
        //        Debug.Log("MARKER FOUND: " + id);
        //    }
        //    markerCube.localPosition = 
        //}
        if (ids != null && ids.Length > 0)
        {
            Mat rvecs = new Mat();
            Mat tvecs = new Mat();

            CvAruco.EstimatePoseSingleMarkers(
                corners,
                0.17f,
                cameraMatrix,
                distCoeffs,
                rvecs,
                tvecs
            );

            for (int i = 0; i < ids.Length; i++)
            {
                Vec3d rvec = rvecs.Get<Vec3d>(i);
                Vec3d tvec = tvecs.Get<Vec3d>(i);

                Debug.Log("MARKER FOUND: " + ids[i]);
                //Debug.Log("POSITION: " + tvec);
                //Debug.Log("ROTATION: " + rvec);

                // POSIZIONE
                Vector3 markerPos = new Vector3((float)tvec.Item0, (float)tvec.Item1, (float)tvec.Item2);
                markerCube.localPosition = markerPos;
            }
        }
    }
}