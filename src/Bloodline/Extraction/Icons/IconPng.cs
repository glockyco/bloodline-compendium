using System.Buffers.Binary;
using System.IO.Compression;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace Bloodline.Extraction.Icons;

internal static class IconPng
{
    public static string PixelHash(int width, int height, ReadOnlySpan<byte> rgba)
    {
        Validate(width, height, rgba);
        // Fixed little-endian dimensions prevent a differently shaped image from sharing a pixel identity.
        Span<byte> dimensions = stackalloc byte[8];
        BinaryPrimitives.WriteInt32LittleEndian(dimensions, width);
        BinaryPrimitives.WriteInt32LittleEndian(dimensions[4..], height);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(dimensions);
        hash.AppendData(rgba);
        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        hash.GetHashAndReset(digest);
        return Convert.ToHexStringLower(digest);
    }

    public static void Write(
        Stream output,
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        CompressionLevel compression = CompressionLevel.SmallestSize
    )
    {
        Validate(width, height, rgba);
        output.Write([137, 80, 78, 71, 13, 10, 26, 10]);
        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header, width);
        BinaryPrimitives.WriteInt32BigEndian(header[4..], height);
        header[8] = 8;
        header[9] = 6;
        header[10..].Clear();
        var crc = new Crc32();
        WriteChunk(output, "IHDR"u8, header, crc);
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, compression, leaveOpen: true))
        {
            var stride = checked(width * 4);
            for (var row = 0; row < height; row++)
            {
                zlib.WriteByte(0);
                zlib.Write(rgba.Slice(row * stride, stride));
            }
        }
        WriteChunk(
            output,
            "IDAT"u8,
            compressed.GetBuffer().AsSpan(0, checked((int)compressed.Length)),
            crc
        );
        WriteChunk(output, "IEND"u8, [], crc);
    }

    private static void WriteChunk(
        Stream output,
        ReadOnlySpan<byte> type,
        ReadOnlySpan<byte> data,
        Crc32 crc
    )
    {
        Span<byte> value = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(value, data.Length);
        output.Write(value);
        output.Write(type);
        output.Write(data);
        crc.Reset();
        crc.Append(type);
        crc.Append(data);
        BinaryPrimitives.WriteUInt32BigEndian(value, crc.GetCurrentHashAsUInt32());
        output.Write(value);
    }

    private static void Validate(int width, int height, ReadOnlySpan<byte> rgba)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (rgba.Length != checked(width * height * 4))
            throw new ArgumentException(
                "RGBA byte count does not match the image dimensions.",
                nameof(rgba)
            );
    }
}
