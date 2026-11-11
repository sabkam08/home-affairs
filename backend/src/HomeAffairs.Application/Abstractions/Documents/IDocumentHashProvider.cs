namespace HomeAffairs.Application.Abstractions.Documents;

public interface IDocumentHashProvider
{
    string ComputeSha256(
        ReadOnlySpan<byte> content);
}
