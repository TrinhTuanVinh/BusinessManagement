public static class Parentheses
{
    private static readonly Dictionary<char, char> Pairs = new() { [')'] = '(', ['}'] = '{', [']'] = '[' };
    // Time: O(n), Space: O(n)
    // This method checks if the parentheses in a string are valid by using a stack to track opening brackets.
    public static bool IsValid(string s)
    {
        var stack = new Stack<char>();
        foreach (var c in s)
        {
            if (Pairs.ContainsValue(c)) stack.Push(c);                                  // ngoặc mở
            else if (Pairs.TryGetValue(c, out var open))                                // ngoặc đóng
            {
                if (stack.Count == 0 || stack.Pop() != open) return false;
            }
            else return false;                                                          // ký tự lạ
        }
        return stack.Count == 0;

    }
}