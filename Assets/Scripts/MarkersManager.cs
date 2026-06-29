using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR.OpenXR.Input;

public class MarkersManager : MonoBehaviour
{
    Dictionary<int, GameObject> markersCreated = new Dictionary<int, GameObject>();

    [SerializeField] private GameObject cubePlaceHolder;
    [SerializeField] private GameObject globalRoot;

    // Start is called before the first frame update
    void Start()
    {
        //PlayerPrefs.SetString("Marker_1", "3_3_3/45_45_45_1");
        //PlayerPrefs.DeleteKey("Marker_1");
        //PlayerPrefs.SetString("Marker_1", "0_0_0/0_0_0_1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void SpawnMarkers(StorageContainerView storageELement)
    {
        GameObject newMarker = new GameObject("Marker_" + storageELement.data.markerId.ToString());
        newMarker.transform.SetParent(this.gameObject.transform, false);
        newMarker.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        markersCreated.Add(storageELement.data.markerId, newMarker);

        string pose = PlayerPrefs.GetString(newMarker.name);
        if (!string.IsNullOrEmpty(pose))
        {
            Debug.Log("PlayerPrefs di Marker_" + storageELement.data.markerId.ToString());
            SetTransformMarker(newMarker, pose);
        }

        if (!string.IsNullOrEmpty(storageELement.data.worldTransform))
        {
            SetRoomByMarker(storageELement, newMarker);
        }
        else
        {
            storageELement.transform.SetPositionAndRotation(newMarker.transform.position, newMarker.transform.rotation);
        }

        // constraint del newMarker rispetto alla global root
        //ConstraintSource source = new()
        //{
        //    sourceTransform = globalRoot.transform,
        //    weight = 1f
        //};
        //ParentConstraint pc = newMarker.AddComponent<ParentConstraint>();
        //pc.AddSource(source);
        //pc.enabled = true;
        //pc.constraintActive = true;

        ConstraintSource source = new ConstraintSource
        {
            sourceTransform = globalRoot.transform,
            weight = 1f
        };

        ParentConstraint pc = newMarker.AddComponent<ParentConstraint>();

        int index = pc.AddSource(source);

        // Calcola gli offset
        Vector3 translationOffset =
            Quaternion.Inverse(globalRoot.transform.rotation) *
            (newMarker.transform.position - globalRoot.transform.position);

        Quaternion rotationOffset =
            Quaternion.Inverse(globalRoot.transform.rotation) *
            newMarker.transform.rotation;

        // Assegna gli offset
        pc.SetTranslationOffset(index, translationOffset);
        pc.SetRotationOffset(index, rotationOffset.eulerAngles);

        // Attiva
        pc.locked = true;
        pc.constraintActive = true;

        // cubo per debug
        GameObject cube = Instantiate(cubePlaceHolder, Vector3.zero, Quaternion.identity, newMarker.transform);
        cube.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cube.name = "CubeMarker_" + storageELement.data.markerId.ToString();
    }


    private void SetTransformMarker(GameObject marker, string pose)
    {
        string[] transform = pose.Split('/');

        string[] pos = transform[0].Split('_');
        string[] rot = transform[1].Split('_');

        Debug.Log("Rot_X = " + rot[0].ToString());

        marker.transform.SetPositionAndRotation(
            new Vector3(
                float.Parse(pos[0], CultureInfo.InvariantCulture),
                float.Parse(pos[1], CultureInfo.InvariantCulture),
                float.Parse(pos[2], CultureInfo.InvariantCulture)),
            new Quaternion(
                float.Parse(rot[0], CultureInfo.InvariantCulture),
                float.Parse(rot[1], CultureInfo.InvariantCulture),
                float.Parse(rot[2], CultureInfo.InvariantCulture),
                float.Parse(rot[3], CultureInfo.InvariantCulture)));
    }

    public void SetRoomByMarker(StorageContainerView storageELement, GameObject marker)
    {
        if (string.IsNullOrEmpty(storageELement.data.worldTransform))
        {
            storageELement.gameObject.transform.
                SetPositionAndRotation(marker.transform.position, marker.transform.rotation);

            return;
        }

        string[] transform = storageELement.data.worldTransform.Split('/');

        string[] pos = transform[0].Split('_');
        string[] rot = transform[1].Split('_');

        Vector3 localPos = new Vector3(
                float.Parse(pos[0], CultureInfo.InvariantCulture),
                float.Parse(pos[1], CultureInfo.InvariantCulture),
                float.Parse(pos[2], CultureInfo.InvariantCulture));

        Quaternion localRot = new Quaternion(
            float.Parse(rot[0], CultureInfo.InvariantCulture),
            float.Parse(rot[1], CultureInfo.InvariantCulture),
            float.Parse(rot[2], CultureInfo.InvariantCulture),
            float.Parse(rot[3], CultureInfo.InvariantCulture));

        storageELement.transform.position = marker.transform.TransformPoint(localPos);
        storageELement.transform.rotation = marker.transform.rotation * localRot;
    }

    public GameObject GetMarkerByID(int id)
    {
        if (markersCreated.ContainsKey(id))
            return markersCreated[id];
        else
            return null;
    }    
}
