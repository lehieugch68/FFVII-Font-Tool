using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FFVII_Font_Tool
{
    public static class FontExtractor
    {
        #region Structure
        private struct TextureHeader
        {
            public int Width;
            public int Height;
            public int MipmapCount;
            public int FileSize;
            public int FourCCLen;
            public byte[] FourCC;
            public int TexSize;
            public long TexOffset;
        }
        private struct GlyphHeader
        {
            public byte[] Magic;
            public int GlyphCount;
            public byte[] Tail;
        }
        private struct FontGlyph
        {
            public ushort CharCode;
            public short Page;
            public short X;
            public short Y;
            public short Width;
            public short Height;
            public short XOff;
            public short YOff;
            public short XAdv;
        }
        #endregion
        private static TextureHeader ReadTextureHeader(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0x39, SeekOrigin.Begin);
            TextureHeader header = new TextureHeader();
            header.FileSize = reader.ReadInt32();
            header.Width = reader.ReadInt32();
            header.Height = reader.ReadInt32();
            header.MipmapCount = reader.ReadInt32();
            header.FourCCLen = reader.ReadInt32();
            header.FourCC = reader.ReadBytes(header.FourCCLen);
            reader.BaseStream.Seek(0x10, SeekOrigin.Current);
            header.TexSize = reader.ReadInt32();
            reader.BaseStream.Seek(0xC, SeekOrigin.Current);
            header.TexOffset = reader.BaseStream.Position;
            return header;
        }
        public static void ExtractDDS(string uexp, string output)
        {
            FileStream stream = File.Open(uexp, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            TextureHeader header = ReadTextureHeader(ref reader);
            using (MemoryStream result = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(result))
                {
                    writer.Write(Properties.Resources.DDSHeader);
                    reader.BaseStream.Position = header.TexOffset;
                    writer.Write(reader.ReadBytes(header.TexSize));
                    writer.BaseStream.Seek(0xC, SeekOrigin.Begin);
                    writer.Write(header.Height);
                    writer.Write(header.Width);
                }
                File.WriteAllBytes(output, result.ToArray());
                Console.WriteLine($"Extracted: {Path.GetFileName(output)}");
            }
            reader.Close();
            stream.Close();
        }
        public static void ImportDDS(string uexp, string dds, string output)
        {
            FileStream stream = File.Open(uexp, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            TextureHeader header = ReadTextureHeader(ref reader);
            byte[] ddsBody = File.ReadAllBytes(dds).Skip(0x94).ToArray();
            if (ddsBody.Length != header.TexSize) throw new Exception("Import data size does not match the original size!");
            using (MemoryStream result = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(result))
                {
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    writer.Write(reader.ReadBytes((int)reader.BaseStream.Length));
                    writer.BaseStream.Seek(header.TexOffset, SeekOrigin.Begin);
                    writer.Write(ddsBody);
                }
                File.WriteAllBytes(output, result.ToArray());
                Console.WriteLine($"Re-imported: {Path.GetFileName(output)}");

            }
            reader.Close();
            stream.Close();
        }
        private static GlyphHeader ReadGlyphHeader(ref BinaryReader reader)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            GlyphHeader header = new GlyphHeader();
            int magic = reader.ReadInt32();
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            if (magic == 0x0901) header.Magic = reader.ReadBytes(6);
            else header.Magic = reader.ReadBytes(8);
            header.GlyphCount = reader.ReadInt32();
            reader.BaseStream.Seek(0x12 * header.GlyphCount, SeekOrigin.Current);
            header.Tail = reader.ReadBytes((int)(reader.BaseStream.Length - reader.BaseStream.Position));
            return header;
        }
        private static FontGlyph[] ReadGlyphs(ref BinaryReader reader, GlyphHeader header)
        {
            reader.BaseStream.Seek(header.Magic.Length + 4, SeekOrigin.Begin);
            FontGlyph[] result = new FontGlyph[header.GlyphCount];
            for (int i = 0; i < header.GlyphCount; i++)
            {
                result[i].CharCode = reader.ReadUInt16();
                result[i].Page = reader.ReadInt16();
                result[i].X = reader.ReadInt16();
                result[i].Y = reader.ReadInt16();
                result[i].Width = reader.ReadInt16();
                result[i].Height = reader.ReadInt16();
                result[i].XOff = reader.ReadInt16();
                result[i].YOff = reader.ReadInt16();
                result[i].XAdv = reader.ReadInt16();
            }
            return result;
        }
        public static void ExtractGlyphs(string uexp, string output)
        {
            FileStream stream = File.Open(uexp, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            GlyphHeader header = ReadGlyphHeader(ref reader);
            FontGlyph[] glyphs = ReadGlyphs(ref reader, header);
            using (FileStream result = File.Open(output, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(result))
                {
                    foreach (var glyph in glyphs)
                    {
                        writer.WriteLine($"CharCode={glyph.CharCode}\tChar={Convert.ToChar(glyph.CharCode)}\tX={glyph.X}\tY={glyph.Y}\tWidth={glyph.Width}\tHeight={glyph.Height}\tXOffset={glyph.XOff}\tYOffset={glyph.YOff}\tXAdvance={glyph.XAdv}\tPage={glyph.Page}");
                    }
                }
            }
            Console.WriteLine($"Extracted: {Path.GetFileName(output)}");
            reader.Close();
            stream.Close();
        }
        public static void ImportGlyphs(string uexp, string input, string output, bool fnt = false, short page = -1, short xAdv = 0)
        {
            FileStream stream = File.Open(uexp, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);
            GlyphHeader header = ReadGlyphHeader(ref reader);
            FontGlyph[] fontGlyphs = ReadGlyphs(ref reader, header);
            List<FontGlyph> newGlyphs = new List<FontGlyph>();
            reader.Close();
            stream.Close();
            using (StreamReader sr = new StreamReader(input))
            {
                if (fnt)
                {
                    string temp = sr.ReadLine();

                    while (!temp.StartsWith("chars count") && !sr.EndOfStream)
                    {
                        temp = sr.ReadLine();
                    };
                }
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line) && !string.IsNullOrEmpty(line))
                    {
                        if (fnt && line.StartsWith("kernings count")) break;
                        string[] fields = fnt ? line.Split((char)32) : line.Split((char)9);
                        FontGlyph glyph = new FontGlyph();
                        try
                        {
                            glyph.CharCode = fnt ? ushort.Parse(Array.Find(fields, f => f.ToLower().StartsWith("id=")).Split((char)61)[1]) :
                                ushort.Parse(Array.Find(fields, f => f.ToLower().StartsWith("charcode=")).Split((char)61)[1]);
                            if (newGlyphs.Exists(g => g.CharCode == glyph.CharCode))
                            {
                                Console.WriteLine($"[Warning] Duplicate CharCode: {glyph.CharCode} in {Path.GetFileName(input)}");
                                continue;
                            }
                            glyph.X = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("x=")).Split((char)61)[1]);
                            glyph.Y = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("y=")).Split((char)61)[1]);
                            glyph.Width = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("width=")).Split((char)61)[1]);
                            glyph.Height = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("height=")).Split((char)61)[1]);
                            glyph.XOff = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("xoffset=")).Split((char)61)[1]);
                            glyph.YOff = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("yoffset=")).Split((char)61)[1]);
                            glyph.XAdv = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("xadvance=")).Split((char)61)[1]);
                            glyph.Page = short.Parse(Array.Find(fields, f => f.ToLower().StartsWith("page=")).Split((char)61)[1]);
                            if (fnt)
                            {
                                glyph.Page = page >= 0 ? page : glyph.Page;
                                glyph.XAdv += xAdv;
                            }
                            
                            newGlyphs.Add(glyph);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }
            using (MemoryStream result = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(result);
                writer.Write(header.Magic);
                List<FontGlyph> glyphs = new List<FontGlyph>();
                if (fnt)
                {
                    foreach (var glyph in fontGlyphs)
                    {
                        int temp = newGlyphs.FindIndex(g => g.CharCode == glyph.CharCode);
                        if (temp >= 0)
                        {
                            glyphs.Add(newGlyphs[temp]);
                            newGlyphs.RemoveAt(temp);
                        }
                        else
                        {
                            glyphs.Add(glyph);
                        }
                    }
                    foreach (var glyph in newGlyphs)
                    {
                        glyphs.Add(glyph);
                    }

                }
                else glyphs = newGlyphs;
                writer.Write(glyphs.Count);
                foreach (var glyph in glyphs)
                {
                    writer.Write(glyph.CharCode);
                    writer.Write(glyph.Page);
                    writer.Write(glyph.X);
                    writer.Write(glyph.Y);
                    writer.Write(glyph.Width);
                    writer.Write(glyph.Height);
                    writer.Write(glyph.XOff);
                    writer.Write(glyph.YOff);
                    writer.Write(glyph.XAdv);
                }
                writer.Write(header.Tail);
                File.WriteAllBytes(output, result.ToArray());
                string uasset = Path.Combine(Path.GetDirectoryName(uexp), $"{Path.GetFileNameWithoutExtension(uexp)}.uasset");
                if (File.Exists(uasset) && newGlyphs.Count != header.GlyphCount)
                {
                    string newUasset = Path.Combine(Path.GetDirectoryName(output), $"{Path.GetFileNameWithoutExtension(output)}.uasset");
                    File.Copy(uasset, newUasset, true);
                    using (FileStream uassetStream = File.Open(newUasset, FileMode.Open, FileAccess.Write))
                    {
                        using (BinaryWriter bw = new BinaryWriter(uassetStream))
                        {
                            bw.BaseStream.Position = bw.BaseStream.Length - 0x5C;
                            bw.Write((int)(writer.BaseStream.Length - 4));
                        }
                    }
                }
                Console.WriteLine($"Re-imported: {Path.GetFileName(output)}");
                writer.Close();
            }
        }
    }
}
