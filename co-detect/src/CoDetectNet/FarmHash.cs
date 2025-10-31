using System;
using FastHashes;

namespace CoDetectNet
{
    /// <summary>
    /// FarmHash wrapper using FastHashes library
    /// Provides compatibility with Python's pyfarmhash.fingerprint64
    /// </summary>
    internal class FarmHash
    {
        private readonly FarmHash64 _hasher;

        public FarmHash()
        {
            _hasher = new FarmHash64();
        }

        /// <summary>
        /// Computes 64-bit FarmHash fingerprint
        /// Compatible with Python's pyfarmhash.fingerprint64
        /// </summary>
        public ulong Fingerprint64(byte[] data)
        {
            // Handle null as 0 (consistent behavior)
            if (data == null)
                return 0;

            // Empty byte array should still be hashed (Python farmhash.fingerprint64(b'') returns non-zero)
            // FastHashes library will handle empty arrays correctly
            var hashBytes = _hasher.ComputeHash(data ?? Array.Empty<byte>());
            
            // Convert byte array to ulong (little-endian)
            if (hashBytes.Length >= 8)
            {
                return BitConverter.ToUInt64(hashBytes, 0);
            }
            
            // Fallback for shorter hashes (shouldn't happen with 64-bit hash)
            ulong result = 0;
            for (int i = 0; i < Math.Min(hashBytes.Length, 8); i++)
            {
                result |= ((ulong)hashBytes[i]) << (i * 8);
            }
            return result;
        }
    }
}

