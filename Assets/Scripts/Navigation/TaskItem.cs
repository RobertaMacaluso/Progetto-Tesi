using UnityEngine;

[System.Serializable]
public class TaskItem
{
    public Artifact Artifact;

    public StorageContainer Shelf;

    public StorageContainerView ShelfView;

    public TaskOperation Operation;

    public bool Completed = false;
}