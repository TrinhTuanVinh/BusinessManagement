public static class Unique
{
    //Time: O(n²), Space: O(1)
    // This method checks for duplicates in an array by comparing each element with every other element.
    public static char? FirstUniqueBruteForce(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            bool isUnique = true;
            for (int j = 0; j < s.Length; j++)
            {
                if (i != j && s[i] == s[j])
                {
                    isUnique = false;
                    break;
                }
            }
            if (isUnique)
            {
                return s[i];
            }
        }
        return null;
    }
    //Time: O(n), Space: O(n)
    // This method checks for duplicates in an array by using a dictionary to count occurrences of each character.
    public static char? FirstUnique(string s)
    {
        var count = new Dictionary<char, int>();
        foreach (var c in s)
        {
            if (!count.ContainsKey(c))
            {
                count[c] = 0;
            }
            count[c]++;
        }

        foreach (var c in s)
        {
            if (count[c] == 1)
            {
                return c;
            }
        }

        return null;
    }
}