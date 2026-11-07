public static class TwoSum
{
  
    //Time: O(n), Space: O(n)
    // This method finds two indices of numbers in the array that add up to the target using a dictionary for quick lookups.
    public static int[]? WithDictionary(int[] nums, int target)
    {
        var sum = new Dictionary<int, int>();
        for (int i = 0; i < nums.Length; i++)
        {
            var complement = target - nums[i];
            if (sum.ContainsKey(complement))
            {
                return new int[] { sum[complement], i };
            }
            sum[nums[i]] = i;
        }
        return null;
    }

    //Time: O(n²), Space: O(1)
    // This method finds two indices of numbers in the array that add up to the target using a brute-force approach.
    public static int[]? WithBruteForce(int[] nums, int target)
    {
        for (int i = 0; i < nums.Length; i++)
        {
            for (int j = i + 1; j < nums.Length; j++)
            {
                if (nums[i] + nums[j] == target)
                {
                    return new int[] { i, j };
                }
            }
        }
        return null;
    }
}