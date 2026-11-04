public class ParenthesesTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("()[]{}", true)]
    [InlineData("{[]}", true)]
    [InlineData("]", false)]
    [InlineData("((", false)]
    [InlineData("([)]", false)]
    [InlineData("(a", false)]
    public void IsValid_ReturnsExpected(string s, bool expected)
        => Assert.Equal(expected, Parentheses.IsValid(s));
}