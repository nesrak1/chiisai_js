## 小さい for upcoming js13kgames comp

![Preview](https://user-images.githubusercontent.com/12544505/42487313-13b5c0ba-83c6-11e8-8097-a3900ed8c42c.png)

This is a new WIP engine that I'm creating that makes a very small WebGL program without all the extra matrix libraries and sample content. You can easily strip out the voxel model generator, controls, and shaders to start from scratch without having to spend hours finding what works and doesn't. With the arrow model, the engine and shaders were compressed to 1.72kb. That gives you a whole ~11kb to do whatever scripting and modeling you want.

## Generating models

Yes, I know, I like C# too much. I've modified the Java version of voxelbox to work in C# and to support the model format that chiisai uses. You can convert any model from MagicaVoxel or Goxel to a much smaller format to lower model sizes. Make sure to `Export -> vox` instead of using the saved version (I haven't tested if it breaks but you should probably export just to be on the safe side.) To convert the .vox file to a .txt you can copy from, use the following command:

`voxelbox mdl.vox mdl.txt`

(Depending on the size of the model, this can take a while.)

Once it's finished, copy the contents of the txt file into the data file and call `addSceneObj(tfm(x,y,z),dataIndex)` in setup to create the scene object.

### Model format

`[material count] {materials} {volumes} <;> <voxels>`

Materials: `[r] [g] [b]`

Volumes: `[x1] [y1] [z1] [x2] [y2] [z2] [mat]`

Voxels: `[x] [y] [z] [mat]`

Every item in square brackets is a single base 36 character (alpha numeric)

The `;` is placed if there are any single voxels by themselves.

## Bugs

* Models with similar rgb will create duplicate materials
* Large models will probably crash the program
* The model generated is flipped (although this is compensated for in the engine)