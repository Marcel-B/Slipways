using System.Linq;
using com.b_velop.Slipways.Data.Models;

namespace com.b_velop.Slipways.Web.Infrastructure
{
    public static class StringHelper
    {
        public static string GetImage(this Extra extra)
        {
            var id = extra.Id;
            return id.ToString().ToUpper() switch
            {
                "8976CEB5-19D6-4F5C-A34D-A43801667B40" => "parking-24.png",
                "F5836F04-E23B-475A-A079-1E4F3C9C4D87" => "camping-24.png",
                "06448FD8-DCC1-4579-947A-8A7B18BC1AAB" => "pier-24.png",
                _ => ""
            };
        }

        public static string FirstUp(string input)
        {
            var letter = input.First().ToString().ToUpper();
            var str = letter + input.Substring(1);
            return str;
        }

        public static string FirstUpper(this string input)
        {
            if (input == null)
                return input;
            input = input.ToLower();
            var strs = input.Split(' ');
            var result = string.Empty;
            foreach (var str in strs)
            {
                result += FirstUp(str) + " ";
            }
            input = result.TrimEnd();
            strs = input.Split('-');
            if (strs.Length == 1)
                return input;
            result = string.Empty;
            foreach (var str in strs)
            {
                result += FirstUp(str) + "-";
            }
            result = result[0..^1];
            return result;
        }
    }
}
