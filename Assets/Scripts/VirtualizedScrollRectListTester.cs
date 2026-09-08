// Copyright (c) Mixed Reality Toolkit Contributors
// Licensed under the BSD 3-Clause

// Disable "missing XML comment" warning for samples. While nice to have, this XML documentation is not required for samples.
#pragma warning disable CS1591

using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.UX.Experimental;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

namespace MixedReality.Toolkit.Examples.Demos
{
    [AddComponentMenu("MRTK/Examples/Virtualized Scroll Rect List Tester")]
    public class VirtualizedScrollRectListTester : MonoBehaviour
    {
        /*[SerializeField]
        [Tooltip("Auto-scrolls the list up and down in a sin wave.")]
        private bool sinScroll = true;*/
        
        [SerializeField] private AppManager appManager;
        [SerializeField] private GameObject warehouse;
        [SerializeField] private GameObject[] buttonPrefabs;
        //[SerializeField] private GameObject selectShelfButton;

        private VirtualizedScrollRectList list;
        private float destScroll;
        private bool animate;
        private bool forDeposit = false;
        private List<GameObject> depositList = new();
        private readonly string depositTextInitial = "Navigare la gerarchia fino al livello desiderato e confermare";
        private readonly string depositTextWithChild = "Continuare a navigare la gerarchia o confermare l'elemento: ";
        private readonly string depositTextNoChild = "Non ci sono ulteriori elementi, confermare: ";
        private List<GameObject> shelvesList = new();
        //private GameObject shelfForDeposit;
        private GameObject currentShelfNavigated;
        private List<GameObject> artifactsList = new();

        private readonly string[] words = { "one", "two", "three", "zebra", "keyboard", "rabbit", "graphite", "ruby", };
        private List<string> buttonsNames = new();

        /// <summary>
        /// A Unity event function that is called on the frame when a script is enabled just before any of the update methods are called the first time.
        /// </summary> 
        private void Start()
        {
            appManager.A_Menu.selectShelfButton.SetActive(false);

            shelvesList = appManager.GetShelvesList();
            artifactsList = appManager.GetArtifactsList();

            if (buttonsNames.Count > 0)
            {
                SetScrollView();
            }
            /*else
            {
                list = GetComponent<VirtualizedScrollRectList>();
                list.OnVisible = (go, i) =>
                {
                    foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        if (text.gameObject.name == "Text")
                        {
                            text.text = $"{words[i % words.Length]}";
                        }
                    }

                    foreach (var item in go.GetComponentsInChildren<PressableButton>())
                    {
                        item.OnClicked.AddListener(() => ButtonListener(i));
                    }
                };

                list.OnInvisible = (go, i) =>
                {
                    foreach (var item in go.GetComponentsInChildren<PressableButton>())
                    {
                        item.OnClicked.RemoveAllListeners();
                    }
                };
            }*/
            
        }

        /// <summary>
        /// A Unity event function that is called every frame, if this object is enabled.
        /// </summary>
        private void Update()
        {
            /*if (sinScroll)
            {
                list.Scroll = (Mathf.Sin(Time.time * 0.5f - (Mathf.PI / 2)) * 0.5f + 0.5f) * list.MaxScroll;
                destScroll = list.Scroll;
                animate = false;
            }*/

            if (animate)
            {
                float newScroll = Mathf.Lerp(list.Scroll, destScroll, 8 * Time.deltaTime);
                list.Scroll = newScroll;
                if (Mathf.Abs(list.Scroll - destScroll) < 0.02f)
                {
                    list.Scroll = destScroll;
                    animate = false;
                }
            }
        }

        /// <summary>
        /// Scrolls the VirtualizedScrollRect to the next page.
        /// </summary>
        public void Next()
        {
            //sinScroll = false;
            animate = true;
            destScroll = Mathf.Min(list.MaxScroll, Mathf.Floor(list.Scroll / list.RowsOrColumns) * list.RowsOrColumns + list.TotallyVisibleCount);
        }

        /// <summary>
        /// Scrolls the VirtualizedScrollRect to the previous page.
        /// </summary>
        public void Prev()
        {
            //sinScroll = false;
            animate = true;
            destScroll = Mathf.Max(0, Mathf.Floor(list.Scroll / list.RowsOrColumns) * list.RowsOrColumns - list.TotallyVisibleCount);
        }

        /// <summary>
        /// Testing function for adjusting the number of items during
        /// runtime.
        /// </summary>
        [ContextMenu("Set Item Count 50")]
        public void TestItemCount1() => list.SetItemCount(50);

        /// <summary>
        /// Testing function for adjusting the number of items during
        /// runtime.
        /// </summary>
        [ContextMenu("Set Item Count 200")]
        public void TestItemCount2() => list.SetItemCount(200);

        public void SetScrollView()
        {
            //Debug.Log("SetScrollView");
            list = GetComponent<VirtualizedScrollRectList>();
            list.OnVisible = (go, i) =>
            {
                foreach (var text in go.GetComponentsInChildren<TextMeshProUGUI>())
                {
                    if (text.gameObject.name == "Text")
                    {
                        text.text = $"{buttonsNames[i % buttonsNames.Count]}";

                        if (this.gameObject.tag == "Deposit list")
                        {
                            GameObject item = shelvesList.Find(x => x.name == text.text);
                            if (item != null)
                            {
                                if (item.GetComponent<StorageContainerView>().data.GetIsShelf())
                                    HandlePrefab(text.gameObject, false);
                                else
                                    HandlePrefab(text.gameObject, true);
                            }
                        }
                        else if (this.gameObject.tag == "Artifact list")
                        {
                            //Artifact artifact = artifactsList[i].GetComponent<ArtifactView>().data;
                            //Artifact artifact = artifactsList.Find(x => x.name == text.text).GetComponent<ArtifactView>().data;
                            string artifactName = buttonsNames[i];

                            GameObject artifactGO =
                                artifactsList.Find(x => x.name == artifactName);

                            Artifact artifact =
                                artifactGO.GetComponent<ArtifactView>().data;

                            //if (appManager.IsArtifactInCurrentTask(artifact.id))
                            //    UpdateTaskButton(go);
                            Debug.Log("setscrollview");
                            bool inTask = appManager.IsArtifactInCurrentTask(artifact.id);

                            UpdateTaskButton(go, inTask);
                            //RefreshVisibleButtons();
                        }
                    }
                }

                foreach (var item in go.GetComponentsInChildren<PressableButton>())
                {
                    item.OnClicked.AddListener(() => ButtonListener(i));
                }
            };

            list.OnInvisible = (go, i) =>
            {
                foreach (var item in go.GetComponentsInChildren<PressableButton>())
                {
                    item.OnClicked.RemoveAllListeners();
                }
            };
        }

        void ButtonListener(int i)
        {
            //Debug.Log("Bottone selezionato: " + buttonsNames[i]);

            if (!forDeposit)
            {
                if (appManager.A_Menu.artifactsPanel.activeSelf)
                {
                    appManager.OnArtifactButtonClicked(i);
                }
                else if (appManager.shelvesListPanel.activeSelf)
                {
                    appManager.OnShelfButtonClicked(i);
                }
            }
            else
            {
                /*if (depositList[i].transform.childCount > 0)
                {
                    //appManager.A_Menu.depositList.SetActive(true);
                    ListForDeposit(depositList[i]);
                }
                else
                {
                    //Debug.Log("Scelto scaffale per deposito");

                    //appManager.StartDepositNavigation(depositList[i]);
                    //shelfForDeposit = depositList[i];
                    ListForDeposit(depositList[i]);

                    // mettere testo che non ha altri figli?
                    appManager.A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = depositTextNoChild + currentShelfNavigated.name.ToString() + "?";
                    appManager.A_Menu.depositList.SetActive(false);
                }*/

                GameObject selected = depositList[i];

                List<GameObject> nextLevel = GetNextLogicalLevel(selected);

                if (nextLevel.Count > 0)
                {
                    // Ci sono elementi del livello logico successivo:
                    // si continua a navigare nella gerarchia.
                    ListForDeposit(selected);
                }
                else
                {
                    // Non ci sono ulteriori elementi visibili:
                    // l'elemento corrente può essere confermato come destinazione.
                    ListForDeposit(selected);

                    appManager.A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text =
                        depositTextNoChild + currentShelfNavigated.name + "?";

                    appManager.A_Menu.depositList.SetActive(false);
                }
            }
            
        }

        /*public void SetShelfForDeposit()
        {
            Debug.Log("Scelto scaffale per deposito");

            appManager.StartDepositNavigation(currentShelfNavigated);
            shelfForDeposit = currentShelfNavigated;
            appManager.A_Menu.selectShelfButton.SetActive(false);
        }*/

        public void SetShelfForDeposit()
        {
            Debug.Log("Scelto scaffale per deposito");

            //shelfForDeposit = currentShelfNavigated;

            appManager.AddDepositTask(
                currentShelfNavigated.GetComponent<StorageContainerView>());

            appManager.A_Menu.selectShelfButton.SetActive(false);
        }

        public async void DepositInShelf()
        {
            /*GameObject artifact = appManager.GetArtifactSelected();
            Artifact data = artifact.GetComponent<ArtifactView>().data;
            data.SetShelfID(shelfForDeposit.GetComponent<StorageContainerView>().data.id);
            
            // salvataggio
            //PlayerPrefs.SetInt(appManager.artifactPP + artifact.GetComponent<ArtifactView>().data.id.ToString(), shelfForDeposit.GetComponent<StorageContainerView>().data.id);
            //PlayerPrefs.SetInt(appManager.artifactPP + artifact.GetComponent<ArtifactView>().data.id.ToString() + "_Last", shelfForDeposit.GetComponent<StorageContainerView>().data.id);

            CalculateArtifactPose(artifact);

            await appManager.apiService.UpdateArtifact(data);

            Debug.Log("Deposit in Shelf");
            appManager.DepositSucceded();*/

            TaskItem taskItem = appManager.GetCurrentTaskItem();

            Artifact data = taskItem.Artifact;

            //data.SetShelfID(
            //    shelfForDeposit.GetComponent<StorageContainerView>().data.id);
            data.SetShelfID(taskItem.ShelfView.data.id);

            CalculateArtifactPose(data, taskItem.ShelfView);

            await appManager.apiService.UpdateArtifact(data);

            Debug.Log("Deposit in Shelf");

            appManager.DepositSucceded();
        }

        //private void CalculateArtifactPose(GameObject artifact)
        private void CalculateArtifactPose(Artifact artifact, StorageContainerView shelfView)
        {
            // calcolo pos e rot del reperto rispetto allo scaffale
            Vector3 localPos =
                shelfView.gameObject.transform.InverseTransformPoint(
                    appManager.A_Menu.artifactProp.transform.position);

            Quaternion localRot =
                Quaternion.Inverse(shelfView.gameObject.transform.rotation) *
                appManager.A_Menu.artifactProp.transform.rotation;

            string position =
                localPos.x.ToString(CultureInfo.InvariantCulture) + "_" +
                localPos.y.ToString(CultureInfo.InvariantCulture) + "_" +
                localPos.z.ToString(CultureInfo.InvariantCulture);

            string rotation =
                localRot.x.ToString(CultureInfo.InvariantCulture) + "_" +
                localRot.y.ToString(CultureInfo.InvariantCulture) + "_" +
                localRot.z.ToString(CultureInfo.InvariantCulture) + "_" +
                localRot.w.ToString(CultureInfo.InvariantCulture);

            artifact.containerLocalPose = position + "/" + rotation;
        }

        public void DepositFinished()
        {
            forDeposit = false;
            appManager.BackButtonArtifact();
        }

        public void SetWords(List<GameObject> items)
        {
            //Debug.Log("SetWords");
            list = GetComponent<VirtualizedScrollRectList>();
            list.SetItemCount(0);
            buttonsNames.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                buttonsNames.Add(items[i].name);
            }

            list.SetItemCount(items.Count);
            this.gameObject.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
        }

        private List<GameObject> GetNextLogicalLevel(GameObject parent)
        {
            List<GameObject> result = new();

            int currentLevel;

            // La warehouse rappresenta il punto di partenza:
            // il primo livello della gerarchia UI è il livello 1.
            if (parent == warehouse)
            {
                currentLevel = 0;
            }
            else
            {
                StorageContainerView view =
                    parent.GetComponent<StorageContainerView>();

                if (view == null || view.data == null)
                {
                    Debug.LogError("StorageContainerView o data mancanti su " + parent.name);
                    return result;
                }

                currentLevel = view.data.logicalHierarchyLevel;
            }

            int targetLevel = currentLevel + 1;

            FindLogicalLevelRecursive(
                parent.transform,
                targetLevel,
                result
            );

            result.Sort((x, y) => x.name.CompareTo(y.name));

            return result;
        }

        private void FindLogicalLevelRecursive(Transform parent, int targetLevel, List<GameObject> result)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                GameObject child = parent.GetChild(i).gameObject;

                StorageContainerView view =
                    child.GetComponent<StorageContainerView>();

                if (view == null || view.data == null)
                    continue;

                int level = view.data.logicalHierarchyLevel;

                if (level == targetLevel)
                {
                    // Abbiamo trovato un elemento del livello richiesto.
                    result.Add(child);

                    // Non scendiamo nei suoi figli:
                    // appartengono ai livelli successivi.
                    continue;
                }

                if (level == -1)
                {
                    // Elemento strutturale nascosto nella UI:
                    // lo attraversiamo per cercare il livello successivo.
                    FindLogicalLevelRecursive(
                        child.transform,
                        targetLevel,
                        result
                    );
                }
            }
        }

        public void ListForDeposit(GameObject parent)
        {
            forDeposit = true;
            depositList.Clear();

            //Debug.Log("List for deposit");

            if (parent == warehouse)
            {
                currentShelfNavigated = null;

                appManager.A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = depositTextInitial;

                // spegnere bottone di conferma shelf?
                appManager.A_Menu.selectShelfButton.SetActive(false);
            }
            else
            {
                currentShelfNavigated = parent;

                appManager.A_Menu.artifactText.GetComponent<TextMeshProUGUI>().text = depositTextWithChild + currentShelfNavigated.name.ToString();

                // accendere bottone di conferma shelf?
                appManager.A_Menu.selectShelfButton.SetActive(true);
            }

            //for (int i = 0; i < parent.transform.childCount; i++)
            //    depositList.Add(parent.transform.GetChild(i).gameObject);

            //depositList.Sort((x,y) => x.name.CompareTo(y.name));
            //SetWords(depositList);

            depositList = GetNextLogicalLevel(parent);
            SetWords(depositList);
        }

        //public void DepositInLastShelf()
        //{
        //    forDeposit = true;
        //    depositList.Clear();

        //    //Debug.Log("Deposit in last shelf");

        //    GameObject artifact = appManager.GetArtifactSelected();
        //    //int lastShelvingUnit = PlayerPrefs.GetInt(appManager.artifactPP + artifact.GetComponent<ArtifactView>().data.id + "_Last");
        //    int lastShelvingUnit = artifact.GetComponent<ArtifactView>().data.lastShelvingUnit;
        //    GameObject shelf = appManager.FindChildRecursive(warehouse.transform, lastShelvingUnit);

        //    appManager.StartDepositNavigation(shelf);
        //    shelfForDeposit = shelf;
        //}

        /*public void Back()
        {
            //GameObject currentParent = depositList[0].transform.parent.gameObject;
            GameObject currentParent = currentShelfNavigated;

            if (currentParent == null)
                currentParent = depositList[0].transform.parent.gameObject;

            //if (currentParent != null)
            //{
            if(currentParent.name != warehouse.name)
            {
                if (forDeposit && !appManager.A_Menu.depositList.activeSelf)
                    appManager.A_Menu.depositList.SetActive(true);

                ListForDeposit(currentParent.transform.parent.gameObject);
            }
            else
            {
                Debug.Log($"Chiusura lista. currentParent = {currentParent}");
                forDeposit = false;
                appManager.BackButtonArtifact();
            }
            //}
        }*/

        public void Back()
        {
            // Siamo già nella schermata iniziale:
            // mostra le stanze. Un ulteriore Back chiude la gerarchia.
            if (currentShelfNavigated == null)
            {
                Debug.Log("Back dal livello iniziale: chiusura lista.");

                forDeposit = false;
                appManager.BackButtonArtifact();
                return;
            }

            StorageContainerView currentView =
                currentShelfNavigated.GetComponent<StorageContainerView>();

            if (currentView == null || currentView.data == null)
            {
                Debug.LogError(
                    "StorageContainerView o data mancanti su " +
                    currentShelfNavigated.name
                );
                return;
            }

            int currentLevel =
                currentView.data.logicalHierarchyLevel;

            Debug.Log(
                $"Back da {currentShelfNavigated.name}, " +
                $"logicalHierarchyLevel = {currentLevel}"
            );

            // Se siamo al livello 1, dobbiamo tornare
            // alla schermata iniziale delle stanze.
            if (currentLevel == 1)
            {
                Debug.Log("Back dal livello 1: ritorno alla lista delle stanze.");

                if (!appManager.A_Menu.depositList.activeSelf)
                    appManager.A_Menu.depositList.SetActive(true);

                appManager.A_Menu.selectShelfButton.SetActive(false);

                ListForDeposit(warehouse);

                return;
            }

            // Livello precedente.
            int previousLevel = currentLevel - 1;

            GameObject previousParent =
                GetLogicalAncestor(
                    currentShelfNavigated,
                    previousLevel
                );

            if (previousParent == null)
            {
                Debug.LogError(
                    $"Impossibile trovare l'ancestor di livello {previousLevel}."
                );
                return;
            }

            Debug.Log(
                $"Ritorno a {previousParent.name}, " +
                $"livello {previousLevel}"
            );

            if (!appManager.A_Menu.depositList.activeSelf)
                appManager.A_Menu.depositList.SetActive(true);

            ListForDeposit(previousParent);
        }

        private GameObject GetLogicalAncestor(GameObject current, int targetLevel)
        {
            if (current == null || current == warehouse)
                return null;

            Transform ancestor = current.transform.parent;

            while (ancestor != null && ancestor.gameObject != warehouse)
            {
                StorageContainerView view =
                    ancestor.GetComponent<StorageContainerView>();

                if (view != null && view.data != null)
                {
                    if (view.data.logicalHierarchyLevel == targetLevel)
                        return ancestor.gameObject;
                }

                ancestor = ancestor.parent;
            }

            return null;
        }

        private List<GameObject> GetLogicalLevelUnder(GameObject parent, int targetLevel)
        {
            List<GameObject> result = new();

            FindLogicalLevelRecursive(
                parent.transform,
                targetLevel,
                result
            );

            result.Sort((x, y) => x.name.CompareTo(y.name));

            return result;
        }

        public void HandlePrefab(GameObject text, bool value)
        {
            //Debug.Log("Handle prefab");
 
            Transform icon = text.transform.parent.GetChild(1);
            if (icon != null)
            {
                //Debug.Log("Handle prefab 2");
                icon.gameObject.SetActive(value);
            }
        
        }

        private void UpdateTaskButton(GameObject button, bool inTask)
        {
            if (inTask)
            {
                Debug.Log("Colore verde");
                ColorUtility.TryParseHtmlString("#0CBE02FF", out var color);
                button.transform.GetChild(0).GetComponent<RawImage>().color = color;

                //button.GetComponent<BoxCollider>().enabled = false;
                //button.GetComponent<BoxCollider>().isTrigger = true;

                //button.GetComponentInChildren<UnityEngine.UI.Image>().sprite = "Icon_ChevronRight_F.png";
            }
            else
            {
                ColorUtility.TryParseHtmlString("#97D8FF51", out var color);
                button.transform.GetChild(0).GetComponent<RawImage>().color = color;

                //button.GetComponent<BoxCollider>().enabled = true;

                Debug.Log("Colore normale");
            }
           
        }

        public void RefreshVisibleButtons()
        {
            for (int i = 0; i < list.ItemCount; i++)
            {
                if (!list.TryGetVisible(i, out GameObject go))
                    continue;

                Artifact artifact =
                    appManager.GetArtifactOnList(i);

                UpdateTaskButton(
                    go,
                    appManager.IsArtifactInCurrentTask(artifact.id));
            }
        }

        public void Searching(string txt)
        {
            List<GameObject> search = new();
            int i = 0;
            foreach (var item in artifactsList)
            {
                if (item.name.Contains(txt, StringComparison.OrdinalIgnoreCase))
                {
                    //Debug.Log("Item \"" + item.name + "\" contains Txt \"" + txt + "\"");
                    GameObject obj = new();
                    obj.name = item.name;
                    search.Add(obj);
                    i++;
                }
            }
            //Debug.Log("Index = " + i + ". Search: " + string.Join(" - ", search.Select(go => go.name)));
            SetWords(search);
            appManager.UpdateArtifactList(search);
        }

        public bool GetForDeposit()
            { return forDeposit; }

        public void SetForDeposit(bool value)
            { forDeposit = value; }

        //public GameObject GetShelfDeposit()
        //{ return shelfForDeposit; }
    }
}
#pragma warning restore CS1591
