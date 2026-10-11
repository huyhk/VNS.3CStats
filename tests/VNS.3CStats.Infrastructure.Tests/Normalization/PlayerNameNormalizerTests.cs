using VNS.ThreeCStats.Infrastructure.Normalization;
using Xunit;

namespace VNS.ThreeCStats.Infrastructure.Tests.Normalization;

public sealed class PlayerNameNormalizerTests
{
    [Theory]
    [InlineData("D’AGATA Alessio", "D'AGATA ALESSIO")]
    [InlineData("D'AGATA   Alessio", "D'AGATA ALESSIO")]
    [InlineData("  VAN 'T ZELFDEN Joris  ", "VAN 'T ZELFDEN JORIS")]
    [InlineData("CIN Aygün", "CIN AYGÜN")]
    public void Normalize_FoldsSafeVariants(string source, string expected)
    {
        Assert.Equal(expected, PlayerNameNormalizer.Normalize(source));
    }
}
