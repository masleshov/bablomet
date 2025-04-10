using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace Bablomet.Common.Infrastructure;

public static class EnvironmentSetter
{
    public static void SetFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        foreach (var line in File.ReadAllLines(filePath))
        {
            var equalityIndex = line.IndexOf('=');
            if (equalityIndex == -1)
            {
                continue;
            }

            var key = line.Substring(0, equalityIndex);
            var value = line.Substring(equalityIndex + 1, line.Length - equalityIndex - 1);

            Environment.SetEnvironmentVariable(key, value);
        }

        foreach (var variable in Environment.GetEnvironmentVariables())
        {
            var entry = (DictionaryEntry)variable;
            if (string.IsNullOrEmpty(entry.Value?.ToString()))
            {
                continue;
            }

            var matches = Regex.Matches(entry.Value.ToString(), @"\${[A-Z]+(?:_[A-Z]+)*}");
            foreach (var match in matches)
            {
                var substitutionKey = Regex.Match(match.ToString(), "[A-Z]+(?:_[A-Z]+)*");
                if (!substitutionKey.Success)
                {
                    continue;
                }

                var substitution = Environment.GetEnvironmentVariable(substitutionKey.Value);
                if (substitution == null)
                {
                    continue;
                }

                entry.Value = entry.Value.ToString().Replace(match.ToString(), substitution);
            }

            Environment.SetEnvironmentVariable(entry.Key.ToString(), entry.Value.ToString().Replace("\"", string.Empty));
        }
    }
}
