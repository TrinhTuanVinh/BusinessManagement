public static class Anagram
{
    //Time: O(n log n), Space: O(n)
    // This method checks if two strings are anagrams by sorting their characters and comparing the sorted arrays.
      public static bool IsAnagramSorting(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var aChars = a.ToCharArray();
        var bChars = b.ToCharArray();

        Array.Sort(aChars);
        Array.Sort(bChars);

        for (int i = 0; i < aChars.Length; i++)
        {
            if (aChars[i] != bChars[i]) return false;
        }

        return true;

    }
    //Time: O(n), Space: O(n)
    // This method checks if two strings are anagrams by counting the occurrences of each character in
    public static bool IsAnagram(string a, string b)
    {
        if (a.Length != b.Length) return false;

        var count = new Dictionary<char, int>();
        foreach (var c in a)
        {
            if (!count.ContainsKey(c))
            {
                count[c] = 0;
            }
            count[c]++;
        }

        foreach (var c in b)
        {
            if (!count.ContainsKey(c)) return false;
            count[c]--;
            if (count[c] < 0) return false;
        }

        return true;
    }

}