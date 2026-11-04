using System.Diagnostics;

foreach (var n in new[] { 1_000, 10_000, 50_000 })
{
    var nums = Enumerable.Range(0, n).ToArray();   // không có phần tử trùng, là trường hợp xấu nhất

    // Chạy khởi động một lần để JIT biên dịch xong, không tính thời gian
    Check.HasDuplicateBruteForce(nums);
    Check.HasDuplicateHashSet(nums);

    var sw = Stopwatch.StartNew();
    Check.HasDuplicateBruteForce(nums);
    var brute = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    Check.HasDuplicateHashSet(nums);
    var hash = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"n={n,8:N0} | brute={brute,10:F2} ms | hashset={hash,8:F3} ms");
}

static class Check
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