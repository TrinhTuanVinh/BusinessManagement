using System.Diagnostics;
using Xunit.Abstractions;

public static class DuplicateChecker
{
    // Time: O(n²), Space: O(1)
    public static bool HasDuplicateBruteForce(int[] nums)
    {
        for (int i = 0; i < nums.Length; i++)
            for (int j = i + 1; j < nums.Length; j++)
                if (nums[i] == nums[j]) return true;
        return false;
    }

    // Time: O(n), Space: O(n)
    public static bool HasDuplicateHashSet(int[] nums)
    {
        var seen = new HashSet<int>();
        foreach (var n in nums)
            if (!seen.Add(n)) return true;
        return false;
    }
}

public class Algorithms
{
  
 


}