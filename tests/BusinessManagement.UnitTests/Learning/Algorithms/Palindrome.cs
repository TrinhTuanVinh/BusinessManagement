public static class Palindrome
{
    //Time: O(n), Space: O(n)
    // This method checks if a string is a palindrome by cleaning the string and comparing characters from both ends.
    public static bool IsBruteForce(string s)
    {
        var cleaned = new System.Text.StringBuilder();
        foreach (var c in s)
        {
            if (char.IsLetterOrDigit(c))
            {
                cleaned.Append(char.ToLower(c));
            }
        }
        var cleanStr = cleaned.ToString();
        int left = 0;
        int right = cleanStr.Length - 1;
        while (left < right)
        {
            if (cleanStr[left] != cleanStr[right])
            {
                return false;
            }
            left++;
            right--;
        }
        return true;
    }
    //Time: O(n), Space: O(1)
    // This method checks if a string is a palindrome by using two pointers to compare characters from both ends without extra space.
    public static bool IsPalindrome(string s)
    {
        int left = 0;
        int right = s.Length - 1;
        while (left < right)
        {
            while (left < right && !char.IsLetterOrDigit(s[left])) left++;
            while (left < right && !char.IsLetterOrDigit(s[right])) right--;
            if (char.ToLower(s[left]) != char.ToLower(s[right]))
            {
                return false;
            }
            left++;
            right--;
        }
        return true;
    }
}