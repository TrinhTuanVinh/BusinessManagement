public static class MaxSum
{

    // Time: O(n), Space: O(1)
    // This method finds the maximum sum of any contiguous subarray of size k using a sliding window approach.
    public static int MaxSumOfSizeK(int[] nums, int k)
    {
        if (nums == null || k <= 0 || k > nums.Length)
        {
            throw new ArgumentException("Invalid input");
        }

        int maxSum = 0;
        for (int i = 0; i < k; i++)
        {
            maxSum += nums[i];
        }

        int currentSum = maxSum;
        for (int i = k; i < nums.Length; i++)
        {
            currentSum += nums[i] - nums[i - k];
            maxSum = Math.Max(maxSum, currentSum);
        }

        return maxSum;
    }



}