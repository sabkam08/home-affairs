using System.Security.Cryptography;
using HomeAffairs.Application.Abstractions.Documents;

namespace HomeAffairs.Infrastructure.Documents;

internal sealed class Sha256DocumentHashProvider
    : IDocumentHashProvider
{
    public string ComputeSha256(
        ReadOnlySpan<byte> content)
    {
        var hash =
            SHA256.HashData(content);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}
