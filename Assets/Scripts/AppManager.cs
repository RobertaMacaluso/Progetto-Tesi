using Microsoft.MixedReality.WorldLocking.Core;
using MixedReality.Toolkit.Examples.Demos;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TMPro;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static Microsoft.MixedReality.GraphicsTools.MeshInstancer;

[System.Serializable]
public struct ArtifactsStruct
{
    public GameObject artifacts;
    public GameObject artifactsPrefab;
    public GameObject artifactsPanel;
    public Transform artifactVirualizedList;
    //public Transform artifactContentTransform;
    public Transform artifactScrollView;
    public PressableButton artifactButtonPrefab;
    public GameObject artifactBackButton;
    public GameObject artifactTitle;
    public GameObject artifactText;
    public GameObject searchGroup;
    public GameObject startNavigationButton;
    public GameObject stopNavigationButton;
    public GameObject artifactTarget;
    public GameObject artifactProp;
    public GameObject artifactIndicator;
    public GameObject solverIndicator;
    public GameObject canvasDistance;
    public GameObject navigationText;
    public GameObject depositButton;
    public GameObject depositInLastShelfButton;
    public GameObject depositList;
    public GameObject depositInShelfButton;
    public GameObject withdrawButton;
    public AudioSource triggerEntered;
    public GameObject[] artifactDepositedUI;
}

public class AppManager : MonoBehaviour
{
    private List<GameObject> shelvesButtonCreated = new();
    private GameObject currentShelvesScrollViewParent = null;
    private GameObject lastShelfPositioned;
    private List<GameObject> allShelves = new();
    private GameObject lastToggledPin;
    private List<GameObject> allArtifacts = new();
    private List<GameObject> artifactsOnList = new();
    private GameObject artifactSelected;
    private Vector3 artifactIndicatorScale;
    private Dictionary<int, GameObject> spawnedArtifacts = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> spawnedShelves = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> rooms = new Dictionary<int, GameObject>();
    private bool artifactScrollViewToBeReset = false;
    private bool shelvesScrollViewToBeReset = false;
    //public readonly string artifactPP = "ArtifactID_";
    public readonly string shelfPP = "ShelfID_";
    private readonly string artifactGeneralText = "Selezionare il reperto a cui si è interessati oppure effettuare una ricerca tramite la barra";
    private readonly string artifactTitle = "Reperto: ";
    private readonly string artifactShelfYes = "Il reperto si trova nello scaffale: ";
    private readonly string artifactShelfNo = "Il reperto non si trova in nessuno scaffale";
    private readonly string artifactShelfLast = "Riposizionare il reperto nell'utlimo scaffale in cui si trovava";
    private readonly string initialDepositText = "Il reperto non è mai stato depositato nel magazzino. Procedere al primo deposito?";
    private readonly string textDeposit = "Deposita reperto in un nuovo scaffale";
    private readonly string initialDepositButtonText = "Deposita reperto in uno scaffale";
    private readonly string artifactNavigation = "Dirigersi verso: ";
    private readonly string[] targetReached = new string[2] {"Reperto raggiunto!", "Scaffale raggiunto. Posizionare il reperto e poi confermare"};
    private readonly List<Transform> currentPath = new();
    private List<Transform> recentPath = new();
    private int step = 0;
    private readonly string indicatorTag = "Target";
    private List<int> currentRoomsID = new List<int>();
    //private readonly List<Transform> depositPath = new();
    private VirtualizedScrollRectListTester vsrltDeposit;
    private TextMeshProUGUI distanceText;
    private MeshRenderer solverRenderer;
    private GameObject distancePlate;
    //private int depositStep = 0;

    [SerializeField] public APIService apiService;
    //[SerializeField] public PanelPositionLock panelPositionLock;
    [SerializeField] public GameObject globalRoot;
    [SerializeField] private GameObject homePanel;
    [SerializeField] private GameObject markerPanel;
    //Shelves panel
    [SerializeField] private GameObject firstText;
    [SerializeField] private GameObject positionButton;
    [SerializeField] private GameObject secondText;
    private readonly string firstTextString = "Scegliere quale elemento posizionare";
    private readonly string firstTextString_2 = "Scegliere se posizionare l'elemento ";
    private readonly string secondTextString = "Oppure navigare tra le sottocategorie di ";
    [SerializeField] private Transform virtualizedList;
    [SerializeField] private GameObject positioningText;
    [SerializeField] private GameObject positioningButton;

    //Warehouse
    [SerializeField] private GameObject warehouse;
    [SerializeField] private MarkersManager markersManager;
    [SerializeField] private GameObject shelfPrefab;
    [SerializeField] public GameObject shelvesListPanel;
    [SerializeField] private PressableButton buttonPrefabShelves;
    [SerializeField] private Transform scrollView;
    [SerializeField] private GameObject returnButton;
    [SerializeField] private GameObject positioningSphere;
    [SerializeField] private GameObject positioningCubeRoom;
    [SerializeField] private GameObject wireframeCubeRoom;
    private bool firstRoom = false;
    [SerializeField] private GameObject sphereIndicator;
    [SerializeField] private GameObject debugCube;

    //Artifacts
    [SerializeField] public ArtifactsStruct A_Menu;
    

    // Start is called before the first frame update
    async void Start()
    {
        
        WorldLockingManager.GetInstance().Load();

        apiService = new APIService();

        SetGlobalRoot();

        markerPanel.SetActive(false);
        shelvesListPanel.SetActive(false);
        positioningSphere.SetActive(false);
        positioningCubeRoom.SetActive(false);
        wireframeCubeRoom.SetActive(false);
        sphereIndicator.SetActive(false);

        PanelsSetting();

        A_Menu.artifactsPanel.SetActive(false);
        A_Menu.artifactBackButton.SetActive(false);
        A_Menu.artifactTarget.SetActive(false);
        A_Menu.artifactProp.SetActive(false);
        A_Menu.artifactIndicator.SetActive(false);
        A_Menu.solverIndicator.SetActive(false);
        //A_Menu.canvasDistance.SetActive(false);
        A_Menu.navigationText.SetActive(false);
        A_Menu.depositButton.SetActive(false);
        A_Menu.depositInShelfButton.SetActive(false);
        A_Menu.depositList.SetActive(false);
        A_Menu.depositInLastShelfButton.SetActive(false);
        A_Menu.withdrawButton.SetActive(false);
        A_Menu.triggerEntered.Stop();

        foreach (var obj in A_Menu.artifactDepositedUI)
        {
            obj.SetActive(false);
        }

        // ================================
        // ORA SCARICA TUTTO WEB SOCKET!!!
        // ================================ 
        // GetAllShelves(warehouse);
        //per ogni scaffale presente nella lista chiamo la funzione per posizionarlo nell'ultima posizione salvata
        //allShelves.ForEach((item) => SetInitialTransform(item));
        
        positioningText.gameObject.SetActive(false);
        positioningButton.transform.parent.gameObject.SetActive(false);

        //Artifacts setup
        //GetAllArtifacts(A_Menu.artifacts);  per artifacts hardcodati

        // ================================
        // ORA SCARICA TUTTO WEB SOCKET!!!
        // ================================ 
        /*List<Artifact> artifactsServer = await apiService.GetAllArtifactsAsync();
        if (artifactsServer == null)
        {
            Debug.Log("Nessun artifact ricevuto");
            return;
        }

        SpawnArtifacts(artifactsServer);
        GetAllArtifacts(A_Menu.artifacts);*/

        vsrltDeposit = A_Menu.depositList.GetComponentInChildren<VirtualizedScrollRectListTester>();
        artifactIndicatorScale = A_Menu.artifactIndicator.transform.localScale;
        distanceText = A_Menu.canvasDistance.GetComponentInChildren<TextMeshProUGUI>();
        solverRenderer = A_Menu.solverIndicator.GetComponent<MeshRenderer>();
        distancePlate = A_Menu.canvasDistance.transform.GetChild(1).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (A_Menu.canvasDistance.activeSelf)
        {
            DirectionalIndicator directionalIndicator = A_Menu.solverIndicator.GetComponent<DirectionalIndicator>();
            float distance = Vector3.Distance(A_Menu.artifactTarget.transform.position, this.gameObject.transform.position);
            int d = Mathf.FloorToInt(distance);
            if (d > 1)
            {
                distanceText.text = d.ToSafeString() + "m";

                if (!directionalIndicator.enabled)
                {
                    //solverRenderer.enabled = true;
                    //distancePlate.SetActive(false);
                    A_Menu.canvasDistance.GetComponent<Follow>().enabled = false;
                    directionalIndicator.enabled = true;
                }
            }  
            else
            {
                distanceText.text = "<1m";

                if (directionalIndicator.enabled)
                {
                    solverRenderer.enabled = false;
                    distancePlate.SetActive(true);
                    A_Menu.canvasDistance.GetComponent<Follow>().enabled = true;
                    directionalIndicator.enabled = false;
                }
            }
            
            //if (!directionalIndicator.enabled)
            //{ return; }

            if (solverRenderer.enabled == true && distancePlate.activeSelf) //solverRenderer.enabled == true && 
            {
                distancePlate.SetActive(false);
                A_Menu.canvasDistance.GetComponent<Follow>().enabled = false;
                A_Menu.canvasDistance.GetComponent<ParentConstraint>().enabled = true;
                A_Menu.canvasDistance.transform.localPosition = Vector3.zero;
                A_Menu.canvasDistance.transform.localScale = Vector3.one * 0.02f;
                //Debug.Log("Scala 0.02");
            }

            if (solverRenderer.enabled == false && !distancePlate.activeSelf) //solverRenderer.enabled == false && 
            {
                distancePlate.SetActive(true);
                A_Menu.canvasDistance.GetComponent<Follow>().enabled = true;
                A_Menu.canvasDistance.GetComponent<ParentConstraint>().enabled = false;
            }
        }
    }

    //settaggio velocità e distanze dei vari pannelli
    public void PanelsSetting()
    {
        float speed = 0.3f;
        float minDistance = 0.6f;
        float maxDistance = 1f;
        float defaultDistance = 0.8f;
        float maxDegrees = 60f;

        // pannello home
        Follow followHome = homePanel.GetComponentInChildren<Follow>();
        followHome.ReorientWhenOutsideParameters = false;
        followHome.MoveLerpTime = speed;
        followHome.RotateLerpTime = speed;
        followHome.MinDistance = minDistance;
        followHome.MaxDistance = maxDistance;
        followHome.DefaultDistance = defaultDistance;
        followHome.MaxViewHorizontalDegrees = maxDegrees;
        followHome.MaxViewVerticalDegrees = maxDegrees;

        // pannello reperti
        Follow followArtifacts = A_Menu.artifactsPanel.GetComponentInChildren<Follow>();
        followArtifacts.ReorientWhenOutsideParameters = false;
        followArtifacts.MoveLerpTime = speed;
        followArtifacts.RotateLerpTime = speed;
        followArtifacts.MinDistance = minDistance;
        followArtifacts.MaxDistance = maxDistance;
        followArtifacts.DefaultDistance = defaultDistance;
        followArtifacts.MaxViewHorizontalDegrees = maxDegrees;
        followArtifacts.MaxViewVerticalDegrees = maxDegrees;

        // pannello scaffali
        Follow followShelves = shelvesListPanel.GetComponentInChildren<Follow>();
        followShelves.ReorientWhenOutsideParameters = false;
        followShelves.MoveLerpTime = speed;
        followShelves.RotateLerpTime = speed;
        followShelves.MinDistance = minDistance;
        followShelves.MaxDistance = maxDistance;
        followShelves.DefaultDistance = defaultDistance;
        followShelves.MaxViewHorizontalDegrees = maxDegrees;
        followShelves.MaxViewVerticalDegrees = maxDegrees;

        // pannello marker
        Follow followMarlker = markerPanel.GetComponentInChildren<Follow>();
        followMarlker.ReorientWhenOutsideParameters = false;
        followMarlker.MoveLerpTime = speed;
        followMarlker.RotateLerpTime = speed;
        followMarlker.MinDistance = minDistance;
        followMarlker.MaxDistance = maxDistance;
        followMarlker.DefaultDistance = defaultDistance;
        followMarlker.MaxViewHorizontalDegrees = maxDegrees;
        followMarlker.MaxViewVerticalDegrees = maxDegrees;
    }

    public void SetGlobalRoot()
    {
        string globalRootTransform = PlayerPrefs.GetString(globalRoot.name);
        if (!string.IsNullOrEmpty(globalRootTransform))
        {
            Debug.Log("SetGlobalRoot");

            string[] transform = globalRootTransform.Split('/');
            string[] localPosition = transform[0].Split('_');
            string[] localRotation = transform[1].Split('_');

            globalRoot.transform.SetPositionAndRotation(
                new Vector3(float.Parse(localPosition[0], CultureInfo.InvariantCulture),
                            float.Parse(localPosition[1], CultureInfo.InvariantCulture),
                            float.Parse(localPosition[2], CultureInfo.InvariantCulture)),
                new Quaternion(float.Parse(localRotation[0], CultureInfo.InvariantCulture),
                               float.Parse(localRotation[1], CultureInfo.InvariantCulture),
                               float.Parse(localRotation[2], CultureInfo.InvariantCulture),
                               float.Parse(localRotation[3], CultureInfo.InvariantCulture)));

            // --- INTEGRAZIONE WORLD LOCKING TOOLS (WLT) ---
            // Recuperiamo lo SpacePin che hai già messo sulla globalRoot nella scena
            SpacePin spacePin = globalRoot.GetComponent<SpacePin>();

            if (spacePin != null)
            {
                // Diciamo al WLT che questa è la posizione teorica ufficiale della root
                spacePin.ResetModelingPose();
                Debug.Log("[WLT] Modeling Pose di GlobalRoot agganciata correttamente.");
            }
            else
            {
                Debug.LogError("Componente SpacePin non trovato sulla globalRoot! Assicurati che sia presente nella scena.");
            }
            // ----------------------------------------------
        }
    }

    //posiziona gli scaffali - chiamata nello start e da ResetShelfPosition
    public void SetInitialTransform(GameObject storageElement)
    {
        StorageContainer data = storageElement.GetComponent<StorageContainerView>().data;

        if (!string.IsNullOrEmpty(data.worldTransform))
        {
            string[] transform = data.worldTransform.Split('/');

            string[] pos = transform[0].Split('_');
            string[] rot = transform[1].Split('_');

            if (data.markerId == -1)
            {
                storageElement.transform.SetLocalPositionAndRotation(
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
            else
            {
                Debug.Log("SetInitialTransfomr room: " + data.markerId.ToString());
                GameObject marker = markersManager.GetMarkerByID(data.markerId);
                if (marker != null) 
                    markersManager.SetRoomByMarker(storageElement.GetComponent<StorageContainerView>(), marker);
            }
          
        }

        if (data.isRoom)
        {
            if (!string.IsNullOrEmpty(data.roomCenterPose))
            {
                string[] roomTransform =
                data.roomCenterPose.Split('/');

                string[] roomPos =
                    roomTransform[0].Split('_');

                storageElement.GetComponent<BoxCollider>().center =
                    new Vector3(
                        float.Parse(roomPos[0], CultureInfo.InvariantCulture),
                        float.Parse(roomPos[1], CultureInfo.InvariantCulture),
                        float.Parse(roomPos[2], CultureInfo.InvariantCulture));
            }
            
            if (data.markerId != -1)
            {
                if (markersManager.GetMarkerByID(data.markerId) == null)
                {
                    Debug.Log("Spawn Marker_" + data.markerId);
                    // spawn del gameObject marker
                    markersManager.SpawnMarkers(storageElement.GetComponent<StorageContainerView>());

                    // aggiunta della room al dizionario
                    if (!rooms.ContainsKey(data.markerId))
                        rooms.Add(data.markerId, storageElement);
                }
                else
                    Debug.Log("Saltato spawn Marker_" + data.markerId);

            }

        }
    }

    //chiamata da SavePositionButton per gestire il salvataggio della posizione dello scaffale ed eventuali scaffali sottostanti
    public void SaveNewPositions()
    {
        WorldLockingManager.GetInstance().Save();
        Debug.Log("salvataggio mondo");

        //SaveTransformObject(lastShelfPositioned);

        firstRoom = true;
        Transform[] allChildren = lastShelfPositioned.GetComponentsInChildren<Transform>();
        foreach (Transform t in allChildren)
        {
            Debug.Log("Saving position of child: " + t.gameObject.name);
            SaveTransformObject(t.gameObject, firstRoom);

            firstRoom=false;
        }

        ResetPanelAfterPositioning();
    }

    //salva la nuova posizione dello scaffale che gli viene passato
    public async void SaveTransformObject(GameObject objectToSave, bool firstRoom)
    {
        if (objectToSave.GetComponent<StorageContainerView>() == null)
        {
            // perché le room hanno all'interno il positioningCubeRoom
            return;
        }

        StorageContainer data = objectToSave.GetComponent<StorageContainerView>().data;
        //data.worldTransform = objectTransformLocal;

        // gestione room
        if (data.isRoom && firstRoom && positioningCubeRoom.activeSelf)
        {
            Debug.Log("Salvataggio room center: " + objectToSave.name);
            
            // 1) rotazione room = rotazione cubo
            objectToSave.transform.rotation =
                positioningCubeRoom.transform.rotation;

            // 2) centro collider = posizione locale cubo
                Vector3 centerInRoomSpace = objectToSave.transform.InverseTransformPoint(positioningCubeRoom.transform.position);

            objectToSave.GetComponent<BoxCollider>().center =
                centerInRoomSpace;

            // 3) salvo solo la posizione locale del cubo
            string cubePositionLocal =
                centerInRoomSpace.x.ToString(CultureInfo.InvariantCulture) + "_" +
                centerInRoomSpace.y.ToString(CultureInfo.InvariantCulture) + "_" +
                centerInRoomSpace.z.ToString(CultureInfo.InvariantCulture);

            data.roomCenterPose =
                cubePositionLocal + "/0_0_0_1";
        }

        // adesso leggo la transform DEFINITIVA dell'elemento
        if (data.isRoom && data.markerId > 0)
        {
            GameObject markerObj = markersManager.GetMarkerByID(data.markerId);
            if (markerObj != null)
            {
                Vector3 localPos = markerObj.transform.InverseTransformPoint(objectToSave.transform.position);
                Quaternion localRot = Quaternion.Inverse(markerObj.transform.rotation) * objectToSave.transform.rotation;

                string objectPositionLocal =
                localPos.x.ToString(CultureInfo.InvariantCulture) + "_" +
                localPos.y.ToString(CultureInfo.InvariantCulture) + "_" +
                localPos.z.ToString(CultureInfo.InvariantCulture);

                string objectRotationLocal =
                    localRot.x.ToString(CultureInfo.InvariantCulture) + "_" +
                    localRot.y.ToString(CultureInfo.InvariantCulture) + "_" +
                    localRot.z.ToString(CultureInfo.InvariantCulture) + "_" +
                    localRot.w.ToString(CultureInfo.InvariantCulture);

                data.worldTransform =
                    objectPositionLocal + "/" + objectRotationLocal;

                Debug.Log("Salvataggio room " + objectToSave.name);
            }
        }
        else
        {
            objectToSave.transform.GetLocalPositionAndRotation(
            out var localPos,
            out var localRot);

            string objectPositionLocal =
                localPos.x.ToString(CultureInfo.InvariantCulture) + "_" +
                localPos.y.ToString(CultureInfo.InvariantCulture) + "_" +
                localPos.z.ToString(CultureInfo.InvariantCulture);

            string objectRotationLocal =
                localRot.x.ToString(CultureInfo.InvariantCulture) + "_" +
                localRot.y.ToString(CultureInfo.InvariantCulture) + "_" +
                localRot.z.ToString(CultureInfo.InvariantCulture) + "_" +
                localRot.w.ToString(CultureInfo.InvariantCulture);

            data.worldTransform =
                objectPositionLocal + "/" + objectRotationLocal;

            Debug.Log("Salvataggio elemento " + objectToSave.name);
        }
        

        //Debug.Log("Salvataggio " + objectToSave.name + ": " + objectTransformLocal);

        await apiService.UpdateShelf(data);

        if (objectToSave.TryGetComponent<ParentConstraint>(out var parentConstraint))
            Destroy(parentConstraint);
    }

    //chiamata dai bottoni StopPositioningButton, BackButton e CloseButton del pannello per annullare il salvataggio della nuova posizione
    public void ResetShelfPosition()
    {
        //if messo per evitare di resettare tutto quando chiamata da BackButton e CloseButton se non si stava posizionando
        if (positioningSphere.activeSelf)
        {
            ResetPanelAfterPositioning();

            SetInitialTransform(lastShelfPositioned);
        } 
    }

    //spegne l'UI del posizionamento e riaccende quella della navigazione della warehouse
    public void ResetPanelAfterPositioning()
    {
        //string resetText = positioningText.GetComponent<TextMeshProUGUI>().text;
        //string toRemove = " \"" + lastShelfPositioned.name + "\"";
        //positioningText.GetComponent<TextMeshProUGUI>().text = resetText.Replace(toRemove, "", System.StringComparison.OrdinalIgnoreCase);
        positioningText.GetComponent<TextMeshProUGUI>().text = "Posizionare la sfera blu nel punto in cui si desidera salvare";
        positioningText.gameObject.SetActive(false);
        positioningButton.transform.parent.gameObject.SetActive(false);
        firstText.gameObject.SetActive(true);
        positionButton.transform.parent.gameObject.SetActive(true);
        secondText.gameObject.SetActive(true);
        if (lastShelfPositioned.transform.childCount > 0)
            virtualizedList.gameObject.SetActive(true);

        Destroy(lastShelfPositioned.GetComponent<ParentConstraint>());
        positioningSphere.SetActive(false);
        //ParentConstraint[] parentConstraint = lastShelfPositioned.GetComponents<ParentConstraint>();
        //foreach(var constraint in parentConstraint)
        //    Destroy(constraint);
        positioningCubeRoom.transform.SetParent(null);
        positioningCubeRoom.SetActive(false);
        wireframeCubeRoom.transform.SetParent(null);
        wireframeCubeRoom.SetActive(false);
        Destroy(wireframeCubeRoom.GetComponent<ParentConstraint>());
        sphereIndicator.SetActive(false);
    }

    //aggiunge alla lista allShelves tutti gli elementi che compongono la warehouse
    public void GetAllShelves(GameObject wh)
    {
        Transform[] allChildren = wh.GetComponentsInChildren<Transform>();

        foreach (Transform t in allChildren)
        {
            if (t != wh.transform)
            {
                //Debug.Log(t.name);
                //t.AddComponent<StorageContainerView>();
                allShelves.Add(t.gameObject);
                
                if (t.childCount == 0)
                {
                    if (t.gameObject.TryGetComponent<StorageContainerView>(out var st))
                    {
                        st.data.SetIsShelf(true);
                        //Debug.Log(t.gameObject.name + " isShelf value: " + st.GetIsShelf());
                    }
                }
            }   
        }
        Debug.Log("Storage Container number: " + allShelves.Count);
    }

    // check per settare isShelf dei nuovi scaffali
    public void NewStorageContainerCheckIsShelf(StorageContainerView newShelf)
    {
        // set dei dati


        // se il nuovo scaffale è il primo figlio di un altro elemento questo non deve più essere shelf
        StorageContainerView parentView = newShelf.gameObject.transform.parent.gameObject.GetComponent<StorageContainerView>();
        if (parentView != null)
            parentView.data.SetIsShelf(false);

        if (newShelf.gameObject.transform.childCount == 0)
        {
            newShelf.data.SetIsShelf(true);
        }
    }

    //gestisce come viene popolata la ScrollView degli scaffali con i vari button degli elementi della warehouse
    public void CreateShelvesScrollView(GameObject parent)
    {
       
        currentShelvesScrollViewParent = parent;
        if (shelvesButtonCreated.Count > 0) 
        {
            //ClearScrollView(scrollView);
            shelvesButtonCreated.Clear();
        }

        if (parent.name == warehouse.name)
        {
            secondText.gameObject.SetActive(false);
            firstText.GetComponent<TextMeshProUGUI>().text = firstTextString;
            positionButton.transform.parent.gameObject.SetActive(false);
            virtualizedList.gameObject.SetActive(true);
            returnButton.SetActive(false);
        }
        else
        {
            returnButton.SetActive(true);
            secondText.gameObject.SetActive(true);
            firstText.GetComponent<TextMeshProUGUI>().text = firstTextString_2 + "\"" + lastShelfPositioned.name + "\"";
            positionButton.transform.parent.gameObject.SetActive(true);
            TextMeshProUGUI txt = positionButton.GetComponentInChildren<TextMeshProUGUI>();
            txt.text = "Posiziona " + lastShelfPositioned.name;

            if(parent.transform.childCount > 0)
            {
                secondText.GetComponent<TextMeshProUGUI>().text = secondTextString + "\"" + lastShelfPositioned.name + "\"";
                virtualizedList.gameObject.SetActive(true);
            } 
            else
            {
                secondText.GetComponent<TextMeshProUGUI>().text = "\"" + lastShelfPositioned.name + "\"" + " non possiede altre sottocategorie";
                virtualizedList.gameObject.SetActive(false);
            }
        }

        if (virtualizedList.gameObject.activeSelf)
        {
            /*int i = 0;
            foreach (Transform t in parent.transform)
            {
                PressableButton newButton = Instantiate(buttonPrefabShelves, contentTransform);
                newButton.name = t.name;
                newButton.GetComponentInChildren<TextMeshProUGUI>().text = t.name;
                int index = i;
                newButton.OnClicked.AddListener(() => OnShelfButtonClicked(index));
                shelvesButtonCreated.Add(t.gameObject);

                i++;
                //Debug.Log("Button " + index + " created");
            }

            contentTransform.GetComponentInParent<VirtualizedScrollRectList>().SetItemCount(i);
            contentTransform.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 1f;*/

            /*int i = 0;
            foreach (Transform t in parent.transform)
            {
                shelvesButtonCreated.Add(t.gameObject);
                i++;
                //Debug.Log("Button " + index + " created");
            }*/

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                shelvesButtonCreated.Add(parent.transform.GetChild(i).gameObject);
            }

            shelvesButtonCreated.Sort((x, y) => x.name.CompareTo(y.name));

            VirtualizedScrollRectListTester list = scrollView.GetComponent<VirtualizedScrollRectListTester>();
            if (shelvesScrollViewToBeReset)
            {
                list.SetScrollView();
                shelvesScrollViewToBeReset = false;
            }
            list.SetWords(shelvesButtonCreated);
        } 
    }

    //chiamata quando si clicca su un button della ScrollView
    public void OnShelfButtonClicked(int index)
    {
        //Debug.Log("Button " + index + " clicked");
        
        lastShelfPositioned = shelvesButtonCreated[index];
        CreateShelvesScrollView(shelvesButtonCreated[index]);
    }

    //svuota la scrollView prima di ripopolarla
    public void ClearScrollView(Transform content)
    {
        if (content.transform.childCount > 0)
        {
            foreach (Transform t in content.transform)
                Destroy(t.gameObject);
        }
    }

    //ripopola la ScrollView tornando indietro di un livello nella gerarchia di warehouse
    public void BackButtonShelves()
    {
        if (positioningSphere.activeSelf)
            ResetShelfPosition();
        
        lastShelfPositioned = lastShelfPositioned.transform.parent.gameObject;
        CreateShelvesScrollView(lastShelfPositioned);
    }

    //chiamata quando si clicca sul button per posizionare un elemento della warehouse
    public void StartPositioning()
    {
        positioningSphere.SetActive(true);
        sphereIndicator.SetActive(true);
        positioningText.GetComponent<TextMeshProUGUI>().text += " \"" + lastShelfPositioned.name + "\"";
        positioningText.gameObject.SetActive(true);
        positioningButton.transform.parent.gameObject.SetActive(true);
        positioningSphere.transform.position = lastShelfPositioned.transform.position;
        //positioningSphere.transform.rotation = lastShelfPositioned.transform.rotation;

        StorageContainer data = lastShelfPositioned.GetComponent<StorageContainerView>().data;
        if (data.isRoom)
        {
            positioningCubeRoom.transform.SetParent(positioningSphere.transform, true);
            positioningCubeRoom.SetActive(true);

            wireframeCubeRoom.transform.SetParent(positioningSphere.transform);
            wireframeCubeRoom.SetActive(true);

            if (!string.IsNullOrEmpty(data.roomCenterPose))
            {
                BoxCollider box = lastShelfPositioned.GetComponent<BoxCollider>();

                positioningCubeRoom.transform.position =
                    lastShelfPositioned.transform.TransformPoint(box.center);
            }
            else
            {
                positioningCubeRoom.transform.position = lastShelfPositioned.transform.position + new Vector3(0.15f, 0f, 0.15f);
                //positioningCubeRoom.transform.rotation = lastShelfPositioned.transform.rotation;
            }
            positioningCubeRoom.transform.rotation = lastShelfPositioned.transform.rotation;
            wireframeCubeRoom.transform.SetPositionAndRotation(positioningCubeRoom.transform.position, positioningCubeRoom.transform.rotation);


            ConstraintSource cubeSource = new()
            {
                sourceTransform = positioningCubeRoom.transform,
                weight = 1f
            };
            ParentConstraint pcCube = wireframeCubeRoom.AddComponent<ParentConstraint>();
            pcCube.AddSource(cubeSource);
            pcCube.enabled = true;
            pcCube.constraintActive = true;

            wireframeCubeRoom.GetComponent<DrawRoomOutline>()
                .UpdateRoomDimensions(lastShelfPositioned.GetComponent<BoxCollider>().size);

            positioningText.GetComponent<TextMeshProUGUI>().text = "Posizionare la sfera blu nel punto in cui si desidera salvare l'entrata di \"" +
                lastShelfPositioned.name + "\" \n" +
                "e il cubo verde al centro della stanza.";
        }

        ConstraintSource sphereSource = new()
        {
            sourceTransform = positioningSphere.transform,
            weight = 1f
        };
        ParentConstraint pc = lastShelfPositioned.AddComponent<ParentConstraint>();
        pc.AddSource(sphereSource);
        pc.rotationAxis = Axis.None;
        pc.enabled = true;
        pc.constraintActive = true;

        firstText.gameObject.SetActive(false);
        positionButton.transform.parent.gameObject.SetActive(false);
        secondText.gameObject.SetActive(false);
        virtualizedList.gameObject.SetActive(false);
    }


    //cambia lo stato del pin del pannello
    public void PinPanel(GameObject panel)
    {
        Follow follow = panel.GetComponent<Follow>();
        //follow.IgnoreDistanceClamp = !follow.IgnoreDistanceClamp;

        PanelPositionLock panelPositionLock = panel.GetComponent<PanelPositionLock>();
        
        if(panelPositionLock.IsPositionLocked())
        {
            follow.IgnoreDistanceClamp = true;
        }
        else
        {
            follow.IgnoreDistanceClamp = !follow.IgnoreDistanceClamp;
        }

        panelPositionLock.EnableFollow();
        //follow.MaxViewHorizontalDegrees = 60;
        follow.MaxViewVerticalDegrees = 60;
    }

    public void UserPinPanel(GameObject panel)
    {
        Follow follow = panel.GetComponent<Follow>();
        

        // 🔥 FIX: blocca influenza verticale del Follow anche in UserPin
        follow.MaxViewVerticalDegrees = 360f;
        //follow.view = 0f;

        PanelPositionLock panelPositionLock = panel.GetComponent<PanelPositionLock>();
        if (panelPositionLock.IsPositionLocked())
        {
            panelPositionLock.EnableFollow();
            follow.IgnoreDistanceClamp = false;
        }
        else
        {
            panelPositionLock.EnablePositionLock();
            follow.IgnoreDistanceClamp = true;
        }
    }

    //gestisce l'attivazione e disattivazione visiva del pin del pannello
    public void ChangeToggle(GameObject toggle)
    {
        if (lastToggledPin != null && lastToggledPin != toggle)
            lastToggledPin.SetActive(false);

        lastToggledPin = toggle;

        if (toggle.activeSelf)
        {
            toggle.SetActive(false);
            //Debug.Log("toggle disattivo");
        }
            
        else
        {
            toggle.SetActive(true);
            //Debug.Log("toggle attivo");
        }
            
    }

    //resetta la gestione del pin di un pannello nel momento in cui si chiude
    public void ResetPin(GameObject panel)
    {
        Follow follow = panel.GetComponent<Follow>();
        follow.IgnoreDistanceClamp = false;
        follow.MaxViewVerticalDegrees = 60;

        PanelPositionLock panelPositionLock = panel.GetComponent<PanelPositionLock>();
        panelPositionLock.EnableFollow();

        if (lastToggledPin != null)
            lastToggledPin.SetActive(false);
    }

    public void SpawnArtifacts(List<Artifact> artifactsServer)
    {
        Debug.Log("artifactsServer.Count: " + artifactsServer.Count);
        for (int i = 0; i < artifactsServer.Count; i++)
        {
            if (artifactsServer[i] == null)
            {
                Debug.Log("Elemento NULL a index " + i);
                continue;
            }
            Vector3 position = new Vector3(0, 0, 0);

            GameObject obj = Instantiate(A_Menu.artifactsPrefab, position, Quaternion.identity, A_Menu.artifacts.transform);
            //Debug.Log(obj == null ? "obj NULL" : "obj OK");
            //Debug.Log(artifactsServer == null ? "lista NULL" : "lista OK");
            //Debug.Log(artifactsServer[i] == null ? "elemento NULL" : "elemento OK");
            //Debug.Log(artifactsServer[i].name == null ? "name NULL" : "name OK");
            obj.name = artifactsServer[i].name;

            ArtifactView view = obj.GetComponent<ArtifactView>();
            view.SetData(artifactsServer[i]);
            //if (view.data.shelvingUnit != -1)
            //    view.data.lastShelvingUnit = view.data.shelvingUnit;

            spawnedArtifacts.Add(artifactsServer[i].id, obj);
        }
    }

    public void SpawnSingleArtifact(Artifact artifact)
    {
        Debug.Log("SpawnSingleArtifact chiamato");

        if (artifact == null)
        {
            Debug.Log("artifact NULL");
            return;
        }

        if (A_Menu.artifactsPrefab == null)
        {
            Debug.Log("prefab NULL");
            return;
        }

        Vector3 position = new Vector3(0, 0, 0);

        GameObject obj = Instantiate(A_Menu.artifactsPrefab, position, Quaternion.identity, A_Menu.artifacts.transform);
        Debug.Log("Instanziato oggetto");


        ArtifactView view = obj.GetComponent<ArtifactView>();
        if (view == null)
        {
            Debug.Log("ArtifactView mancante!");
            return;
        }
        view.SetData(artifact);
        //if (view.data.shelvingUnit != -1)
        //    view.data.lastShelvingUnit = view.data.shelvingUnit;

        Debug.Log(artifact.name == null ? "name NULL" : "name OK");
        obj.name = artifact.name;

        //aggiunta a lista locale
        allArtifacts.Add(obj);
        allArtifacts.Sort((x, y) => x.name.CompareTo(y.name));
        //SetArtifactsShelf(obj);

        //aggiornamento scrollView
        VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
        list.SetWords(allArtifacts);
        UpdateArtifactList(allArtifacts);
        MRTKTMPInputField text = A_Menu.searchGroup.GetComponentInChildren<MRTKTMPInputField>();
        SearchArtifact(text.gameObject);

        //aggiunta al dizionario
        spawnedArtifacts.Add(artifact.id, obj);
    }

    //Distrugge il gameobject del reperto che è stato eliminato
    public void DeleteArtifact(int id)
    {
        Debug.Log("DELETE artifact: " + id);

        //eliminazione dalla lista
        if (allArtifacts.Contains(spawnedArtifacts[id]))
        {
            foreach (var artifact in allArtifacts)
            {
                if (artifact.name == spawnedArtifacts[id].name)
                {
                    Debug.Log("DELETE from list");
                    allArtifacts.Remove(artifact);

                    //aggiornamento ScrollView
                    VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
                    list.SetWords(allArtifacts);
                    UpdateArtifactList(allArtifacts);

                    break;
                }
            }
        }

        //eliminazione dal daizionario
        if (spawnedArtifacts.ContainsKey(id))
        {
            Destroy(spawnedArtifacts[id]);

            spawnedArtifacts.Remove(id);
        }

        MRTKTMPInputField text = A_Menu.searchGroup.GetComponentInChildren<MRTKTMPInputField>();
        SearchArtifact(text.gameObject);
    }

    //Update di un artifact
    public void UpdateArtifact(Artifact artifact)
    {
        Debug.Log("UPDATE artifact: " + artifact.id);

        if (!spawnedArtifacts.ContainsKey(artifact.id))
        {
            Debug.Log("Artifact non trovato per update");

            return;
        }

        GameObject obj = spawnedArtifacts[artifact.id];
        GameObject objList = new();
        foreach (var item in allArtifacts)
        {
            if (item.GetComponent<ArtifactView>().data.id == artifact.id)
            {
                objList = item;
                break;
            }
        }

        // aggiorna nome GameObject
        obj.name = artifact.name;
        objList.name = artifact.name;

        // aggiorna ArtifactView
        ArtifactView view = obj.GetComponent<ArtifactView>();

        if (view != null)
        {
            view.SetData(artifact);

            //if (view.data.shelvingUnit != -1)
            //    view.data.lastShelvingUnit = view.data.shelvingUnit;
        }

        // aggiornamento ScrollView
        VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
        list.SetWords(allArtifacts);
        UpdateArtifactList(allArtifacts);
    }

    //aggiunge tutti gli elementi che compongono l'empty Artifacts alla lista allArtifacts
    public void GetAllArtifacts(GameObject ar)
    {
        for (int i = 0; i < ar.transform.childCount; i++)
        {
            allArtifacts.Add(ar.transform.GetChild(i).gameObject);
        }

        Debug.Log("Artifacts number: " + allArtifacts.Count);

        allArtifacts.Sort((x,y) => x.name.CompareTo(y.name));
        //allArtifacts.ForEach(x => SetArtifactsShelf(x));
    }

    public async Task RefreshArtifacts()
    {
        Debug.Log("REFRESH ARTIFACTS");

        // distruggi vecchi oggetti
        foreach (var obj in spawnedArtifacts.Values)
        {
            Destroy(obj);
        }

        spawnedArtifacts.Clear();
        allArtifacts.Clear();

        // riscarica dal server
        List<Artifact> artifacts = await apiService.GetAllArtifactsAsync();

        // respawn
        SpawnArtifacts(artifacts);
        GetAllArtifacts(A_Menu.artifacts);

        // reset ScrollView
        if (A_Menu.artifactsPanel.activeSelf && A_Menu.artifactScrollView.gameObject.activeSelf)
        {
            Debug.Log("Reset artifact scroll view");
            VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
            list.SetScrollView();
            list.SetWords(allArtifacts);
            UpdateArtifactList(allArtifacts);
        }
        else
            artifactScrollViewToBeReset = true;
    }

    public async Task RefreshShelves()
    {
        Debug.Log("REFRESH SHELVES");

        // distruggi vecchi scaffali
        foreach (var obj in spawnedShelves.Values)
        {
            Destroy(obj);
        }

        spawnedShelves.Clear();
        allShelves.Clear();
        rooms.Clear();

        // riscarica dal server
        List<StorageContainer> shelves = await apiService.GetShelves();

        // respawn
        SpawnShelves(shelves);
        GetAllShelves(warehouse);

        allShelves.ForEach((item) => SetInitialTransform(item));

        // reset ScrollView
        //if (A_Menu.artifactsPanel.activeSelf && A_Menu.artifactScrollView.gameObject.activeSelf)
        //{
        //    Debug.Log("Reset artifact scroll view");
        //    VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
        //    list.SetScrollView();
        //    list.SetWords(allArtifacts);
        //    UpdateArtifactList(allArtifacts);
        //}
        //else
        //    artifactScrollViewToBeReset = true;

        if (shelvesListPanel.activeSelf && scrollView.gameObject.activeSelf)
        {
            Debug.Log("Reset shelves scroll view");
            VirtualizedScrollRectListTester list = scrollView.GetComponent<VirtualizedScrollRectListTester>();
            list.SetScrollView();
            CreateShelvesScrollView(warehouse);
        }
        else
            shelvesScrollViewToBeReset = true;
    }

    public void SpawnShelves(List<StorageContainer> shelves)
    {
        // FASE 1
        Debug.Log("shelves.Count: " + shelves.Count);

        Vector3 position = new Vector3(0, 0, 0);

        for (int i = 0; i < shelves.Count; i++)
        {
            GameObject obj = Instantiate(shelfPrefab, position, Quaternion.identity, warehouse.transform);

            obj.name = shelves[i].name;

            spawnedShelves.Add(shelves[i].id, obj);
        }

        // FASE 2
        foreach (StorageContainer shelf in shelves)
        {
            GameObject obj = spawnedShelves[shelf.id];

            if (shelf.parentShelfId != -1)
            {
                GameObject parent = spawnedShelves[shelf.parentShelfId];

                obj.transform.SetParent(parent.transform, true);
            }

            //ApplyShelfTransform(obj, shelf.worldTransform);
            obj.GetComponent<StorageContainerView>().data = shelf;

            if (shelf.isRoom)
            {
                BoxCollider col = obj.AddComponent<BoxCollider>();
                col.isTrigger = true;

                Rigidbody rb = obj.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                col.size = new Vector3(
                    shelf.roomWidth,
                    shelf.roomHeight,
                    shelf.roomDepth
                );


                if (!string.IsNullOrEmpty(shelf.roomCenterPose))
                {
                    //Debug.Log("Settaggio iniziale scaffale " + shelf.name);

                    string[] transform = shelf.roomCenterPose.Split('/');
                    string[] localPosition = transform[0].Split('_');
                    string[] localRotation = transform[1].Split('_');

                    //storageElement.transform.SetLocalPositionAndRotation(
                    Vector3 roomCenterWorldPosition = new Vector3(float.Parse(localPosition[0], CultureInfo.InvariantCulture),
                                float.Parse(localPosition[1], CultureInfo.InvariantCulture),
                                float.Parse(localPosition[2], CultureInfo.InvariantCulture));

                    Vector3 localPos = obj.transform.InverseTransformPoint(roomCenterWorldPosition);
                    col.center = localPos;

                    obj.transform.localRotation = new Quaternion(float.Parse(localRotation[0], CultureInfo.InvariantCulture),
                                   float.Parse(localRotation[1], CultureInfo.InvariantCulture),
                                   float.Parse(localRotation[2], CultureInfo.InvariantCulture),
                                   float.Parse(localRotation[3], CultureInfo.InvariantCulture));
                }
            }
        }
    }


    public void SpawnSingleShelf(StorageContainer shelf)
    {
        Debug.Log("SpawnSingleShelf chiamato");

        if (shelf == null)
        {
            Debug.Log("shelf NULL");
            return;
        }

        if (shelfPrefab == null)
        {
            Debug.Log("prefab NULL");
            return;
        }

        Vector3 position = new Vector3(0, 0, 0);

        GameObject obj = Instantiate(shelfPrefab, position, Quaternion.identity, warehouse.transform);
        Debug.Log("Instanziato oggetto");


        StorageContainerView view = obj.GetComponent<StorageContainerView>();
        if (view == null)
        {
            Debug.Log("StorageContainerView mancante!");
            return;
        }
        view.SetData(shelf);
        Debug.Log(shelf.name == null ? "name NULL" : "name OK");
        obj.name = shelf.name;

        //aggiunta a lista locale
        allShelves.Add(obj);
        allShelves.Sort((x, y) => x.name.CompareTo(y.name));

        //aggiunta al dizionario
        spawnedShelves.Add(shelf.id, obj);

        // set del parent
        if (shelf.parentShelfId != -1)
        {
            GameObject parent = spawnedShelves[shelf.parentShelfId];

            obj.transform.SetParent(parent.transform, true);
        }

        
        NewStorageContainerCheckIsShelf(view);
        // CHECK SE è STANZA DA FARE

        //aggiornamento scrollView
        //VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
        //list.SetWords(allArtifacts);
        //UpdateArtifactList(allArtifacts);
        //MRTKTMPInputField text = A_Menu.searchGroup.GetComponentInChildren<MRTKTMPInputField>();
        //SearchArtifact(text.gameObject);
        if (currentShelvesScrollViewParent != null)
            CreateShelvesScrollView(currentShelvesScrollViewParent);
        else
            CreateShelvesScrollView(warehouse);
    }

    // Update di uno shelf
    public void UpdateShelf(StorageContainer shelf)
    {
        Debug.Log("UPDATE shelf: " + shelf.id);

        if (!spawnedShelves.ContainsKey(shelf.id))
        {
            Debug.Log("Shelf non trovato per update");

            return;
        }

        GameObject obj = spawnedShelves[shelf.id];
        GameObject objList = new();
        foreach (var item in allShelves)
        {
            if (item.GetComponent<StorageContainerView>().data.id == shelf.id)
            {
                objList = item;
                break;
            }
        }

        // aggiorna StorageContainerView
        StorageContainerView view = obj.GetComponent<StorageContainerView>();

        if (view != null)
        {
            view.SetData(shelf);
        }

        // aggiorna GameObject
        obj.name = shelf.name;
        objList.name = shelf.name;
        SetInitialTransform(obj);

        NewStorageContainerCheckIsShelf(view);

        // aggiornamento ScrollView
        VirtualizedScrollRectListTester list = scrollView.GetComponent<VirtualizedScrollRectListTester>();
        list.SetWords(allShelves);
        if (currentShelvesScrollViewParent != null)
            CreateShelvesScrollView(currentShelvesScrollViewParent);
        else
            CreateShelvesScrollView(warehouse);
    }


    // =======================================================
    // NON SERVE PIU' PERCHE' PRENDE IL DATO DAL SERVER
    // =======================================================
    /*public void SetArtifactsShelf(GameObject artifact)
    {
        int shelfID = PlayerPrefs.GetInt(artifactPP + artifact.gameObject.GetComponent<ArtifactView>().data.id.ToString());
        //Debug.Log("ShelfID: " +  shelfID);
        if (shelfID != -1)
            artifact.GetComponent<ArtifactView>().data.SetShelfID(shelfID);
    }*/

    //crea la scrollView con i reperti
    public void CreateArtifactScrollView()
    {
        A_Menu.artifactTitle.GetComponent<TextMeshProUGUI>().text = artifactGeneralText;
        A_Menu.artifactVirualizedList.gameObject.SetActive(true);
        A_Menu.searchGroup.SetActive(true);
        A_Menu.artifactBackButton.SetActive(false);
        A_Menu.startNavigationButton.SetActive(false);
        A_Menu.stopNavigationButton.SetActive(false);
        A_Menu.artifactText.SetActive(false);
        A_Menu.navigationText.SetActive(false);
        //artifactSelected = null;

        VirtualizedScrollRectListTester list = A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>();
        if (artifactScrollViewToBeReset)
        {
            list.SetScrollView();
            artifactScrollViewToBeReset = false;
        }
        list.SetWords(allArtifacts);
        UpdateArtifactList(allArtifacts);
    }

    //gestione del pulsante per tornare indietro nelle varie situazioni in cui può essere cliccato
    public void BackButtonArtifact()
    {
        Debug.Log("Deposit List value: " +  vsrltDeposit.GetForDeposit());
        if (!vsrltDeposit.GetForDeposit())
        {
            A_Menu.artifactVirualizedList.gameObject.SetActive(true);
            A_Menu.searchGroup.SetActive(true);
            A_Menu.artifactText.SetActive(false);
            A_Menu.startNavigationButton.SetActive(false);
            A_Menu.artifactBackButton.SetActive(false);

            //if(A_Menu.stopNavigationButton.activeSelf)

            //int i = 0;
            //while (allArtifacts[i] != artifactSelected)
            //{ i++; }
            //OnArtifactButtonClicked(i);

            if (A_Menu.depositList.activeSelf)
            {
                int i = 0;
                while (allArtifacts[i] != artifactSelected)
                { i++; }

                Debug.Log(DateTime.Now.ToString("HH:mm:ss.fff") + ". Deposit list - Attiva");
                OnArtifactButtonClicked(i);
            }
            else
            { 
                artifactSelected = null;
                Debug.Log(DateTime.Now.ToString("HH:mm:ss.fff") + ". Deposit list - Non attiva");
            }
            StopNavigation();
            A_Menu.depositList.SetActive(false);
            
        }
        else
        {
            Debug.Log("Back");
            if (!A_Menu.depositList.activeSelf)
            {
                DepositConfirmed();
                StopNavigation();
                return;
            }
            else
                vsrltDeposit.Back();
        }
    }

    //chiamata quando si clicca sul bottone di un reperto
    public void OnArtifactButtonClicked(int index)
    {
        //Debug.Log("Button clicked: " + index + " - " + artifactsOnList[index].name);

        currentPath.Clear();
        int shelfID = artifactsOnList[index].GetComponent<ArtifactView>().data.GetShelfID();
        A_Menu.artifactVirualizedList.gameObject.SetActive(false);
        this.gameObject.GetComponent<DictationManager>().StopDictation();
        A_Menu.searchGroup.SetActive(false);
        A_Menu.artifactTitle.GetComponent<TextMeshProUGUI>().text = artifactTitle + artifactsOnList[index].name;
        artifactSelected = artifactsOnList[index];
        A_Menu.artifactText.SetActive(true);
        A_Menu.artifactBackButton.SetActive(true);

        if (shelfID != -1)
        {
            A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = artifactShelfYes + spawnedShelves[shelfID].name.ToString();
            A_Menu.startNavigationButton.SetActive(true);
            
            GameObject shelf = FindChildRecursive(warehouse.transform, shelfID);
            CalculatePath(shelf.transform);

            Debug.Log("Path: " + string.Join(" - ", currentPath.Select(go => go.name)));
        }
        else
        {
            A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = artifactShelfNo;
            A_Menu.depositButton.SetActive(true);

            //int lastShelvingUnit = PlayerPrefs.GetInt(artifactPP + artifactsOnList[index].GetComponent<ArtifactView>().data.id.ToString() + "_Last");
            int lastShelvingUnit = artifactsOnList[index].GetComponent<ArtifactView>().data.lastShelvingUnit;
            if (lastShelvingUnit != -1)
            {
                A_Menu.depositButton.GetComponentInChildren<TextMeshProUGUI>().text = textDeposit;
                A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text += ".\n" + artifactShelfLast + " (" + spawnedShelves[lastShelvingUnit].name.ToString() + ")?";
                A_Menu.depositInLastShelfButton.SetActive(true);
            }
            else
            {
                A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = initialDepositText;
                A_Menu.depositButton.GetComponentInChildren<TextMeshProUGUI>().text = initialDepositButtonText;
            }
        }
    }

    public void UpdateArtifactList(List<GameObject> list)
    {
        artifactsOnList.Clear();
        foreach (GameObject go in list)
        {
            //allArtifacts.Find(x => x.name == go.name);
            artifactsOnList.Add(allArtifacts.Find(x => x.name == go.name));
        }
        
        //Debug.Log(string.Join(" - ", artifactsOnList.Select(x => x.name)));
    }

    //trova il gameobject dello scaffale dall'ID salvato nel reperto
    public GameObject FindChildRecursive(Transform parent, int id)
    {
        foreach (Transform child in parent)
        {
            if (child.gameObject.GetComponent<StorageContainerView>().data.id == id)
                return child.gameObject;

            GameObject result = FindChildRecursive(child, id);
            if (result != null)
                return result;
        }
        return null;
    }

    //calcola il tutti gli step del percorso per raggiungere il reperto
    public void CalculatePath(Transform current)
    {
        if (!A_Menu.depositList.activeSelf)
        {
            while (current.gameObject.name != warehouse.name)
            {
                currentPath.Add(current);
                current = current.parent;
            }
            currentPath.Reverse();
        }
        else
        {
            while (current.gameObject.name != warehouse.name)
            {
                currentPath.Add(current);
                current = current.parent;
            }
            currentPath.Reverse();
        }
        
    }

    //inizia la navigazione per portare l'utente al reperto
    public void StartNavigation()
    {
        Debug.Log("Start navigation. Path count = " + currentPath.Count);
        A_Menu.startNavigationButton.SetActive(false);
        A_Menu.stopNavigationButton.SetActive(true);
        A_Menu.solverIndicator.SetActive(true);
        A_Menu.solverIndicator.GetComponent<DirectionalIndicator>().enabled = true;
        //A_Menu.canvasDistance.SetActive(true);
        //A_Menu.canvasDistance.transform.SetParent(A_Menu.solverIndicator.transform);
        A_Menu.artifactTarget.GetComponent<Follow>().enabled = true;
        A_Menu.artifactTarget.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        //se parte del percorso del nuovo reperto è uguale a quello del reperto precedente si saltano quei passaggi
        step = 0;
        
        //if (currentRoomsID.Count > 0)
        //{
        //    SkipSteps();
        //    //return;
        //}

        if (recentPath.Count > 0)
        {
            Debug.Log("recentPath.Count > 0");
            while (step < currentPath.Count && step < recentPath.Count)
            {
                if (currentPath[step].name == recentPath[step].name)
                {
                    Debug.Log("Passaggio saltato. " + currentPath[step].name + " - " + recentPath[step].name);
                    step++;
                }
                else
                {
                    /*for (int i = 0; i < recentPath.Count; i++)
                    {
                        if (i >= step)
                        {
                            Debug.Log("Removing " + recentPath[i].name);
                            recentPath.RemoveAt(i);
                            i--;
                        }
                    }*/
                    if (step < recentPath.Count)
                    {
                        recentPath.RemoveRange(step, recentPath.Count - step);
                    }
                    Debug.Log($"Recent path: {string.Join(", ", recentPath.Select(x => x.name))}");
                    break;
                }
            }

            if (currentPath.Count == recentPath.Count)
            {
                Debug.Log("Step--");
                step--;
                recentPath.RemoveRange(step, recentPath.Count - step);
            }
        }

        if (currentRoomsID.Count > 0)
        {
            SkipSteps();
            //return;
        }

        NextStep();
    }

    private void SkipSteps()
    {
        int index = 0;
        foreach (var localStep in currentPath)
        {
            index++;
            if ( currentRoomsID.Contains(localStep.gameObject.GetComponent<StorageContainerView>().data.id) && step < index)
            {
                step = index;
                Debug.Log("Già nella stanza. Step = " + step);
                //break;
            }
            
        }

        //for (int i = 0; i < index; i++)
        //{
        //    NextStep();
        //}
    }

    //gestione del passaggio del prossimo punto da raggiungere (chiamata anche dal bottone Skip Step nella scena) e del punto di arrivo
    public void NextStep()
    {
        if (step < currentPath.Count)
        {
            A_Menu.artifactTarget.GetComponent<ArtifactIndicator>().SetTargetPosition(currentPath[step]);
            A_Menu.artifactTarget.transform.position = currentPath[step].position;
            Debug.Log("Next step: " + step + " - " + currentPath[step].name);
            A_Menu.solverIndicator.GetComponent<DirectionalIndicator>().enabled = true;
            A_Menu.artifactTarget.SetActive(true);
            A_Menu.navigationText.SetActive(true);
            A_Menu.navigationText.GetComponent<TextMeshProUGUI>().text = artifactNavigation + currentPath[step].name;

            if (currentPath[step].gameObject.TryGetComponent<StorageContainerView>(out var st) )
            {
                if (st.data.GetIsShelf())
                {
                    A_Menu.artifactTarget.GetComponent<Follow>().enabled = false;
                    A_Menu.artifactTarget.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    Debug.Log("Rotazione step = " + step);
                }
                    
            }

            //Ogni volta che si raggiunge la freccia target quel passaggio (che corrisponde a step - 1) viene aggiunto al recentPath.
            //La seconda condizione dell'if serve ad evitare doppioni quando si inizia la navigazione saltando gli step già fatti per l'ultimo reperto !recentPath.Contains(currentPath[step - 1])
            if (step - 1 >= 0 && recentPath.LastOrDefault() != currentPath[step - 1])
                recentPath.Add(currentPath[step - 1]);
            Debug.Log($"Recent path: {string.Join(", ", recentPath.Select(x => x.name))}");
        }
        else
        {
            Debug.Log("Destinazione raggiunta! =)");
            //A_Menu.artifactTarget.SetActive(false);
            //A_Menu.solverIndicator.SetActive(false);
            A_Menu.artifactTarget.GetComponent<Follow>().enabled = false;
            A_Menu.artifactTarget.SetActive(true);

            //nel caso tutti gli step siano stati skippati perché si naviga verso lo stesso scaffale
            A_Menu.artifactTarget.GetComponent<ArtifactIndicator>().SetTargetPosition(currentPath[step-1]);
            A_Menu.artifactTarget.transform.position = currentPath[step-1].position;
            Debug.Log("Step -1 = " + (step - 1));
            
            A_Menu.stopNavigationButton.SetActive(false);
            if (vsrltDeposit.GetForDeposit())
                A_Menu.navigationText.GetComponent<TextMeshProUGUI>().text = targetReached[1];
            else
                A_Menu.navigationText.GetComponent<TextMeshProUGUI>().text = targetReached[0];

            //salvataggio del percorso dell'ultimo reperto raggiunto per saltare eventuali step uguali per il prossimo reperto
            if (recentPath.Count > 0)
                recentPath.Clear();

            foreach (var item in currentPath)
                recentPath.Add(item);

            //VirtualizedScrollRectListTester vsrltDeposit = A_Menu.depositList.GetComponentInChildren<VirtualizedScrollRectListTester>();
            if (!vsrltDeposit.GetForDeposit())
            {
                ArtifactReached();
                //A_Menu.withdrawButton.SetActive(true);
            }
                
            else
            {
                ArtifactPositioning();
                //A_Menu.depositInShelfButton.SetActive(true);
            }
        }

        if (step <= currentPath.Count)
            step++;
    }

    private void ArtifactReached()
    {
        A_Menu.withdrawButton.SetActive(true);
        A_Menu.artifactTarget.SetActive(false);
        A_Menu.solverIndicator.SetActive(false);
        //A_Menu.canvasDistance.SetActive(false);

        // gestione indicatore reperto
        Artifact artifact = artifactSelected.GetComponent<ArtifactView>().data;
        GameObject shelfDeposit = currentPath[currentPath.Count-1].gameObject;

        Debug.Log("ArtifactSelected = " + artifact.name);
        Debug.Log("ShelfDeposit = " + shelfDeposit.name);

        A_Menu.artifactIndicator.SetActive(true);
        //A_Menu.artifactIndicator.GetComponent<Follow>().enabled = false;
        A_Menu.artifactIndicator.transform.SetParent(shelfDeposit.transform);

        if (string.IsNullOrEmpty(artifact.containerLocalPose))
        {
            A_Menu.artifactIndicator.transform.position = shelfDeposit.transform.position;
            //A_Menu.artifactProp.transform.rotation = shelfDeposit.transform.rotation;
        }
        else
        {
            string[] poseParts = artifact.containerLocalPose.Split('/');

            string[] pos = poseParts[0].Split('_');
            string[] rot = poseParts[1].Split('_');

            Vector3 localPosition = new Vector3(
                float.Parse(pos[0], CultureInfo.InvariantCulture),
                float.Parse(pos[1], CultureInfo.InvariantCulture),
                float.Parse(pos[2], CultureInfo.InvariantCulture)
            );

            Quaternion localRotation = new Quaternion(
                float.Parse(rot[0], CultureInfo.InvariantCulture),
                float.Parse(rot[1], CultureInfo.InvariantCulture),
                float.Parse(rot[2], CultureInfo.InvariantCulture),
                float.Parse(rot[3], CultureInfo.InvariantCulture)
            );

            A_Menu.artifactIndicator.transform.position =
                shelfDeposit.transform.TransformPoint(localPosition) + 
                new Vector3(0f, artifact.artifactHeight / 2, 0f);

            A_Menu.artifactIndicator.transform.rotation =
                shelfDeposit.transform.rotation * localRotation;
        }

        // scala dell'indicatore
        if (artifact.artifactWidth == 0 || artifact.artifactHeight == 0 || artifact.artifactDepth == 0)
        {
            A_Menu.artifactIndicator.transform.localScale = artifactIndicatorScale * 6f; // metà della grandezza massima
        }
        else
        {
            //float volume = artifact.artifactWidth * artifact.artifactHeight * artifact.artifactDepth;
            //float scaleFactor = volume * 1000f;
            //Debug.Log("volume - scaleFactor = " + volume + " - " + scaleFactor);
            //A_Menu.artifactIndicator.transform.localScale = artifactIndicatorScale * scaleFactor;


            //float volume = artifact.artifactWidth * artifact.artifactHeight * artifact.artifactDepth;
            //float scaleFactor = Mathf.Pow(volume, 1f / 3f) * 4f;
            //Debug.Log("volume - scaleFactor = " + volume + " - " + scaleFactor);
            //scaleFactor = Mathf.Clamp(scaleFactor, 1f, 12.0f);

            float volume = artifact.artifactWidth * artifact.artifactHeight * artifact.artifactDepth;
            float size = Mathf.Pow(volume, 1f / 3f);

            // mapping fisico → 0..1
            float t = Mathf.InverseLerp(0.2f, 1.85f, size);

            // curva non lineare
            t = Mathf.SmoothStep(0f, 1f, t);

            // output finale
            float scaleFactor = Mathf.Lerp(1f, 12f, t);

            A_Menu.artifactIndicator.transform.localScale =
                artifactIndicatorScale * scaleFactor;
        }
        //A_Menu.artifactIndicator.GetComponent<Follow>().enabled = true;
    }

    // posizionamento del reperto all'interno dello scaffale scelto
    private void ArtifactPositioning()
    {
        A_Menu.artifactTarget.SetActive(false);
        A_Menu.depositInShelfButton.SetActive(true);

        // gestione prop
        Artifact artifact = artifactSelected.GetComponent<ArtifactView>().data;
        GameObject shelfDeposit = vsrltDeposit.GetShelfDeposit();

        A_Menu.artifactProp.SetActive(true);
        A_Menu.artifactProp.transform.position = shelfDeposit.transform.position;
        if (artifact.artifactWidth > 0 && artifact.artifactHeight > 0 && artifact.artifactDepth > 0)
            A_Menu.artifactProp.transform.localScale = new Vector3(artifact.artifactWidth, artifact.artifactHeight, artifact.artifactDepth);
        else
            A_Menu.artifactProp.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        //A_Menu.artifactProp.transform.rotation = shelfDeposit.transform.rotation;
    }

    //quando la freccia target entra nel trigger si passa al punto successivo da raggiungere
    private void OnTriggerEnter(Collider other)
    {
        int tmp = step - 1;
        if (other.gameObject.CompareTag(indicatorTag) && tmp < currentPath.Count)
        {
            A_Menu.artifactTarget.SetActive(false);
            A_Menu.triggerEntered.Play();
            Debug.Log("Collider di " + other.gameObject.name + ". Step = " + step);
            NextStep();
        }

        // gestione per sapere dentro quale stanza sono
        //StorageContainer storageContainer = other.gameObject.TryGetComponent<StorageContainerView>().data;
        other.gameObject.TryGetComponent<StorageContainerView>(out StorageContainerView storageContainerView);

        if (storageContainerView == null)
            return;

        if (storageContainerView.data.isRoom && !currentRoomsID.Contains(storageContainerView.data.id))
        {
            currentRoomsID.Add(storageContainerView.data.id);
            Debug.Log("currentRoomsID = " + string.Join(", ", currentRoomsID));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        other.gameObject.TryGetComponent<StorageContainerView>(out StorageContainerView storageContainerView);
        //StorageContainer storageContainer = other.gameObject.GetComponent<StorageContainerView>().data;

        if (storageContainerView == null)
            return;

        if (storageContainerView.data.isRoom && currentRoomsID.Contains(storageContainerView.data.id))
        {
            currentRoomsID.Remove(storageContainerView.data.id);
            Debug.Log("currentRoomsID = " + string.Join(", ", currentRoomsID));

            // se esco da un trigger del percorso torno indietro negli step
            //if(!A_Menu.artifactTarget.activeSelf)
            //{ return; }

            int index = currentPath.IndexOf(other.gameObject.transform);

            if (index != -1)
            {
                step = index;
                Debug.Log("Step - recentPath.count: " + step + " - " + recentPath.Count);
                if (step < recentPath.Count)
                    recentPath.RemoveRange(index, recentPath.Count - index);

                if (A_Menu.artifactTarget.activeSelf)
                {
                    A_Menu.artifactTarget.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    A_Menu.artifactTarget.GetComponent<Follow>().enabled = true;
                    NextStep();
                }
            }
        }
    }

    //chiamata quando si interrompe la navigazione verso un reperto o uno scaffale, ma anche dalla funzione del back button e dalla X del pannello reperti
    public void StopNavigation()
    {
        A_Menu.artifactTarget.SetActive(false);
        A_Menu.artifactProp.SetActive(false);
        A_Menu.artifactIndicator.SetActive(false);
        A_Menu.solverIndicator.SetActive(false);
        //A_Menu.canvasDistance.SetActive(false);
        A_Menu.artifactIndicator.transform.SetParent(null);
        A_Menu.stopNavigationButton.SetActive(false);
        A_Menu.navigationText.SetActive(false);
        A_Menu.depositButton.SetActive(false);
        A_Menu.depositInLastShelfButton.SetActive(false);
        A_Menu.depositInShelfButton.SetActive(false);
        foreach (var obj in A_Menu.artifactDepositedUI)
        {
            obj.SetActive(false);
        }

        if (vsrltDeposit.GetForDeposit())
        {
            A_Menu.artifactText.SetActive(true);
            A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = artifactShelfNo;
        }
            

        if (A_Menu.artifactVirualizedList.gameObject.activeSelf)
        {
            Debug.Log("Deposit list - navigation if");
            A_Menu.artifactTitle.GetComponent<TextMeshProUGUI>().text = artifactGeneralText;
        }
        else
        {
            if (artifactSelected != null)
            {
                Debug.Log("Deposit list - navigation else");
                int shelfID = artifactSelected.GetComponent<ArtifactView>().data.GetShelfID();

                if (shelfID != -1)
                {
                    A_Menu.startNavigationButton.SetActive(true);
                    Debug.Log("Deposit list - navigation button on");
                }
                else
                {
                    A_Menu.depositButton.SetActive(true);
                    Debug.Log("Deposit list - deposit button on");

                    //int lastShelvingUnit = PlayerPrefs.GetInt(artifactPP + artifactSelected.GetComponent<ArtifactView>().data.id.ToString() + "_Last");
                    int lastShelvingUnit = artifactSelected.GetComponent<ArtifactView>().data.lastShelvingUnit;
                    if (lastShelvingUnit != -1)
                    {
                        A_Menu.depositButton.GetComponentInChildren<TextMeshProUGUI>().text = textDeposit;
                        A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = artifactShelfNo;
                        A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text += ".\n" + artifactShelfLast + " (" + spawnedShelves[lastShelvingUnit].name.ToString() + ")?";
                        A_Menu.depositInLastShelfButton.SetActive(true);
                    }
                    else
                    {
                        A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = initialDepositText;
                        A_Menu.depositButton.GetComponentInChildren<TextMeshProUGUI>().text = initialDepositButtonText;
                    }
                }
            }
        }

        step = 0;


        A_Menu.withdrawButton.SetActive(false);
        //A_Menu.depositButton.SetActive(false);
        A_Menu.depositList.SetActive(false);
        //VirtualizedScrollRectListTester vsrltDeposit = A_Menu.depositList.GetComponentInChildren<VirtualizedScrollRectListTester>();
        vsrltDeposit.SetForDeposit(false);

        Debug.Log("Deposit list - Stop Navigation");
    }

    public void ResetPath()
    {
        step = 0;
        A_Menu.artifactTarget.GetComponent<Follow>().enabled = true;
        A_Menu.artifactTarget.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        recentPath.Clear();
        NextStep();
    }

    public void ManageCubeAndSphere(bool toParent)
    {
        if (!positioningCubeRoom.activeSelf)
        { return; }

        if (toParent)
        {
            positioningCubeRoom.transform.SetParent(positioningSphere.transform);
            wireframeCubeRoom.transform.SetParent(positioningSphere.transform);
        }
        else
        {
            positioningCubeRoom.transform.SetParent(null);
            wireframeCubeRoom.transform.SetParent(null);
        }
    }

    //chiamata quando si ritira un reperto dallo scaffale
    public async void WithdrawArtifact()
    {
        //PlayerPrefs.DeleteKey(artifactPP + artifactSelected.GetComponent<ArtifactView>().data.id.ToString());
        //artifactSelected.GetComponent<ArtifactView>().data.SetShelfID(-1);
        A_Menu.artifactIndicator.SetActive(false);
        A_Menu.artifactIndicator.transform.SetParent(null);
        Artifact data = artifactSelected.GetComponent<ArtifactView>().data;
        data.shelvingUnit = -1;
        data.containerLocalPose = "";

        await apiService.UpdateArtifact(data);

        BackButtonArtifact();
    }
    
    public GameObject GetArtifactSelected()
    { return artifactSelected; }

    public void ResetArtifactSelected()
        { artifactSelected = null; }

    public void StartDepositNavigation(GameObject shelf)
    {
        //depositPath.Clear();
        //depositStep = 0;
        //VirtualizedScrollRectListTester vsrltDeposit = A_Menu.depositList.GetComponentInChildren<VirtualizedScrollRectListTester>();
        A_Menu.artifactText.SetActive(false);
        A_Menu.depositList.SetActive(false);
        currentPath.Clear();
        CalculatePath(shelf.transform);
        StartNavigation();
    }

    public void DepositSucceded()
    {
        foreach (var obj in A_Menu.artifactDepositedUI)
        {
            obj.SetActive(true);
        }

        A_Menu.depositList.SetActive(false);
        A_Menu.artifactText.SetActive(false);
        vsrltDeposit.DepositFinished();
    }

    public void DepositConfirmed()
    {
        A_Menu.artifactText.SetActive(true);

        foreach (var obj in A_Menu.artifactDepositedUI)
        {
            obj.SetActive(false);
        }

        Debug.Log("Deposit confirmed");
        A_Menu.artifactText.SetActive(true);
        A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = artifactShelfNo;
    }

    //chiamata quando si scrive sulla barra di ricerca dei reperti
    public void SearchArtifact(GameObject inputText)
    {
        string txt = inputText.GetComponent<MRTKTMPInputField>().text;
        //Debug.Log("Searching: \"" + txt + "\"");
        A_Menu.artifactScrollView.GetComponent<VirtualizedScrollRectListTester>().Searching(txt);
    }

    public List<GameObject> GetShelvesList()
    {
        return allShelves;
    }

    public List<GameObject> GetArtifactsList()
    { return allArtifacts; }

    public GameObject GetRoomByID(int id)
    {
        if (rooms.ContainsKey(id))
            return rooms[id];
        else
            return null;
    }

    //funzione di debug chiamata da handmenu per visualizzare la posizione di tutti gli shelves
    public void VisualizeAllShelves(bool visualize)
    {
        string emptyName = "Empty debug";
        if(visualize)
        {
            GameObject emptyDebug = new GameObject(emptyName);
            foreach(GameObject child in allShelves)
            {
                GameObject newCube = Instantiate(debugCube, emptyDebug.transform);
                newCube.name = "Debug_" + child.name;
                newCube.transform.position = child.transform.position;
            }
        }
        else
        {
            GameObject emptyDebug = GameObject.Find(emptyName);
            Destroy(emptyDebug);
        }
    }
}

