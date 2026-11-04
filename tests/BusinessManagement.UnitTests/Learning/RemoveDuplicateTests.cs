public class RemoveDuplicateTests
{
    [Theory]
    [InlineData(new int[] { }, new int[] { })]
    [InlineData(new[] { 2, 2, 2 }, new[] { 2 })]
    [InlineData(new[] { 3, 1, 3, 2, 1 }, new[] { 3, 1, 2 })]   // không sắp xếp, khoá thứ tự
    public void RemoveDuplicates_BothImplementations_KeepFirstOccurrenceOrder(int[] nums, int[] expected)
    {
        Assert.Equal(expected, RemoveDuplicate.WithBruteForce(nums));
        Assert.Equal(expected, RemoveDuplicate.WithHashSet(nums));
    }
}