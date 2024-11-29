using System;
using UnityEngine;

[Serializable]
public class Idea
{
    [field: SerializeField, TextArea(1, 6)]
    public string Prompt { get; set; }

    public string Author { get; set; }
    public string Source { get; set; }
    public string Slug { get; set; }

    private string NewSlug => Guid.NewGuid().ToString().Substring(0, 7);

    public Idea()
    {
        Author = "polbot";
        Source = "manual";
        Slug = NewSlug;
    }

    public Idea(string prompt) : this()
    {
        Prompt = prompt;
    }

    public Idea(string title, string text, string author, string source, string slug = null)
    {
        if (!string.IsNullOrEmpty(text))
            title = $"{title}: {text}";
        Prompt = title;
        Author = author;
        Source = source;

        if (string.IsNullOrEmpty(slug))
            slug = NewSlug;
        Slug = slug;
    }

    public Idea RePrompt(TextAsset prompt)
    {
        Prompt = prompt.Format(Prompt);
        return this;
    }
}