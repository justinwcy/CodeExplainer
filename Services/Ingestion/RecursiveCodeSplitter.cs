﻿namespace CodeExplainer.Services.Ingestion;

public class RecursiveCodeSplitter
{
    private readonly List<string> _separators;
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    /// <summary>
    /// Initializes a new instance of the RecursiveCodeSplitter.
    /// </summary>
    /// <param name="chunkSize">The desired maximum length of each chunk.</param>
    /// <param name="chunkOverlap">The character overlap between consecutive chunks.
    /// This is primarily applied as a final fixed-size split fallback.</param>
    /// <param name="separators">Optional. A list of strings to use as separators, ordered
    /// from the most semantically significant (e.g., paragraphs) to the least (e.g., characters).
    /// If null, default C# syntax-aware separators are used.</param>
    public RecursiveCodeSplitter(int chunkSize, int chunkOverlap, List<string> separators = null)
    {
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
        // Default C# specific separators, ordered from largest semantic unit to smallest.
        // The order is crucial: try to break on major structural elements first.
        _separators = separators ?? new List<string>
        {
            //"\n\n", // Blank lines (often separating major code blocks like classes, methods, regions)
            //"\n",   // Newlines (for individual lines of code)
            "{", // Start of code blocks (methods, classes, if/for/while blocks). Keeping this helps
            // ensure a closing brace stays with its block content.
            "}", // End of code blocks (methods, classes, if/for/while blocks). Keeping this helps
            // ensure a closing brace stays with its block content.
            ";", // End of statements. Keeping this helps ensure a statement stays intact.
        };
    }

    /// <summary>
    /// Splits the given C# code text into chunks based on predefined syntax-aware separators,
    /// adhering to a maximum chunk size and overlap.
    /// </summary>
    /// <param name="text">The input C# code text to be chunked.</param>
    /// <returns>A list of text chunks, where each chunk is a semantically meaningful portion of the code.</returns>
    public List<string> SplitText(string text)
    {
        List<string> finalChunks = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return finalChunks;
        }

        // Step 1: Perform the recursive splitting based on C# syntax-aware delimiters.
        // This generates potentially large, but semantically coherent, raw chunks.
        List<string> rawChunks = InternalSplit(text, _separators);

        // Step 2: Iterate through the raw chunks. If any raw chunk is still too large
        // (after recursive splitting, which prioritizes semantic breaks over strict size),
        // apply a final fixed-size splitting with overlap. This ensures all final chunks
        // meet the _chunkSize constraint while preserving some context.
        foreach (string currentChunk in rawChunks)
        {
            if (currentChunk.Length > _chunkSize)
            {
                // If a chunk is still too big, break it down further using fixed-size with overlap.
                // This is the fallback for when semantic splits don't yield small enough chunks.
                finalChunks.AddRange(FixedSizeChunkerWithOverlap(currentChunk, _chunkSize, _chunkOverlap));
            }
            else
            {
                // If the chunk is already within the desired size, add it directly.
                finalChunks.Add(currentChunk);
            }
        }

        return finalChunks;
    }

    /// <summary>
    /// Recursively splits the text by the current separator. If a resulting part is still too large,
    /// it tries to split it further using the next separator in the prioritized list.
    /// </summary>
    /// <param name="text">The current text segment to split.</param>
    /// <param name="currentSeparators">The list of separators remaining to be tried, in order.</param>
    /// <returns>A list of chunks generated by this recursive step.</returns>
    private List<string> InternalSplit(string text, List<string> currentSeparators)
    {
        List<string> chunks = new List<string>();
        if (string.IsNullOrEmpty(text))
        {
            return chunks;
        }

        // Base case: If no more separators are available, the current text segment
        // cannot be further broken down semantically by a defined separator.
        // It will be handled by the FixedSizeChunkerWithOverlap in the main SplitText method
        // if its length exceeds _chunkSize.
        if (currentSeparators.Count == 0)
        {
            chunks.Add(text.Trim());
            return chunks;
        }

        string currentSeparator = currentSeparators[0];
        List<string> nextSeparators = currentSeparators.Skip(1).ToList();

        // Determine if the current separator (like '}' or ';') should be included in the chunk.
        // This is important for C# code structure to keep logical units together.
        //bool shouldKeepSeparator = currentSeparator == "}" || 
        //                           currentSeparator == "{" || 
        //                           currentSeparator == ";";
        var shouldKeepSeparator = true;

        List<string> parts = new List<string>();
        int lastIndex = 0;
        int sepLength = currentSeparator.Length;

        // Manually find and split by the current separator to control inclusion/exclusion.
        while (lastIndex < text.Length)
        {
            int sepIndex = text.IndexOf(currentSeparator, lastIndex, StringComparison.Ordinal);

            if (sepIndex == -1) // Separator not found, add the remaining text as the last part
            {
                string remaining = text.Substring(lastIndex);
                if (!string.IsNullOrWhiteSpace(remaining))
                {
                    parts.Add(remaining.Trim());
                }

                break; // Exit the loop
            }

            // Extract the text segment before the separator
            string partBeforeSeparator = text.Substring(lastIndex, sepIndex - lastIndex);

            // Construct the chunk to add, potentially including the separator
            string chunkToAdd = partBeforeSeparator.Trim();

            // If the separator should be kept, append it to the current chunk
            if (shouldKeepSeparator && !string.IsNullOrEmpty(currentSeparator))
            {
                chunkToAdd += currentSeparator;
            }

            // Add the constructed chunk if it's not empty after trimming
            if (!string.IsNullOrWhiteSpace(chunkToAdd))
            {
                parts.Add(chunkToAdd);
            }

            // Move the lastIndex past the current separator for the next iteration
            lastIndex = sepIndex + sepLength;
        }

        // Handle cases where no splits occurred, but the original text was not empty
        if (parts.Count == 0 && !string.IsNullOrWhiteSpace(text))
        {
            parts.Add(text.Trim());
        }

        // Recursively process each part generated by the current separator
        foreach (string part in parts)
        {
            if (string.IsNullOrEmpty(part)) continue;

            // If the part is still larger than the target chunk size AND there are more
            // specific separators to try (e.g., a "paragraph" was too long, try splitting by "lines")
            if (part.Length > _chunkSize && nextSeparators.Any())
            {
                chunks.AddRange(InternalSplit(part, nextSeparators)); // Recurse with the next separator
            }
            else
            {
                // If the part is within the desired size or no more separators, add it directly.
                // The final fixed-size chunking (with overlap) will be applied in SplitText if needed.
                chunks.Add(part);
            }
        }

        return chunks;
    }

    /// <summary>
    /// A helper method to perform a fixed-size split with overlap. This is used as a fallback
    /// when semantic splitting (recursive splitting) does not yield chunks small enough.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="chunkSize">The maximum size of each chunk.</param>
    /// <param name="overlapSize">The size of the overlap between consecutive chunks.</param>
    /// <returns>A list of fixed-size, potentially overlapping, chunks.</returns>
    private static List<string> FixedSizeChunkerWithOverlap(string text, int chunkSize, int overlapSize)
    {
        List<string> chunks = new List<string>();
        if (string.IsNullOrEmpty(text) || chunkSize <= 0) return chunks;

        // Calculate the step size for moving through the text
        // Ensures progress and considers overlap
        int step = chunkSize - overlapSize;
        if (step <= 0) step = 1; // Avoid infinite loop for invalid overlap sizes

        for (int i = 0; i < text.Length; i += step)
        {
            // Determine the length of the current chunk, ensuring it doesn't exceed text bounds
            int len = Math.Min(chunkSize, text.Length - i);
            if (len > 0)
            {
                chunks.Add(text.Substring(i, len));
            }
        }

        return chunks;
    }
}