using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WarehouseTask
{
    public List<TaskItem> Items = new();

    public int CurrentIndex = 0;

    public IReadOnlyList<TaskItem> TaskItems => Items;

    public TaskItem Current
    {
        get
        {
            if (CurrentIndex >= Items.Count)
                return null;

            return Items[CurrentIndex];
        }
    }

    public bool HasCurrent
    {
        get
        {
            return CurrentIndex < Items.Count;
        }
    }

    public bool IsCompleted
    {
        get
        {
            return CurrentIndex >= Items.Count;
        }
    }

    //public void Next()
    //{
    //    if (CurrentIndex < Items.Count)
    //        CurrentIndex++;
    //}

    public void Next()
    {
        if (!HasCurrent)
            return;

        Current.Completed = true;
        CurrentIndex++;
    }

    public void Clear()
    {
        Items.Clear();
        CurrentIndex = 0;
    }

    public void Add(TaskItem item)
    {
        Items.Add(item);
    }

    public int Count
    {
        get
        {
            return Items.Count;
        }
    }

    public void Reset()
    {
        CurrentIndex = 0;

        foreach (var item in Items)
            item.Completed = false;
    }

    public bool HasNext()
    {
        return CurrentIndex + 1 < Items.Count;
    }

    public TaskItem NextItem()
    {
        if (!HasNext())
            return null;

        return Items[CurrentIndex + 1];
    }

    public bool ContainsArtifact(int artifactId)
    {
        //return Items.Exists(x => x.Artifact.id == artifactId);
        return Items.Exists(x =>
            x.Artifact != null &&
            x.Artifact.id == artifactId);
    }

    public void RemoveArtifact(int artifactId)
    {
        Items.RemoveAll(x => x.Artifact.id == artifactId);

        if (CurrentIndex >= Items.Count)
            CurrentIndex = Mathf.Max(0, Items.Count - 1);
    }

    public bool IsEmpty
    {
        get
        {
            return Items.Count == 0;
        }
    }

    public int RemainingCount
    {
        get
        {
            return Items.Count - CurrentIndex;
        }
    }
}