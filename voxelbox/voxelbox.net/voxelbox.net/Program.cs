using System;
using System.Collections.Generic;
using System.IO;
using static voxelbox.net.VoxelRaster;

namespace voxelbox.net
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("not enough args - voxelbox [file] [out]");
            }
            else
            {
                Console.WriteLine("voxelbox.net v1");
                using (FileStream stream = new FileStream(args[0], FileMode.Open))
                {
                    byte[] voxelData = Vox2Rbv.ReadFile(stream);
                    if (voxelData == null)
                        return;
                    VoxelModel voxelModel = new VoxelRaster().CreateVBFromVoxels(voxelData);
                    using (FileStream outStream = new FileStream(args[1], FileMode.Create))
                    using (StreamWriter outStreamWriter = new StreamWriter(outStream))
                    {
                        List<VoxelColor> colors = voxelModel.colors;
                        List<VoxelVolume> volumes = voxelModel.volumes;
                        if (colors.Count > 34)
                        {
                            Console.WriteLine("format does not support > 34 materials");
                            return;
                        }
                        outStreamWriter.Write(IntToBase36(colors.Count));
                        for (int i = 0; i < colors.Count; i++)
                        {
                            int r = (int)Math.Round(colors[i].r / 32d);
                            int g = (int)Math.Round(colors[i].g / 32d);
                            int b = (int)Math.Round(colors[i].b / 32d);
                            outStreamWriter.Write("" + r + g + b);
                        }
                        bool singleVoxels = false;
                        for (int i = 0; i < volumes.Count; i++)
                        {
                            VoxelVolume vol = volumes[i];
                            char x1 = IntToBase36(vol.x1);
                            char y1 = IntToBase36(vol.y1);
                            char z1 = IntToBase36(vol.z1);
                            char x2 = IntToBase36(vol.x2);
                            char y2 = IntToBase36(vol.y2);
                            char z2 = IntToBase36(vol.z2);
                            char mat = IntToBase36(vol.mat);
                            if (vol.x1 == vol.x2 && vol.y1 == vol.y2 && vol.z1 == vol.z2 && singleVoxels == false)
                            {
                                outStreamWriter.Write("Z");
                                singleVoxels = true;
                            }
                            if (!singleVoxels)
                                outStreamWriter.Write("" + x1 + y1 + z1 + x2 + y2 + z2 + mat);
                            else
                                outStreamWriter.Write("" + x1 + y1 + z1 + mat);
                        }
                        Console.WriteLine("done");
                    }
                }
            }
        }

        public static char IntToBase36(int value)
        {
            char[] baseChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            return baseChars[value];
        }
    }
}
