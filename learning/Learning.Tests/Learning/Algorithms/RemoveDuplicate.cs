public static class RemoveDuplicate
{
    //Time: O(n²), Space: O(1)
    // This method removes duplicates from an array by checking each element against a list of unique elements.
    public static List<int> WithBruteForce(int[] nums)
    {
        var unique = new List<int>();
        for (int i = 0; i < nums.Length; i++)
        {
            bool isDuplicate = false;
            for (int j = 0; j < unique.Count; j++)
            {
                if (unique[j] == nums[i])
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate)
            {
                unique.Add(nums[i]);
            }
        }
        return unique;
    }
    //Time: O(n), Space: O(n)
    // This method removes duplicates from an array by using a HashSet to track unique elements.
    public static List<int> WithHashSet(int[] nums)
    {
        var unique = new HashSet<int>();
        var result = new List<int>();
        foreach (var n in nums)
        {
            if (unique.Add(n))
            {
                result.Add(n);
            }
        }
        return result;
    }
}