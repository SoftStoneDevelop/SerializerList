using System;
using System.Collections.Generic;
using System.Reflection;

namespace ListSerializer.Helper
{
    public static class StringHelper
    {
        public static readonly Func<int, string> FastAllocateString =
            (Func<int, string>)typeof(string)
            .GetMethod("FastAllocateString", BindingFlags.NonPublic | BindingFlags.Static)
            .CreateDelegate(typeof(Func<int, string>))
            ;
    }

    public static class StringHashSetHelper
    {
        public static readonly Func<HashSet<string>, string,int> FindItemIndex =
            (Func< HashSet<string>, string, int>)typeof(HashSet<string>)
            .GetMethod("FindItemIndex", BindingFlags.NonPublic | BindingFlags.Instance)
            .CreateDelegate(typeof(Func<HashSet<string>, string, int>))
            ;
    }
}