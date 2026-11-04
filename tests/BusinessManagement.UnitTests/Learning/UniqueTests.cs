public class UniqueTests
{

    [Theory]
    [InlineData("aabb", null)]
    [InlineData("", null)]
    [InlineData("loveleetcode", 'v')]
    public void FirstUniqueBruteForce_Success_Checkexpected(string input, char? expected)
    {

        var result = Unique.FirstUniqueBruteForce(input);
        Assert.Equal(expected, result);
    }
    [Theory]
    [InlineData("aabb", null)]
    [InlineData("", null)]
    [InlineData("loveleetcode", 'v')]
    public void FirstUnique_Success_Checkexpected(string input, char? expected)
    {

        var result = Unique.FirstUnique(input);
        Assert.Equal(expected, result);
    }
}