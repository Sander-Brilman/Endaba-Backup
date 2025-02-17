using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EnDaBaServices;

public sealed class HashService
{
    public async Task<string> CalculateHashFromFile(string filePath, CancellationToken cancellationToken)
    {
        using var stream = File.OpenRead(filePath);
        using var md5 = MD5.Create();
        
        var hash = await md5.ComputeHashAsync(stream, cancellationToken);

        return Convert.ToHexString(hash);
    }
}
