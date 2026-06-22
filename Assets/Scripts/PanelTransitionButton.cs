using Microsoft.MixedReality.WorldLocking.Core;
using MixedReality.Toolkit.SpatialManipulation;
using System.Collections;
using UnityEngine;

public class PanelTransitionButton : MonoBehaviour
{
    [SerializeField] private GameObject sourcePanel;
    [SerializeField] private GameObject targetPanel;

    //[SerializeField] private SpacePin pin;

    public void OpenTargetPanel()
    {
        //StartCoroutine(OpenPanelRoutine());

        targetPanel.SetActive(true);
        sourcePanel.SetActive(false);

    }

    //public void showNamePin()
    //{
    //    Debug.Log("spacePin name = " + pin.AnchorId.ToString());
    //}

    //private IEnumerator OpenPanelRoutine()
    //{
    //    targetPanel.SetActive(true);

    //    yield return null;

    //    //Transform sourceRoot = sourcePanel.GetComponentInChildren<Follow>()?.transform;

    //    //Vector3 pos = sourceRoot != null
    //    //    ? sourceRoot.position
    //    //    : sourcePanel.transform.position;

    //    //Quaternion rot = sourceRoot != null
    //    //    ? sourceRoot.rotation
    //    //    : sourcePanel.transform.rotation;

    //    //targetPanel.transform.position = pos;
    //    //targetPanel.transform.rotation = rot;

    //    sourcePanel.SetActive(false);
    //}
}