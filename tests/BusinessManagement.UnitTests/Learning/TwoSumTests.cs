public class TwoSumTests
{
    private static readonly (string Name, Func<int[], int, int[]?> Solve)[] Implementations =
      {
        ("BruteForce", TwoSum.WithBruteForce),
        ("WithDictionary", TwoSum.WithDictionary)
    };

    [Theory]
    [InlineData(3, new int[] { 2, 7 }, null)]
    [InlineData(6, new int[] { 3, 3 }, new int[] { 0, 1 })]
    [InlineData(3, new int[] { }, null)]
    [InlineData(3, new int[] { 4 }, null)]
    [InlineData(-8, new[] { -1, -2, -3, -4, -5 }, new[] { 2, 4 })]
    public void TwoSum_Success_Checkexpected(int target, int[] nums, int[]? expected)
    {
        foreach (var (name, solve) in Implementations)
        {
            var actual = solve(nums, target);
            var ok = expected is null ? actual is null : actual is not null && expected.SequenceEqual(actual);
            Assert.True(ok, $"{name} returned [{(actual is null ? "null" : string.Join(", ", actual))}]");
        }
    }

    // [Theory]
    // [InlineData(3, new int[] { 2, 7 }, new int[] { 0, 1 })]
    // [InlineData(6, new int[] { 3, 3 }, new int[] { 0, 1 })]
    // [InlineData(3, new int[] { }, new int[] { 0, 1 })]
    // [InlineData(3, new int[] { 4 }, new int[] { 0, 1 })]
    // [InlineData(8, new int[] { -1, -2, -3, -4, 5 }, new int[] { 2, 4 })]
    // public void TwoSumBruteForce_Success_Checkexpected(int target, int[] nums, int[] expected)
    // {

    //     var result = TwoSum.WithBruteForce(nums, target);
    //     Assert.Equal(expected, result);
    // }
}