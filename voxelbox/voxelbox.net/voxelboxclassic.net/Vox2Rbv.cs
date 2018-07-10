using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace voxelbox.net
{
    public class Vox2Rbv
    {
        public static byte[] ReadFile(FileStream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            int magic, version;
            List<CHUNK> chunks = new List<CHUNK>();

            magic = reader.ReadInt32();
            if (magic != VOX_)
            {
                return null;
            }

            version = reader.ReadInt32();
            if (version < 150)
            {
                Console.WriteLine("format too old! (" + version + ")");
                return null;
            }
            else if (version > 150)
            {
                Console.WriteLine("format is newer than 150!");
                Console.WriteLine("to continue, change the version");
                Console.WriteLine("at pos 0x4 to 0x96000000");
                return null;
            }

            ParseChunks(reader, chunks);

            SIZEBlock sb;
            int sx;
            int sy;
            int sz;
            if (ContainsInstance<SIZEBlock>(chunks))
            {
                sb = GetInstance<SIZEBlock>(chunks);
                sx = sb.sizeX;
                sy = sb.sizeY;
                sz = sb.sizeZ;
            }
            else
            {
                Console.WriteLine("no size detected! is the file corrupt?");
                return null;
            }

            List<RGBABlock.COLORB> colors = new List<RGBABlock.COLORB>();
            if (ContainsInstance<RGBABlock>(chunks))
            {
                RGBABlock rgba = GetInstance<RGBABlock>(chunks);
                for (int i = 0; i < 256; i++)
                {
                    colors.Add(rgba.colors[i]);
                }
            }
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    RGBABlock.COLORB color = new RGBABlock.COLORB();
                    color.a = (byte)((DEFAULT_PALETTE[i] & 0xFF000000) >> 24);
                    color.r = (byte)((DEFAULT_PALETTE[i] & 0x00FF0000) >> 16);
                    color.g = (byte)((DEFAULT_PALETTE[i] & 0x0000FF00) >> 8);
                    color.b = (byte)((DEFAULT_PALETTE[i] & 0x000000FF));
                    colors.Add(color);
                }
            }

            List<byte> usedMaterials = new List<byte>();
            if (ContainsInstance<XYZIBlock>(chunks))
            {
                XYZIBlock xyzi = GetInstance<XYZIBlock>(chunks);
                int voxelCount = sx * sy * sz;

                for (int i = 0; i < xyzi.voxels.Length; i++)
                {
                    if (!usedMaterials.Contains(xyzi.voxels[i].m))
                    {
                        usedMaterials.Add(xyzi.voxels[i].m);
                    }
                }
                usedMaterials.Sort();

                int voxelDataStart = 4 + (3 * usedMaterials.Count);
                int dataSize = voxelDataStart + voxelCount;

                byte[] modelData = new byte[dataSize];

                modelData[0] = (byte)sx;
                modelData[1] = (byte)sy;
                modelData[2] = (byte)sz;
                modelData[3] = (byte)usedMaterials.Count;
                for (int i = 0; i < usedMaterials.Count; i++)
                {
                    RGBABlock.COLORB color = colors[usedMaterials[i] - 1];
                    modelData[i * 3 + 4] = color.r;
                    modelData[i * 3 + 5] = color.g;
                    modelData[i * 3 + 6] = color.b;
                }

                for (int i = voxelDataStart; i < dataSize; i++)
                    modelData[i] = 0;

                for (int i = 0; i < xyzi.numVoxels; i++)
                {
                    XYZIBlock.XYZIB xyzib = xyzi.voxels[i];
                    int x = xyzib.x;
                    int y = xyzib.y;
                    int z = xyzib.z;
                    byte mat = (byte)usedMaterials.FindIndex(m => m == xyzib.m);
                    int index = x + y * sx + z * sx * sy;
                    modelData[voxelDataStart + index] = (byte)(mat + 1);
                }

                stream.Close();
                return modelData;
            }
            else
            {
                Console.WriteLine("no blocks detected! is the file corrupt?");
                return null;
            }
        }

        private static void ParseChunks(BinaryReader reader, List<CHUNK> chunks)
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int block = reader.ReadInt32();
                int contentLen = reader.ReadInt32();
                int childrenLen = reader.ReadInt32();
                switch (block)
                {
                    case VOX_:
                        Console.WriteLine("should not have encountered \"VOX\" here!");
                        break;
                    case PACK:
                        PACKBlock pack = new PACKBlock();
                        pack.id = block;
                        pack.contentLen = contentLen;
                        pack.childrenLen = childrenLen;
                        pack.numModels = reader.ReadInt32();
                        chunks.Add(pack);
                        break;
                    case SIZE:
                        SIZEBlock size = new SIZEBlock();
                        size.id = block;
                        size.contentLen = contentLen;
                        size.childrenLen = childrenLen;
                        size.sizeX = reader.ReadInt32();
                        size.sizeY = reader.ReadInt32();
                        size.sizeZ = reader.ReadInt32();
                        chunks.Add(size);
                        break;
                    case XYZI:
                        XYZIBlock xyzi = new XYZIBlock();
                        xyzi.id = block;
                        xyzi.contentLen = contentLen;
                        xyzi.childrenLen = childrenLen;
                        xyzi.numVoxels = reader.ReadInt32();
                        xyzi.voxels = new XYZIBlock.XYZIB[xyzi.numVoxels];
                        for (int i = 0; i < xyzi.numVoxels; i++)
                        {
                            xyzi.voxels[i] = new XYZIBlock.XYZIB();
                            xyzi.voxels[i].x = reader.ReadByte();
                            xyzi.voxels[i].y = reader.ReadByte();
                            xyzi.voxels[i].z = reader.ReadByte();
                            xyzi.voxels[i].m = reader.ReadByte();
                        }
                        chunks.Add(xyzi);
                        break;
                    case RGBA:
                        RGBABlock rgba = new RGBABlock();
                        rgba.id = block;
                        rgba.contentLen = contentLen;
                        rgba.childrenLen = childrenLen;
                        rgba.colors = new RGBABlock.COLORB[256];
                        for (int i = 0; i < 256; i++)
                        {
                            rgba.colors[i] = new RGBABlock.COLORB();
                            rgba.colors[i].r = reader.ReadByte();
                            rgba.colors[i].g = reader.ReadByte();
                            rgba.colors[i].b = reader.ReadByte();
                            rgba.colors[i].a = reader.ReadByte();
                        }
                        chunks.Add(rgba);
                        break;
                    default:
                        CHUNK genericChunk = new CHUNK();
                        genericChunk.id = block;
                        genericChunk.contentLen = contentLen;
                        genericChunk.childrenLen = childrenLen;
                        chunks.Add(genericChunk);
                        break;
                }
            }
        }

        private class CHUNK
        {
            public int id;
            public int contentLen;
            public int childrenLen;
        }

        private class PACKBlock : CHUNK
        {
            public int numModels;
        }

        private class SIZEBlock : CHUNK
        {
            public int sizeX;
            public int sizeY;
            public int sizeZ;
        }

        private class XYZIBlock : CHUNK
        {
            public int numVoxels;
            public XYZIB[] voxels;
            public class XYZIB
            {
                public byte x;
                public byte y;
                public byte z;
                public byte m;
            }
        }

        private class RGBABlock : CHUNK
        {
            public COLORB[] colors;
            public class COLORB
            {
                public byte r;
                public byte g;
                public byte b;
                public byte a;
            }
        }
        private static bool ContainsInstance<T>(List<CHUNK> list)
        {
            return list.OfType<T>().Any();
        }
        private static T GetInstance<T>(List<CHUNK> list)
        {
            return list.OfType<T>().FirstOrDefault();
        }

        private const int VOX_ = 0x20584F56;
        private const int PACK = 0x4B434150;
        private const int SIZE = 0x455A4953;
        private const int XYZI = 0x495A5958;
        private const int RGBA = 0x41424752;

        private static readonly uint[] DEFAULT_PALETTE = new uint[] {
            0x00000000, 0xffffffff, 0xffccffff, 0xff99ffff, 0xff66ffff, 0xff33ffff, 0xff00ffff, 0xffffccff, 0xffccccff,
            0xff99ccff, 0xff66ccff, 0xff33ccff, 0xff00ccff, 0xffff99ff, 0xffcc99ff, 0xff9999ff, 0xff6699ff, 0xff3399ff,
            0xff0099ff, 0xffff66ff, 0xffcc66ff, 0xff9966ff, 0xff6666ff, 0xff3366ff, 0xff0066ff, 0xffff33ff, 0xffcc33ff,
            0xff9933ff, 0xff6633ff, 0xff3333ff, 0xff0033ff, 0xffff00ff, 0xffcc00ff, 0xff9900ff, 0xff6600ff, 0xff3300ff,
            0xff0000ff, 0xffffffcc, 0xffccffcc, 0xff99ffcc, 0xff66ffcc, 0xff33ffcc, 0xff00ffcc, 0xffffcccc, 0xffcccccc,
            0xff99cccc, 0xff66cccc, 0xff33cccc, 0xff00cccc, 0xffff99cc, 0xffcc99cc, 0xff9999cc, 0xff6699cc, 0xff3399cc,
            0xff0099cc, 0xffff66cc, 0xffcc66cc, 0xff9966cc, 0xff6666cc, 0xff3366cc, 0xff0066cc, 0xffff33cc, 0xffcc33cc,
            0xff9933cc, 0xff6633cc, 0xff3333cc, 0xff0033cc, 0xffff00cc, 0xffcc00cc, 0xff9900cc, 0xff6600cc, 0xff3300cc,
            0xff0000cc, 0xffffff99, 0xffccff99, 0xff99ff99, 0xff66ff99, 0xff33ff99, 0xff00ff99, 0xffffcc99, 0xffcccc99,
            0xff99cc99, 0xff66cc99, 0xff33cc99, 0xff00cc99, 0xffff9999, 0xffcc9999, 0xff999999, 0xff669999, 0xff339999,
            0xff009999, 0xffff6699, 0xffcc6699, 0xff996699, 0xff666699, 0xff336699, 0xff006699, 0xffff3399, 0xffcc3399,
            0xff993399, 0xff663399, 0xff333399, 0xff003399, 0xffff0099, 0xffcc0099, 0xff990099, 0xff660099, 0xff330099,
            0xff000099, 0xffffff66, 0xffccff66, 0xff99ff66, 0xff66ff66, 0xff33ff66, 0xff00ff66, 0xffffcc66, 0xffcccc66,
            0xff99cc66, 0xff66cc66, 0xff33cc66, 0xff00cc66, 0xffff9966, 0xffcc9966, 0xff999966, 0xff669966, 0xff339966,
            0xff009966, 0xffff6666, 0xffcc6666, 0xff996666, 0xff666666, 0xff336666, 0xff006666, 0xffff3366, 0xffcc3366,
            0xff993366, 0xff663366, 0xff333366, 0xff003366, 0xffff0066, 0xffcc0066, 0xff990066, 0xff660066, 0xff330066,
            0xff000066, 0xffffff33, 0xffccff33, 0xff99ff33, 0xff66ff33, 0xff33ff33, 0xff00ff33, 0xffffcc33, 0xffcccc33,
            0xff99cc33, 0xff66cc33, 0xff33cc33, 0xff00cc33, 0xffff9933, 0xffcc9933, 0xff999933, 0xff669933, 0xff339933,
            0xff009933, 0xffff6633, 0xffcc6633, 0xff996633, 0xff666633, 0xff336633, 0xff006633, 0xffff3333, 0xffcc3333,
            0xff993333, 0xff663333, 0xff333333, 0xff003333, 0xffff0033, 0xffcc0033, 0xff990033, 0xff660033, 0xff330033,
            0xff000033, 0xffffff00, 0xffccff00, 0xff99ff00, 0xff66ff00, 0xff33ff00, 0xff00ff00, 0xffffcc00, 0xffcccc00,
            0xff99cc00, 0xff66cc00, 0xff33cc00, 0xff00cc00, 0xffff9900, 0xffcc9900, 0xff999900, 0xff669900, 0xff339900,
            0xff009900, 0xffff6600, 0xffcc6600, 0xff996600, 0xff666600, 0xff336600, 0xff006600, 0xffff3300, 0xffcc3300,
            0xff993300, 0xff663300, 0xff333300, 0xff003300, 0xffff0000, 0xffcc0000, 0xff990000, 0xff660000, 0xff330000,
            0xff0000ee, 0xff0000dd, 0xff0000bb, 0xff0000aa, 0xff000088, 0xff000077, 0xff000055, 0xff000044, 0xff000022,
            0xff000011, 0xff00ee00, 0xff00dd00, 0xff00bb00, 0xff00aa00, 0xff008800, 0xff007700, 0xff005500, 0xff004400,
            0xff002200, 0xff001100, 0xffee0000, 0xffdd0000, 0xffbb0000, 0xffaa0000, 0xff880000, 0xff770000, 0xff550000,
            0xff440000, 0xff220000, 0xff110000, 0xffeeeeee, 0xffdddddd, 0xffbbbbbb, 0xffaaaaaa, 0xff888888, 0xff777777,
            0xff555555, 0xff444444, 0xff222222, 0xff111111
        };
    }
}
