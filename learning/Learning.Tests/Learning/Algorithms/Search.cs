public static class Search
{
    //Time: O(n), Space: O(1)
    // This method performs a linear search on a sorted array to find the index of a target value.
    public static int Linear(int[] sorted, int target)
    {
        for (int i = 0; i < sorted.Length; i++)
        {
            if (sorted[i] == target)
            {
                return i;
            }
        }
        return -1;

    }
    //Time: O(log n), Space: O(1)
    // This method performs a binary search on a sorted array to find the index of a target value.
    public static int Binary(int[] sorted, int target)
    {
        int left = 0;
        int right = sorted.Length - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;

            if (sorted[mid] == target)
            {
                return mid;
            }
            else if (sorted[mid] < target)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return -1; // Not found
    }
}