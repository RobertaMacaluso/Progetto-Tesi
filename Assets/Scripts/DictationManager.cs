using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit.UX;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class DictationManager : MonoBehaviour
{
    private DictationSubsystem dictationSubsystem;
    private bool dictating = false;
    //private bool typing = false;
    //private GameObject lastFocused;

    [SerializeField] private MRTKTMPInputField inputField;
    [SerializeField] private GameObject micButtonOff;
    [SerializeField] private GameObject micButtonOn;


    // Start is called before the first frame update
    void Start()
    {
        micButtonOn.SetActive(false);

        dictationSubsystem = XRSubsystemHelpers.GetFirstRunningSubsystem<DictationSubsystem>();
        if (dictationSubsystem != null)
        {
            dictationSubsystem.Recognized += OnDictationResult;
        }

        //inputField.onSelect.AddListener(_ => OnKeyboardOpened());
        //inputField.onDeselect.AddListener(_ => OnKeyboardClosed());
    }

    // Update is called once per frame
    void Update()
    {
        //if (typing)
        //{
        //    GameObject focused = EventSystem.current.currentSelectedGameObject;

        //    if (focused == null || focused == lastFocused)
        //        return;

        //    if (focused != inputField)
        //    {
        //        Debug.Log("Restore focus");
        //        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
        //        inputField.ActivateInputField();
        //        lastFocused = focused;
        //    }
        //}
    }

    private void OnDictationResult(DictationResultEventArgs args)
    {
        Debug.Log("Testo dettato: " + args.Result.ToString());
        if (inputField.text != "")
            inputField.text += " ";

        inputField.text += args.Result.ToString();
        StopDictation();
    }

    public void StartDictation()
    {
        Debug.Log("Inzio dettatura");
        dictationSubsystem.StartDictation();
        dictating = true;
        //StartCoroutine(Dictating());
    }

    public void StopDictation()
    {
        if (dictating)
        {
            Debug.Log("Fine dettatura");
            dictationSubsystem.StopDictation();
            micButtonOn.SetActive(false);
            micButtonOff.SetActive(true);
            //inputField.ReleaseSelection();
            //inputField.DeactivateInputField();
            //StopAllCoroutines(); //altrimenti se viene effettuata una nuova dettatura la coroutine della prima la interrompe prima del dovuto
            dictating = false;
        }
    }

    public void ResetText()
    {
        inputField.text = "";
        StopDictation();
    }

    //IEnumerator Dictating()
    //{
    //    yield return new WaitForSeconds(10f);
    //    StopDictation();
    //}

    //private void OnKeyboardOpened()
    //{
    //    typing = true;
    //}

    //private void OnKeyboardClosed()
    //{
    //    typing = false;
    //}
}
