using System.Security.Cryptography;
using System.Text;

namespace Fair.Application.Dispatch;

internal static class DeterministicGuid
{
    // Stable namespace for this product (random constant, never change once committed)
    private static readonly Guid Namespace = Guid.Parse("7b7f1b6d-2a7d-4b5f-a2f1-7a87f2b5c2aa");

    public static Guid ForOffer(Guid tripId, int tripVersion, Guid driverId)
        => Create(Namespace, $"offer:{tripId}:{tripVersion}:{driverId}");

    private static Guid Create(Guid @namespace, string name)
    {
        // RFC 4122-ish v5 (SHA1) deterministic GUID
        var nsBytes = @namespace.ToByteArray();
        SwapByteOrder(nsBytes);

        var nameBytes = Encoding.UTF8.GetBytes(name);

        byte[] hash;
        using (var sha1 = SHA1.Create())
            hash = sha1.ComputeHash(nsBytes.Concat(nameBytes).ToArray());

        var newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        // Set version to 5
        newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4));
        // Set variant to RFC 4122
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);

        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }

    private static void SwapByteOrder(byte[] guid)
    {
        // GUID byte order quirks (.NET vs RFC)
        void Swap(int a, int b) { (guid[a], guid[b]) = (guid[b], guid[a]); }

        Swap(0, 3); Swap(1, 2);
        Swap(4, 5);
        Swap(6, 7);
    }
}