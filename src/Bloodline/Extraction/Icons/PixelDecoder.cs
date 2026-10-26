using System.Buffers.Binary;
using CUE4Parse.UE4.Assets.Exports.Texture;

namespace Bloodline.Extraction.Icons;

internal static class PixelDecoder
{
    public static byte[] Decode(EPixelFormat format, int width, int height, ReadOnlySpan<byte> data)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        var blockBytes = format switch
        {
            EPixelFormat.PF_DXT1 => 8,
            EPixelFormat.PF_DXT5 or EPixelFormat.PF_BC5 => 16,
            EPixelFormat.PF_B8G8R8A8 => 0,
            EPixelFormat.PF_FloatRGBA => 0,
            _ => throw new NotSupportedException($"Unsupported pixel format {format}."),
        };
        var expected =
            blockBytes == 0
                ? checked(width * height * (format == EPixelFormat.PF_FloatRGBA ? 8 : 4))
                : checked(((width + 3) / 4) * ((height + 3) / 4) * blockBytes);
        if (data.Length != expected)
            throw new InvalidDataException(
                $"{format} mip needs {expected} bytes, found {data.Length}."
            );

        var rgba = new byte[checked(width * height * 4)];
        if (format == EPixelFormat.PF_B8G8R8A8)
        {
            for (var i = 0; i < rgba.Length; i += 4)
            {
                rgba[i] = data[i + 2];
                rgba[i + 1] = data[i + 1];
                rgba[i + 2] = data[i];
                rgba[i + 3] = data[i + 3];
            }
            return rgba;
        }
        if (format == EPixelFormat.PF_FloatRGBA)
        {
            for (var i = 0; i < rgba.Length; i++)
            {
                var value = (float)
                    BitConverter.UInt16BitsToHalf(
                        BinaryPrimitives.ReadUInt16LittleEndian(data[(i * 2)..])
                    );
                if (!float.IsFinite(value))
                    throw new InvalidDataException("FloatRGBA mip contains a non-finite channel.");
                value = Math.Clamp(value, 0, 1);
                // Render-target FloatRGBA stores linear light. PNG colour bytes are sRGB, but alpha stays linear.
                if (i % 4 != 3)
                    value =
                        value <= 0.0031308f
                            ? 12.92f * value
                            : 1.055f * MathF.Pow(value, 1f / 2.4f) - 0.055f;
                rgba[i] = (byte)MathF.Round(value * 255);
            }
            return rgba;
        }

        Span<byte> colours = stackalloc byte[16];
        Span<byte> first = stackalloc byte[16];
        Span<byte> second = stackalloc byte[16];
        var offset = 0;
        for (var by = 0; by < height; by += 4)
        {
            for (var bx = 0; bx < width; bx += 4)
            {
                var block = data.Slice(offset, blockBytes);
                offset += blockBytes;
                if (format == EPixelFormat.PF_BC5)
                {
                    DecodeAlpha(block, first);
                    DecodeAlpha(block[8..], second);
                }
                else
                {
                    if (format == EPixelFormat.PF_DXT5)
                        DecodeAlpha(block, first);
                    DecodeColours(
                        format == EPixelFormat.PF_DXT5 ? block[8..] : block,
                        colours,
                        format == EPixelFormat.PF_DXT1
                    );
                }
                var colourIndices = BinaryPrimitives.ReadUInt32LittleEndian(
                    block[(blockBytes - 4)..]
                );
                for (var y = 0; y < 4 && by + y < height; y++)
                {
                    for (var x = 0; x < 4 && bx + x < width; x++)
                    {
                        var pixel = y * 4 + x;
                        var destination = rgba.AsSpan(((by + y) * width + bx + x) * 4, 4);
                        if (format == EPixelFormat.PF_BC5)
                        {
                            // BC5 stores only red and green. Do not invent a blue normal-map channel for an icon.
                            destination[0] = first[pixel];
                            destination[1] = second[pixel];
                            destination[2] = 0;
                            destination[3] = 255;
                        }
                        else
                        {
                            colours
                                .Slice((int)((colourIndices >> (pixel * 2)) & 3) * 4, 4)
                                .CopyTo(destination);
                            if (format == EPixelFormat.PF_DXT5)
                                destination[3] = first[pixel];
                        }
                    }
                }
            }
        }
        return rgba;
    }

    private static void DecodeColours(
        ReadOnlySpan<byte> block,
        Span<byte> colours,
        bool allowTransparent
    )
    {
        var c0 = BinaryPrimitives.ReadUInt16LittleEndian(block);
        var c1 = BinaryPrimitives.ReadUInt16LittleEndian(block[2..]);
        Expand565(c0, colours);
        Expand565(c1, colours[4..]);
        if (allowTransparent && c0 <= c1)
        {
            for (var c = 0; c < 3; c++)
                colours[8 + c] = (byte)((colours[c] + colours[4 + c]) / 2);
            colours[11] = 255;
            colours[12..16].Clear();
        }
        else
        {
            for (var c = 0; c < 3; c++)
            {
                colours[8 + c] = (byte)((2 * colours[c] + colours[4 + c]) / 3);
                colours[12 + c] = (byte)((colours[c] + 2 * colours[4 + c]) / 3);
            }
            colours[11] = colours[15] = 255;
        }
    }

    private static void Expand565(ushort value, Span<byte> rgba)
    {
        var r = (value >> 11) & 31;
        var g = (value >> 5) & 63;
        var b = value & 31;
        rgba[0] = (byte)((r << 3) | (r >> 2));
        rgba[1] = (byte)((g << 2) | (g >> 4));
        rgba[2] = (byte)((b << 3) | (b >> 2));
        rgba[3] = 255;
    }

    private static void DecodeAlpha(ReadOnlySpan<byte> block, Span<byte> pixels)
    {
        Span<byte> values = stackalloc byte[8];
        values[0] = block[0];
        values[1] = block[1];
        var sevenStep = values[0] > values[1];
        var divisor = sevenStep ? 7 : 5;
        for (var i = 1; i < divisor; i++)
            values[i + 1] = (byte)(((divisor - i) * values[0] + i * values[1]) / divisor);
        if (!sevenStep)
        {
            values[6] = 0;
            values[7] = 255;
        }
        ulong indices = 0;
        for (var i = 0; i < 6; i++)
            indices |= (ulong)block[i + 2] << (8 * i);
        for (var i = 0; i < 16; i++)
            pixels[i] = values[(int)((indices >> (3 * i)) & 7)];
    }
}
