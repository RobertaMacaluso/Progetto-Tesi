using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.XR.ARSubsystems.XRCpuImage;
using System.Globalization;

public class WorldOriginBootstrap : MonoBehaviour
{
    public APIService apiService;
    public Transform GlobalRoot;

    private WorldOrigin currentOrigin;
    private bool hasOrigin = false;

    void Awake()
    {
        apiService = new APIService();
    }

    async void Start()
    {
        currentOrigin = await apiService.GetWorldOrigin();

        if (currentOrigin != null)
        {
            ApplyWorldOrigin(currentOrigin);
            hasOrigin = true;
        }
        else
        {
            Debug.Log("Waiting for ArUco...");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ApplyWorldOrigin(WorldOrigin origin)
    {
        Debug.Log("ApplyWorldOrigin");

        Vector3 pos = ParseVector3(origin.position);
        Quaternion rot = ParseQuaternion(origin.rotation);

        GlobalRoot.position = pos;
        GlobalRoot.rotation = rot;
    }

    public bool GetHasOrigin()
        { return hasOrigin; }

    public void SetHasOrigin(bool value)
        { hasOrigin = value; }

    Vector3 ParseVector3(string s)
    {
        string[] split = s.Split('_');

        return new Vector3(
            float.Parse(split[0], CultureInfo.InvariantCulture),
            float.Parse(split[1], CultureInfo.InvariantCulture),
            float.Parse(split[2], CultureInfo.InvariantCulture)
        );
    }

    Quaternion ParseQuaternion(string s)
    {
        string[] split = s.Split('_');

        return new Quaternion(
            float.Parse(split[0], CultureInfo.InvariantCulture),
            float.Parse(split[1], CultureInfo.InvariantCulture),
            float.Parse(split[2], CultureInfo.InvariantCulture),
            float.Parse(split[3], CultureInfo.InvariantCulture)
        );
    }
}
