public class PalindromeTests
{

    [Theory]
    [InlineData("", true)]
    [InlineData("race a car", false)]
    [InlineData(",.", true)]
    [InlineData("0P", false)]
    public void IsPalindromeBruteForce_Success_Checkexpected(string s, bool expected)
    {
        var result = Palindrome.IsBruteForce(s);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("race a car", false)]
    [InlineData(",.", true)]
    [InlineData("0P", false)]
    public void IsPalindrome_Success_Checkexpected(string s, bool expected)
    {
        var result = Palindrome.IsPalindrome(s);
        Assert.Equal(expected, result);
    }
}