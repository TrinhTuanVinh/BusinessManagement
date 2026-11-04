public class SearchTests
{

    [Theory]
    [InlineData(new int[] { }, 3, -1)]
    [InlineData(new int[] { 1 }, 1, 0)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 1, 0)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 5, 4)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 0, -1)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 6, -1)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 3, 2)]

    public void BinarySearchBruteForce_Success_Checkexpected(int[] nums, int target, int expected)
    {
        var result = Search.Linear(nums, target);
        Assert.Equal(expected, result);
    }
    [Theory]
    [InlineData(new int[] { }, 3, -1)]
    [InlineData(new int[] { 1 }, 1, 0)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 1, 0)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 5, 4)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 0, -1)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 6, -1)]
    [InlineData(new int[] { 1, 2, 3, 4, 5 }, 3, 2)]
    public void BinarySearch_Success_Checkexpected(int[] nums, int target, int expected)
    {
        var result = Search.Binary(nums, target);
        Assert.Equal(expected, result);
    }
}