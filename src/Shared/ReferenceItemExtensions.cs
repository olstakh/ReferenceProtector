using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReferenceModels;

internal static class ReferenceItemExtensions
{
    public static string ToFileLine(this ReferenceItem item)
    {
        return $"{item.Source}\t{item.LinkType}\t{item.Target}";
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
        var items = lines.Select(line => ReferenceItem.FromLine(line));

        return items.ToList();
    }
}