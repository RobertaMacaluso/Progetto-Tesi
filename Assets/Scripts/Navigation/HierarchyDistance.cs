using System;
using System.Collections;
using System.Collections.Generic;

public static class HierarchyDistance
{
    public static int Distance(int[] pathA, int[] pathB)
    {
        int common = 0;

        int max = Math.Min(pathA.Length, pathB.Length);

        while (common < max && pathA[common] == pathB[common])
        {
            common++;
        }

        return (pathA.Length - common) + (pathB.Length - common);
    }

}
