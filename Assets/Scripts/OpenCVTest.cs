using OpenCvSharp;
using UnityEngine;

public class OpenCVTest : MonoBehaviour
{
    void Start()
    {
        Mat mat = new Mat(
            100,
            100,
            MatType.CV_8UC3
        );

        Debug.Log("OpenCV OK: " + mat.Width);
    }
}