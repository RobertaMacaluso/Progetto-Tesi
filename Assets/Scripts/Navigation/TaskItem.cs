using UnityEngine;

[System.Serializable]
public class TaskItem
{
    public Artifact Artifact;

    //public StorageContainer Shelf;

    public StorageContainerView ShelfView;

    public TaskOperation Operation;

    public bool Completed = false;

    // Le due fasi di uno stesso transfer condividono questo identificatore.
    public string TransferId;

    public bool IsTransfer => !string.IsNullOrEmpty(TransferId);
}