using System.Buffers.Binary;
using Bloodline.Extraction.Icons;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace Bloodline.Tests.Extraction.Icons;

public sealed class PixelDecoderTests
{
    [Fact]
    public void Bc1DecodesFourColoursInRowOrder()
    {
        byte[] block = [0, 248, 224, 7, 0xE4, 0x1B, 0xE4, 0x1B];
        var rgba = PixelDecoder.Decode(EPixelFormat.PF_DXT1, 4, 4, block);
        Assert.Equal<byte>(
            [255, 0, 0, 255, 0, 255, 0, 255, 170, 85, 0, 255, 85, 170, 0, 255],
            rgba[..16]
        );
        Assert.Equal<byte>(
            [85, 170, 0, 255, 170, 85, 0, 255, 0, 255, 0, 255, 255, 0, 0, 255],
            rgba[16..32]
        );
    }

    [Theory]
    [InlineData(0, 65535)]
    [InlineData(0, 0)]
    public void Bc1UsesOneBitAlphaWhenEndpointsAreNotDescending(ushort first, ushort second)
    {
        var block = new byte[8];
        BinaryPrimitives.WriteUInt16LittleEndian(block, first);
        BinaryPrimitives.WriteUInt16LittleEndian(block.AsSpan(2), second);
        block[4] = 0xE4;
        var rgba = PixelDecoder.Decode(EPixelFormat.PF_DXT1, 4, 4, block);
        var midpoint = second == 0 ? (byte)0 : (byte)127;
        Assert.Equal<byte>([midpoint, midpoint, midpoint, 255], rgba[8..12]);
        Assert.Equal<byte>([0, 0, 0, 0], rgba[12..16]);
    }

    [Fact]
    public void Bc1ClipsPartialBlocksWithoutChangingBlockOrdering()
    {
        byte[] blocks =
        [
            0,
            248,
            0,
            0,
            0,
            0,
            0,
            0,
            224,
            7,
            0,
            0,
            0,
            0,
            0,
            0,
            31,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            255,
            255,
            0,
            0,
            0,
            0,
            0,
            0,
        ];
        var rgba = PixelDecoder.Decode(EPixelFormat.PF_DXT1, 5, 5, blocks);
        Assert.Equal<byte>([255, 0, 0, 255], rgba[..4]);
        Assert.Equal<byte>([0, 255, 0, 255], rgba[16..20]);
        Assert.Equal<byte>([0, 0, 255, 255], rgba[80..84]);
        Assert.Equal<byte>([255, 255, 255, 255], rgba[96..100]);
    }

    [Theory]
    [InlineData(240, 30, new byte[] { 240, 30, 210, 180, 150, 120, 90, 60 })]
    [InlineData(10, 110, new byte[] { 10, 110, 30, 50, 70, 90, 0, 255 })]
    public void Bc3DecodesBothAlphaInterpolationModes(byte first, byte second, byte[] expected)
    {
        var block = new byte[16];
        AlphaBlock(first, second).CopyTo(block, 0);
        // BC3 colour always has four entries, even when colour endpoints would select BC1 transparency.
        block[10] = block[11] = 255;
        block.AsSpan(12).Fill(255);
        var rgba = PixelDecoder.Decode(EPixelFormat.PF_DXT5, 4, 4, block);
        Assert.Equal(
            expected.Concat(expected),
            Enumerable.Range(0, 16).Select(i => rgba[i * 4 + 3])
        );
        Assert.Equal<byte>([170, 170, 170], rgba[..3]);
    }

    [Fact]
    public void Bc5KeepsRedAndGreenChannelsSeparate()
    {
        var block = AlphaBlock(240, 30).Concat(AlphaBlock(10, 110)).ToArray();
        var rgba = PixelDecoder.Decode(EPixelFormat.PF_BC5, 4, 4, block);
        Assert.Equal<byte>([240, 10, 0, 255, 30, 110, 0, 255, 210, 30, 0, 255], rgba[..12]);
        Assert.Equal<byte>([60, 255, 0, 255], rgba[28..32]);
    }

    [Fact]
    public void BgraSwizzlesWithoutFlippingRowsOrPremultiplyingAlpha()
    {
        var rgba = PixelDecoder.Decode(
            EPixelFormat.PF_B8G8R8A8,
            1,
            2,
            [10, 20, 30, 40, 50, 60, 70, 0]
        );
        Assert.Equal<byte>([30, 20, 10, 40, 70, 60, 50, 0], rgba);
    }

    [Fact]
    public void FloatRgbaMapsLinearColourToSrgbButKeepsAlphaLinear()
    {
        var bytes = new byte[16];
        Half[] channels =
        [
            (Half)0,
            (Half)0.5,
            (Half)1,
            (Half)0.5,
            (Half)(-1),
            (Half)2,
            (Half)0.003,
            (Half)0,
        ];
        for (var i = 0; i < channels.Length; i++)
            BinaryPrimitives.WriteUInt16LittleEndian(
                bytes.AsSpan(i * 2),
                BitConverter.HalfToUInt16Bits(channels[i])
            );
        Assert.Equal<byte>(
            [0, 188, 255, 128, 0, 255, 10, 0],
            PixelDecoder.Decode(EPixelFormat.PF_FloatRGBA, 2, 1, bytes)
        );
    }

    [Fact]
    public void RejectsTruncatedMipAndUnsupportedFormat()
    {
        Assert.Throws<InvalidDataException>(() =>
            PixelDecoder.Decode(EPixelFormat.PF_DXT5, 4, 4, new byte[15])
        );
        Assert.Throws<NotSupportedException>(() =>
            PixelDecoder.Decode(EPixelFormat.PF_Unknown, 4, 4, [])
        );
    }

    private static byte[] AlphaBlock(byte first, byte second)
    {
        var block = new byte[8];
        block[0] = first;
        block[1] = second;
        ulong indices = 0;
        for (var i = 0; i < 16; i++)
            indices |= (ulong)(i % 8) << (3 * i);
        for (var i = 0; i < 6; i++)
            block[i + 2] = (byte)(indices >> (8 * i));
        return block;
    }
}
