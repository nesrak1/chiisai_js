using System;
using System.Collections.Generic;

namespace voxelbox.net
{
    public class VoxelRaster
    {
        private int sx;
        private int sy;
        private int sz;
        private int size;
        private byte[] voxels;
        private short[] canvas;
        public VoxelModel CreateVBFromVoxels(byte[] data)
        {
            sx = data[0];
            sy = data[1];
            sz = data[2];
            size = sx * sy * sz;
            int colorCount = data[3];
            int voxelStart = (colorCount * 3) + 4;
            List<VoxelColor> colors = new List<VoxelColor>();
            for (int i = 4; i < voxelStart; i += 3)
            {
                colors.Add(new VoxelColor(data[i], data[i + 1], data[i + 2]));
            }
            Console.WriteLine($"converting [{sx}x{sy}x{sz}] size model");

            //first we get just the voxels
            voxels = new byte[size];
            Array.Copy(data, voxelStart, voxels, 0, size);
            //also copy it into the canvas, where we can clear voxels out
            canvas = new short[size];
            Array.Copy(data, voxelStart, canvas, 0, size);

            List<VoxelVolume> volumes = new List<VoxelVolume>();

            int overflow = size;
            while (overflow > 0)
            {
                int highestStart = -1;
                int highestEnd = -1;
                int highestMass = 0;
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        int[] mass1 = GetCoords(i);
                        int[] mass2 = GetCoords(j);
                        if (!IsSolid(mass1[0], mass1[1], mass1[2], mass2[0], mass2[1], mass2[2]))
                            continue;
                        int mass = GetMass(mass1[0], mass1[1], mass1[2], mass2[0], mass2[1], mass2[2]);
                        if (mass > highestMass)
                        {
                            highestStart = i;
                            highestEnd = j;
                            highestMass = mass;
                        }
                    }
                }
                if (highestStart == -1 || highestEnd == -1 || highestMass == 0)
                    break;
                if (highestMass == 1)
                {
                    Console.WriteLine("filling remaining voxels");
                    for (int i = 0; i < size; i++)
                    {
                        int[] loc = GetCoords(i);
                        if (GetUsed(loc[0], loc[1], loc[2]))
                        {
                            int mat = voxels[i] - 1;
                            volumes.Add(new VoxelVolume(loc[0], loc[1], loc[2], loc[0], loc[1], loc[2], mat));
                        }
                    }
                    break;
                }
                int[] vol1 = GetCoords(highestStart);
                int[] vol2 = GetCoords(highestEnd);
                int volmat = voxels[highestStart] - 1;
                ClearVolume(vol1[0], vol1[1], vol1[2], vol2[0], vol2[1], vol2[2]);
                Console.WriteLine($"adding {vol1[0]},{vol1[1]},{vol1[2]} x {vol2[0]},{vol2[1]},{vol2[2]} w/ {highestMass} mass");
                volumes.Add(new VoxelVolume(vol1[0], vol1[1], vol1[2], vol2[0], vol2[1], vol2[2], volmat));
                overflow--;
            }

            if (overflow == 0)
                Console.WriteLine("overflow reached zero, probably stuck in loop");

            return new VoxelModel(colors, volumes);
        }

        private int[] GetCoords(int idx)
        {
            int x = idx % sx;
            int y = (int)Math.Floor(idx / (double)sx) % sy;
            int z = (int)Math.Floor(idx / ((double)(sx * sy)));
            return new int[] { x, y, z };
        }

        private int GetMass(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            int mass = 0;
            for (int i = x1; i < x2 + 1; i++)
            {
                for (int j = y1; j < y2 + 1; j++)
                {
                    for (int k = z1; k < z2 + 1; k++)
                    {
                        if (GetUsed(i, j, k))
                        {
                            mass++;
                        }
                    }
                }
            }
            return mass;
        }

        private void ClearVolume(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            for (int i = x1; i < x2 + 1; i++)
            {
                for (int j = y1; j < y2 + 1; j++)
                {
                    for (int k = z1; k < z2 + 1; k++)
                    {
                        canvas[i + j * sx + k * sx * sy] = -1;
                    }
                }
            }
        }

        private bool IsSolid(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            int firstMat = -1;
            for (int i = x1; i < x2 + 1; i++)
            {
                for (int j = y1; j < y2 + 1; j++)
                {
                    for (int k = z1; k < z2 + 1; k++)
                    {
                        if (!GetNotEmpty(i, j, k))
                        {
                            return false;
                        }
                        else
                        {
                            int mat = GetMat(i, j, k);
                            if (firstMat == -1)
                            {
                                firstMat = mat;
                            }
                            else
                            {
                                if (firstMat != mat)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        private bool GetNotEmpty(int x, int y, int z)
        {
            if (x > sx - 1 || x < 0 || y > sy - 1 || y < 0 || z > sz - 1 || z < 0) return false;
            if (canvas[x + y * sx + z * sx * sy] != 0x00) return true;
            return false;
        }

        private bool GetUsed(int x, int y, int z)
        {
            if (x > sx - 1 || x < 0 || y > sy - 1 || y < 0 || z > sz - 1 || z < 0) return false;
            if (canvas[x + y * sx + z * sx * sy] > 0) return true;
            return false;
        }

        private int GetMat(int x, int y, int z)
        {
            return voxels[x + y * sx + z * sx * sy];
        }

        public class VoxelColor
        {
            public byte r;
            public byte g;
            public byte b;
            public VoxelColor(byte r, byte g, byte b)
            {
                this.r = r;
                this.g = g;
                this.b = b;
            }
        }

        public class VoxelVolume
        {
            public int x1, y1, z1;
            public int x2, y2, z2;
            public int mat;
            public VoxelVolume(int x1, int y1, int z1, int x2, int y2, int z2, int mat)
            {
                this.x1 = x1;
                this.y1 = y1;
                this.z1 = z1;
                this.x2 = x2;
                this.y2 = y2;
                this.z2 = z2;
                this.mat = mat;
            }
        }

        public class VoxelModel
        {
            public List<VoxelColor> colors;
            public List<VoxelVolume> volumes;
            public VoxelModel(List<VoxelColor> colors, List<VoxelVolume> volumes)
            {
                this.colors = colors;
                this.volumes = volumes;
            }
        }
    }
}
