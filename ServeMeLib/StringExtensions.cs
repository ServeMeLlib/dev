using System;

namespace ServeMeLib
{
    public static class StringExtensions
    {
        public static string AppTrimStart(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            if (!string.IsNullOrEmpty(value))
            {
                while (!string.IsNullOrEmpty(inputText) && inputText.StartsWith(value, comparisonType))
                {
                    inputText = inputText.Substring(value.Length - 1);
                }
            }

            return inputText;
        }

        public static string AppTrimEnd(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            if (!string.IsNullOrEmpty(value))
            {
                while (!string.IsNullOrEmpty(inputText) && inputText.EndsWith(value, comparisonType))
                {
                    inputText = inputText.Substring(0, (inputText.Length - value.Length));
                }
            }

            return inputText;
        }

        public static string AppTrim(this string inputText, string value, StringComparison comparisonType = StringComparison.CurrentCultureIgnoreCase)
        {
            return AppTrimStart(AppTrimEnd(inputText, value, comparisonType), value, comparisonType);
        }
    }
}