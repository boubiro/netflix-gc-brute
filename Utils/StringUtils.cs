using System;
using System.Linq;

namespace Netflix_GC_Brute.Utils
{
    public class StringUtils
    {
        
        private static readonly Random Random = new Random();
        
        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }
        
    }
}