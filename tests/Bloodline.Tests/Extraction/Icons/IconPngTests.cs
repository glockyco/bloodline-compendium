using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Bloodline.Extraction.Icons;

namespace Bloodline.Tests.Extraction.Icons;

public sealed class IconPngTests
{
    [Fact]
    public void WritesRgbaPngWithValidChunksAndUnfilteredRows()
    {
        byte[] pixels = [255, 0, 0, 255, 0, 255, 0, 128, 0, 0, 255, 0, 20, 40, 60, 80];
        var png = Encode(2, 2, pixels, CompressionLevel.SmallestSize);
        Assert.Equal<byte>([137, 80, 78, 71, 13, 10, 26, 10], png[..8]);
        var chunks = ReadChunks(png);
        Assert.Equal(["IHDR", "IDAT", "IEND"], chunks.Select(x => x.Type));
        Assert.Equal<byte>([0, 0, 0, 2, 0, 0, 0, 2, 8, 6, 0, 0, 0], chunks[0].Data);
        Assert.Empty(chunks[2].Data);
        using var compressed = new MemoryStream(chunks[1].Data);
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        using var scanlines = new MemoryStream();
        zlib.CopyTo(scanlines);
        Assert.Equal<byte>(
            [0, 255, 0, 0, 255, 0, 255, 0, 128, 0, 0, 0, 255, 0, 20, 40, 60, 80],
            scanlines.ToArray()
        );
    }

    [Fact]
    public void PixelHashDoesNotDependOnPngCompressionAndIncludesDimensions()
    {
        var pixels = Enumerable.Repeat((byte)42, 64 * 4).ToArray();
        var uncompressed = Encode(8, 8, pixels, CompressionLevel.NoCompression);
        var compressed = Encode(8, 8, pixels, CompressionLevel.SmallestSize);
        Assert.NotEqual(uncompressed, compressed);
        var hash = IconPng.PixelHash(8, 8, pixels);
        Assert.Equal(hash, IconPng.PixelHash(8, 8, InflatePixels(uncompressed, 8, 8)));
        Assert.Equal(hash, IconPng.PixelHash(8, 8, InflatePixels(compressed, 8, 8)));
        Assert.NotEqual(hash, IconPng.PixelHash(4, 16, pixels));
        var identity = new byte[pixels.Length + 8];
        identity[0] = identity[4] = 8;
        pixels.CopyTo(identity, 8);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(identity)), hash);
    }

    private static byte[] Encode(int width, int height, byte[] pixels, CompressionLevel compression)
    {
        using var stream = new MemoryStream();
        IconPng.Write(stream, width, height, pixels, compression);
        return stream.ToArray();
    }

    private static byte[] InflatePixels(byte[] png, int width, int height)
    {
        using var compressed = new MemoryStream(ReadChunks(png).Single(x => x.Type == "IDAT").Data);
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        var pixels = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            Assert.Equal(0, zlib.ReadByte());
            zlib.ReadExactly(pixels.AsSpan(row * width * 4, width * 4));
        }
        Assert.Equal(-1, zlib.ReadByte());
        return pixels;
    }

    private static List<(string Type, byte[] Data)> ReadChunks(byte[] png)
    {
        var chunks = new List<(string, byte[])>();
        var offset = 8;
        while (offset < png.Length)
        {
            var size = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset));
            var typeAndData = png.AsSpan(offset + 4, size + 4);
            Assert.Equal(
                ReferenceCrc(typeAndData),
                BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(offset + size + 8))
            );
            chunks.Add((Encoding.ASCII.GetString(typeAndData[..4]), typeAndData[4..].ToArray()));
            offset += size + 12;
        }
        Assert.Equal(png.Length, offset);
        return chunks;
    }

    private static uint ReferenceCrc(ReadOnlySpan<byte> bytes)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ ((crc & 1) == 1 ? 0xEDB88320 : 0);
        }
        return ~crc;
    }
}
