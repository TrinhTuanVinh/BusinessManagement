public class MaxSumTests
{

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]      // lớn hơn độ dài mảng [1,2,3,4,5]
    public void MaxSumOfSizeK_InvalidK_Throws(int k)
      => Assert.Throws<ArgumentException>(() => MaxSum.MaxSumOfSizeK(new[] { 1, 2, 3, 4, 5 }, k));

    [Fact]
    public void MaxSumOfSizeK_NullArray_Throws()
        => Assert.Throws<ArgumentException>(() => MaxSum.MaxSumOfSizeK(null!, 2));
}