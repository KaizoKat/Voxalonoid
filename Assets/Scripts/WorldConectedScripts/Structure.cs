using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Structure
{
    public static Queue<VoxelMod> MakeTree (Vector3 position, int minTrunkHeight, int maxTrunkHeight) {

        Queue<VoxelMod> queue = new Queue<VoxelMod>();

        int height = (int)(maxTrunkHeight * Noise.Get2DPerlin(new Vector2(position.x,position.z),240.0f, 3f));

        if(height < minTrunkHeight)
            height = minTrunkHeight;

        for (int i = 1; i < height; i++)
        {
            for(int y = 0; y <= 3; y++)
            {
                if(y >= 0 && y < 2)
                {
                    for(int x = -2; x <= 2; x++)
                    {
                        for(int z = -2; z <= 2; z++)
                        {
                            queue.Enqueue(new VoxelMod(new Vector3(position.x + x,position.y + height + y,position.z + z),6));
                            queue.Enqueue(new VoxelMod(new Vector3(position.x,position.y + height +y,position.z),8));
                        }
                    }
                }

                if(y == 2)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        for(int z = -1; z <= 1; z++)
                        {
                            queue.Enqueue(new VoxelMod(new Vector3(position.x + x,position.y + height + y,position.z + z),6));
                            queue.Enqueue(new VoxelMod(new Vector3(position.x,position.y+ height +y,position.z),8));
                        }
                    }
                }

                if(y == 3)
                {
                    for(int x = -1; x <= 1; x++)
                    {
                        for(int z = -1; z <= 1; z++)
                        {
                            queue.Enqueue(new VoxelMod(new Vector3(position.x + x,position.y + height + y,position.z + z),6));
                        }
                    }
                }
            }
            queue.Enqueue(new VoxelMod(new Vector3(position.x,position.y+i,position.z),8));
        }

        return queue;
    }
}