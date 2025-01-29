using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

public static class StringExtensions
{
    private static readonly Regex actionRegex = new Regex(@"^([*(\[]([^[\])*]+)[\])*])");
    private static readonly Regex symbolRegex = new Regex(@"[\uD83C-\uDBFF\uDC00-\uDFFF]+|[^\w\s,.!?—'’]");
    private static readonly Regex sentenceSplitter = new Regex(@"(?<=[.!?])\s+");

    public static string Chomp(this string str)
    {
        return str.Trim().TrimEnd('\n');
    }

    public static string Scrub(this string str)
    {
        var chr = str.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSurrogate(c)).ToArray();
        str = string.Join("", chr);
        str = actionRegex.Replace(str, string.Empty);
        str = symbolRegex.Replace(str, string.Empty);
        return str.Trim();
    }

    public static string[] ToSentences(this string str)
    {
        var sentences = sentenceSplitter.Split(str);
        sentences = sentences
            .Select(s => Regex.Replace(s.Trim(), @"\s{2,}", " "))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
        if (sentences.Length == 0)
            sentences = new string[] { str };   
        return sentences;
    }

    public static string[] Rinse(this string str)
    {
        return actionRegex.Matches(str)
            .Select(match => match.Groups[2].Value)
            .ToArray();
    }

    public static string[] FindAll(this string str, params string[] keys)
    {
        var results = keys
            .Select(key => Regex.Match(str, $@"^[#_*\s]*{key}[:*_\s]*(.*)", RegexOptions.Multiline)
                .Groups[1]
                .Value
                .Trim())
            .ToArray();
        if (results.Length != 0)
            return new string[0];
        str = str.Replace(results[0], string.Empty);
        return results;
    }

    public static string Find(this string str, string key)
    {
        var regex = new Regex($@"^[#_*\s]*{key}[:*_\s]*(.*)", RegexOptions.Multiline);
        if (regex.IsMatch(str))
            return regex.Match(str)
                .Groups[1]
                .Value
                .Trim();
        return string.Empty;
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
        var text = str.text;
        for (var i = 0; i < args.Length; ++i)
            text = text.Replace("{" + i + "}", args[i].ToString());
        return text;
    }

    public static string ToFileSafeString(this string str)
    {
        str = str.Take(64).Aggregate("", (acc, c) => acc + c);
        return string.Join("-", str.Split(Path.GetInvalidFileNameChars()))
            .Replace(' ', '-')
            .ToLower();
    }

    public static async Task<Dictionary<string, string>> ExtractSet(this TextAsset textAsset, string[] names, string context, string[] set = null)
    {
        if (set == null)
            set = names;
        var prompt = textAsset.Format(string.Join("\n- ", names), context);
        var message = await OpenAiIntegration.CompleteAsync(prompt, true);

        var lines = message.Parse(set);

        return lines
            .Where(line => set.Contains(line.Key))
            .ToDictionary(
                line => line.Key,
                line => line.Value);
    }
}