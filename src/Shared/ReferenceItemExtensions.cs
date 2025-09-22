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
        if (File.Exists(outputFile))
        {
            var currentContents = File.ReadAllLines(outputFile);
            var newContents = items.Select(item => item.ToFileLine()).ToArray();
            // Assuming order is the same for incremental builds, no sorting needed
            if (currentContents.SequenceEqual(newContents))
            {
                // No changes, skip writing to avoid updating the timestamp, which will trigger unnecessary rebuilds
                return;
            }
        }

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