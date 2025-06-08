using Microsoft.SemanticKernel.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace CodeExplainer.Services.Ingestion;

public class CodeFileDirectorySource(string sourceDirectory) : IIngestionSource
{
    public string SourceFileId(string path) => Path.GetRelativePath(sourceDirectory, path);
    public static string SourceFileVersion(string path) => File.GetLastWriteTimeUtc(path).ToString("o");

    public string SourceId => $"{nameof(CodeFileDirectorySource)}:{sourceDirectory}";

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileVersion = SourceFileVersion(sourceFile);
            var existingDocumentVersion = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) ? existingDocument.DocumentVersion : null;
            if (existingDocumentVersion != sourceFileVersion)
            {
                results.Add(new() { Key = Guid.CreateVersion7().ToString(), SourceId = SourceId, DocumentId = sourceFileId, DocumentVersion = sourceFileVersion });
            }
        }

        return Task.FromResult((IEnumerable<IngestedDocument>)results);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var currentFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        var currentFileIds = currentFiles.ToLookup(SourceFileId);
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return Task.FromResult(deletedDocuments);
    }

    public Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        var filePath = Path.Combine(sourceDirectory, document.DocumentId);
        var lines = File.ReadAllLines(filePath);
        var chunks = new List<IngestedChunk>();
        int chunkSize = 200;
        int chunkIndex = 0;

        for (int i = 0; i < lines.Length; i += chunkSize)
        {
            var chunkLines = lines.Skip(i).Take(chunkSize);
            var chunkText = string.Join(Environment.NewLine, chunkLines);

            chunks.Add(new IngestedChunk
            {
                Key = Guid.CreateVersion7().ToString(),
                DocumentId = document.DocumentId,
                Text = chunkText
            });

            chunkIndex++;
        }

        return Task.FromResult((IEnumerable<IngestedChunk>)chunks);
    }
}
