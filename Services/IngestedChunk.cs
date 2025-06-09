using Microsoft.Extensions.VectorData;

namespace CodeExplainer.Services;

public class IngestedChunk
{
    // 1536 is the default vector size for the OpenAI text-embedding-3-small model
    // 768 is the default vector size for the nomic-embed-text:latest
    private const int VectorDimensions = 768; 
    private const string VectorDistanceFunction = DistanceFunction.CosineDistance;

    [VectorStoreKey]
    public required string Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public required string Text { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public string? Vector => Text;
}
