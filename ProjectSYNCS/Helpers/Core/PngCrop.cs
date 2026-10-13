using System.Buffers.Binary;
using System.IO.Compression;

namespace ProjectSYNCS.Helpers;

// Crops the centre out of an 8-bit RGB / RGBA, non-interlaced PNG — the format the shipped avatars
// use — so a picture can be zoomed in at upload time without an image library and without touching
// the file on disk. Anything else throws, and the caller keeps the current picture.
internal static class PngCrop
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    // Keeps the central `keep` share of each side (0.9 = trim 5 % off every edge).
    public static byte[] CenterZoom(string path, double keep)
    {
        var data = File.ReadAllBytes(path);
        if (data.Length < 8 || !data.AsSpan(0, 8).SequenceEqual(Signature))
            throw new InvalidDataException("Not a PNG.");

        int width = 0, height = 0, bpp = 0;
        using var idat = new MemoryStream();
        for (var pos = 8; pos + 8 <= data.Length;)
        {
            var len = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos, 4));
            var type = System.Text.Encoding.ASCII.GetString(data, pos + 4, 4);
            var body = data.AsSpan(pos + 8, len);
            if (type == "IHDR")
            {
                width = (int)BinaryPrimitives.ReadUInt32BigEndian(body[..4]);
                height = (int)BinaryPrimitives.ReadUInt32BigEndian(body[4..8]);
                bpp = (body[8], body[9], body[12]) switch
                {
                    (8, 2, 0) => 3,
                    (8, 6, 0) => 4,
                    _ => throw new NotSupportedException("Only 8-bit RGB/RGBA, non-interlaced PNGs."),
                };
            }
            else if (type == "IDAT") idat.Write(body);
            else if (type == "IEND") break;
            pos += 12 + len;
        }

        var stride = width * bpp;
        var raw = new byte[(stride + 1) * height];
        idat.Position = 0;
        using (var z = new ZLibStream(idat, CompressionMode.Decompress))
            z.ReadExactly(raw);

        // Undo the per-row filters in place.
        for (var y = 0; y < height; y++)
        {
            var row = y * (stride + 1);
            var filter = raw[row];
            for (var x = 0; x < stride; x++)
            {
                int a = x >= bpp ? raw[row + 1 + x - bpp] : 0;
                int b = y > 0 ? raw[row - (stride + 1) + 1 + x] : 0;
                int c = x >= bpp && y > 0 ? raw[row - (stride + 1) + 1 + x - bpp] : 0;
                var add = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw new InvalidDataException("Bad PNG filter."),
                };
                raw[row + 1 + x] = (byte)(raw[row + 1 + x] + add);
            }
            raw[row] = 0;
        }

        var cw = (int)Math.Round(width * keep);
        var ch = (int)Math.Round(height * keep);
        var x0 = (width - cw) / 2;
        var y0 = (height - ch) / 2;
        var cropped = new byte[(cw * bpp + 1) * ch];
        for (var y = 0; y < ch; y++)
            Buffer.BlockCopy(raw, (y0 + y) * (stride + 1) + 1 + x0 * bpp, cropped, y * (cw * bpp + 1) + 1, cw * bpp);

        using var output = new MemoryStream();
        output.Write(Signature);
        var ihdr = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(0, 4), (uint)cw);
        BinaryPrimitives.WriteUInt32BigEndian(ihdr.AsSpan(4, 4), (uint)ch);
        ihdr[8] = 8;
        ihdr[9] = (byte)(bpp == 3 ? 2 : 6);
        WriteChunk(output, "IHDR", ihdr);
        using (var packed = new MemoryStream())
        {
            using (var z = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
                z.Write(cropped);
            WriteChunk(output, "IDAT", packed.ToArray());
        }
        WriteChunk(output, "IEND", []);
        return output.ToArray();
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static void WriteChunk(Stream s, string type, byte[] body)
    {
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(len, (uint)body.Length);
        s.Write(len);
        var typed = new byte[4 + body.Length];
        System.Text.Encoding.ASCII.GetBytes(type, typed);
        body.CopyTo(typed, 4);
        s.Write(typed);
        BinaryPrimitives.WriteUInt32BigEndian(len, Crc32(typed));
        s.Write(len);
    }

    private static uint Crc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        return ~crc;
    }
}
