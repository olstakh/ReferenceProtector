using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReferenceModels;

internal record ReferenceItem(string Source, string Target, ReferenceKind LinkType);

internal enum ReferenceKind
{
    ProjectReferenceDirect,
    ProjectReferenceTransitive,
}

internal static class ReferenceItemExtensions
{
    public static string ToFileLine(this ReferenceItem item)
    {
        return $"{item.Source}\t{item.LinkType}\t{item.Target}";
    }

    public static ReferenceItem FromFileLine(string line)
    {
        var parts = line.Split('\t');
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid reference item format: {line}");
        }

        return new ReferenceItem(
            Source: parts[0],
            LinkType: (ReferenceKind)Enum.Parse(typeof(ReferenceKind), parts[1]),
            Target: parts[2]);
    }

    public static void SaveToFile(this List<ReferenceItem> items, string outputFile)
    {
        File.WriteAllLines(outputFile, items.Select(item => item.ToFileLine()));
    }

    public static List<ReferenceItem> LoadFromFile(string inputFile)
    {
        if (!File.Exists(inputFile))
        {
            throw new FileNotFoundException($"The file '{inputFile}' does not exist.");
        }

        var lines = File.ReadAllLines(inputFile);
        var items = lines.Select(line => FromFileLine(line));

        return items.ToList();
    }
}