using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TaskOptimizer
{
    //public static void Optimize(WarehouseTask task, Dictionary<int, int[]> hierarchyPaths)
    //{
    //    List<TaskItem> remaining = new(task.Items);
    //    List<TaskItem> ordered = new();

    //    TaskItem current = remaining[0];

    //    ordered.Add(current);
    //    remaining.RemoveAt(0);

    //    while (remaining.Count > 0)
    //    {
    //        TaskItem best = null;
    //        int bestDistance = int.MaxValue;

    //        foreach (TaskItem candidate in remaining)
    //        {
    //            int distance = Distance(current, candidate, hierarchyPaths);

    //            if (distance < bestDistance)
    //            {
    //                bestDistance = distance;
    //                best = candidate;
    //            }
    //        }

    //        ordered.Add(best);
    //        remaining.Remove(best);
    //        current = best;
    //    }

    //    Debug.Log("Ordine ottimizzato:");

    //    foreach (TaskItem item in ordered)
    //    {
    //        Debug.Log($"{item.Artifact.name} --> {item.ShelfView.data.name}");
    //    }

    //    task.Items.Clear();
    //    task.Items.AddRange(ordered);
    //}

    public static void Optimize(WarehouseTask task, Dictionary<int, int[]> hierarchyPaths)
    {
        Debug.Log($"Ordine iniziale: {string.Join(", ", task.Items.Select(go => go.Artifact.name))}");

        List<TaskItem> ordered =
            BuildNearestNeighbor(task.Items, hierarchyPaths);

        Debug.Log($"Ordine ottimizzato: {string.Join(", ", ordered.Select(go => go.Artifact.name))}");

        task.Items.Clear();
        task.Items.AddRange(ordered);
    }

    private static List<TaskItem> BuildNearestNeighbor(IReadOnlyList<TaskItem> items, Dictionary<int, int[]> hierarchyPaths)
    {
        List<TaskItem> remaining = new(items);
        List<TaskItem> ordered = new();

        TaskItem current = GetStartingTask(remaining);

        ordered.Add(current);
        remaining.RemoveAt(0);

        while (remaining.Count > 0)
        {
            TaskItem best = null;
            int bestDistance = int.MaxValue;

            foreach (TaskItem candidate in remaining)
            {
                int distance = Distance(current, candidate, hierarchyPaths);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            ordered.Add(best);
            remaining.Remove(best);
            current = best;
        }

        return ordered;
    }

    private static TaskItem GetStartingTask(List<TaskItem> remaining)
    {
        return remaining[0];
    }

    private static int Distance(TaskItem a, TaskItem b, Dictionary<int, int[]> hierarchyPaths)
    {
        int idA = a.ShelfView.data.id;
        int idB = b.ShelfView.data.id;

        return HierarchyDistance.Distance(hierarchyPaths[idA], hierarchyPaths[idB]);
    }
}
