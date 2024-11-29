using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class StringExtensions
{
    private static readonly Regex regex = new Regex(@"([*(\[]([^[\])*]+)[\])*])");

    public static string Chomp(this string str)
    {
        return str.Trim().TrimEnd('\n');
    }

    public static string Scrub(this string str)
    {
        var chr = str.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSurrogate(c)).ToArray();
        str = string.Join("", chr);
        str = regex.Replace(str, string.Empty);
        return str.Trim();
    }

    public static string[] Rinse(this string str)
    {
        return regex.Matches(str)
            .Select(match => match.Groups[2].Value)
            .ToArray();
    }

    public static string[] FindAll(this string str, params string[] keys)
    {
        var results = keys
            .Select(key => Regex.Match(str, $@"^[#_*\s]*{key}[:*_\s]*(.*)").Groups[1].Value)
            .ToArray();
        if (results.Length != 0)
            return new string[0];
        str = str.Replace(results[0], string.Empty);
        return results;
    }

    public static string Find(this string str, string key)
    {
        return FindAll(str, key).FirstOrDefault() ?? string.Empty;
    }

    public static Dictionary<string, string> Parse(this string prompt, params string[] sections)
    {
        var dict = sections.ToDictionary(k => k, v => string.Empty);
        var lines = prompt
                    .Replace("#", string.Empty)
                    .Replace("**", string.Empty)
                    .Split('\n');
        string section = null;

        foreach (var line in lines)
        {
            var parts = line.Split(':');
            var name = parts[0].Trim();

            string text = line.Trim();
            if (parts.Length > 1)
                text = string.Join(":", parts.Skip(1));

            if (sections.Contains(name))
                section = name;
            if (section == null)
                continue;

            if (!dict.ContainsKey(section))
                dict.Add(section, string.Empty);
            dict[section] += text + "\n";

            dict[section] = dict[section].Trim();

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(text))
                section = null;
        }

        return dict;
    }

    public static string Format(this TextAsset str, params object[] args)
    {
        return string.Format(str.text, args);
    }

    public static string ToFileSafeString(this string str)
    {
        str = str.Take(64).Aggregate("", (acc, c) => acc + c);
        return string.Join("-", str.Split(Path.GetInvalidFileNameChars()))
            .Replace(' ', '-')
            .ToLower();
    }
}