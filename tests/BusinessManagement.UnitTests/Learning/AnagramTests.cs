public class AnagramTests
{

    [Theory]
    [InlineData("", "", true)]
    [InlineData("rat", "car", false)]
    [InlineData("anagram", "nagarams", false)]
    [InlineData("aab", "abb", false)]
    public void IsAnagram_Success_Checkexpected(string s, string t, bool expected)
    {

        var result = Anagram.IsAnagram(s, t);
        Assert.Equal(expected, result);
    }
}