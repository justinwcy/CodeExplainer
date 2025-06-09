using System.Security.Cryptography;
using System.Text;

namespace CodeExplainer.Services.Ingestion;

public class CodeFileDirectorySource(string sourceDirectory) : IIngestionSource
{
    public string SourceFileId(string path) => Path.GetRelativePath(sourceDirectory, path);

    public string SourceId => $"{nameof(CodeFileDirectorySource)}:{sourceDirectory}";

    public string SourceFileHashsum(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        return Encoding.Default.GetString(md5.ComputeHash(stream));
    }

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileVersion = SourceFileHashsum(sourceFile);
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
        var codeString = File.ReadAllText(filePath);
        var recursiveCodeSplitter = new RecursiveCodeSplitter(200, 20);
        var splitCodeStrings = recursiveCodeSplitter.SplitText(codeString);
        var chunks = splitCodeStrings.Select(splitCodeString => new IngestedChunk()
        {
            Key = Guid.CreateVersion7().ToString(),
            DocumentId = document.DocumentId,
            Text = splitCodeString
        });

        //var lines = File.ReadAllLines(filePath);
        //var chunks = new List<IngestedChunk>();
        //int chunkSize = 200;
        //int chunkIndex = 0;

        //for (int i = 0; i < lines.Length; i += chunkSize)
        //{
        //    var chunkLines = lines.Skip(i).Take(chunkSize);
        //    var chunkText = string.Join(Environment.NewLine, chunkLines);

        //    chunks.Add(new IngestedChunk
        //    {
        //        Key = Guid.CreateVersion7().ToString(),
        //        DocumentId = document.DocumentId,
        //        Text = chunkText
        //    });

        //    chunkIndex++;
        //}

        return Task.FromResult((IEnumerable<IngestedChunk>)chunks);
    }
}
