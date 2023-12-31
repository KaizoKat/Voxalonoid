﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.IO;

public class World : MonoBehaviour {

    public Settings settings;

    public bool enableThreading = false;
    public BiomeAttributes biome;

    [Range(0f,1f)]
    public float globalLightLevel;
    public Color day;
    public Color night;

    public Transform player;
    public Vector3 spawnPosition;

    public Material material;
    public Material transparentMaterial;

    public BlockType[] blocktypes;

    Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    public ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
    public List<Chunk> chunksToUpdate = new List<Chunk>();
    public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

    bool applyingModifications = false;

    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();

    private bool _inUI = false;

    public GameObject debugScreen;

    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;

    Thread ChunkUpdateThread1;
    public object ChunkUpdateThreadLock1 = new object();

    private void Start() {
        // string jsonExport = JsonUtility.ToJson(settings);
        // File.WriteAllText(Application.dataPath + "/settings.sts", jsonExport);

        // string jsonImport = File.ReadAllText((Application.dataPath + "/settings.sts"));
        // settings = JsonUtility.FromJson<Settings>(jsonImport);

        Random.InitState(settings.seed);

        Shader.SetGlobalFloat("minGlobalLightLevel", VoxelData.minLightLevel);
        Shader.SetGlobalFloat("maxGlobalLightLevel", VoxelData.maxLightLevel);

        ChunkUpdateThread1 = new Thread(new ThreadStart(ThreadedUpdate1));

        ChunkUpdateThread1.Start();

        spawnPosition = new Vector3((VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f, VoxelData.ChunkHeight - (biome.solidGroundHeight + biome.terrainScale), (VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth) / 2f);
        GenerateWorld();
        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
        
    }

    private void Update() {

        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        Shader.SetGlobalFloat("GlobalLightLevel", globalLightLevel);
        Camera.main.backgroundColor = Color.Lerp(night,day,globalLightLevel);

        // Only update the chunks if the player has moved from the chunk they were previously on.
        if (!playerChunkCoord.Equals(playerLastChunkCoord))
            CheckViewDistance();

        if (chunksToCreate.Count > 0)
            CreateChunk();


        if (chunksToDraw.Count > 0)
            if (chunksToDraw.Peek().isEditable)
                chunksToDraw.Dequeue().CreateMesh();

        if (Input.GetKeyDown(KeyCode.F3))
            debugScreen.SetActive(!debugScreen.activeSelf);


    }

    void GenerateWorld () {

        for (int x = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; x < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; x++) {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - settings.viewDistance; z < (VoxelData.WorldSizeInChunks / 2) + settings.viewDistance; z++) {
                ChunkCoord newChunk = new ChunkCoord(x,z);
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
                chunksToCreate.Add(newChunk);

            }
        }

        player.position = spawnPosition;
        CheckViewDistance();
    }

    void CreateChunk () {

        ChunkCoord c = chunksToCreate[0];
        chunksToCreate.RemoveAt(0);
        chunks[c.x, c.z].Init();

    }

    public void UpdateChunks () {

        bool updated = false;
        int index = 0;
        lock(ChunkUpdateThreadLock1)
        {
            while (!updated && index < chunksToUpdate.Count - 1) {

                if (chunksToUpdate[index].isEditable) {
                    chunksToUpdate[index].UpdateChunk();
                    activeChunks.Add(chunksToUpdate[index].coord);
                    chunksToUpdate.RemoveAt(index);
                    updated = true;
                } else
                    index++;

            }
        }
    }


    void ThreadedUpdate1()
    {
        while(true)
        {
            if (!applyingModifications)
                ApplyModifications();
            
            if (chunksToUpdate.Count > 0)
                if(chunksToUpdate[0] != null)
                    UpdateChunks();
        }
    }

    private void OnDisable()
    {
        ChunkUpdateThread1.Abort();
    }

    void ApplyModifications () {

        applyingModifications = true;

        while (modifications.Count > 0) {

            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0) {

                VoxelMod v = queue.Dequeue();

                ChunkCoord c = GetChunkCoordFromVector3(v.position);

                if (chunks[c.x, c.z] == null) {
                    chunks[c.x, c.z] = new Chunk(c, this);
                    chunksToCreate.Add(c);
                }

                chunks[c.x, c.z].modifications.Enqueue(v);

            }
        }

        applyingModifications = false;

    }

    ChunkCoord GetChunkCoordFromVector3 (Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);

    }

    public Chunk GetChunkFromVector3 (Vector3 pos) {

        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];

    }

    void CheckViewDistance () {



        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        activeChunks.Clear();

        for (int x = coord.x - settings.viewDistance; x < coord.x + settings.viewDistance; x++) {
            for (int z = coord.z - settings.viewDistance; z < coord.z + settings.viewDistance; z++) {

                if (IsChunkInWorld (new ChunkCoord (x, z))) {


                    if (chunks[x, z] == null) {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
                        chunksToCreate.Add(new ChunkCoord(x, z));
                    }  else if (!chunks[x, z].isActive) {
                        chunks[x, z].isActive = true;
                    }
                    activeChunks.Add(new ChunkCoord(x, z));
                }

                for (int i = 0; i < previouslyActiveChunks.Count; i++) {

                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                        previouslyActiveChunks.RemoveAt(i);
                       
                }

            }
        }

        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false;

    }

    public bool CheckForVoxel (Vector3 pos) {

        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return false;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return blocktypes[chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos).id].isSolid;

        return blocktypes[GetVoxel(pos)].isSolid;

    }

    public VoxelState GetVoxelState (Vector3 pos) {

        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > VoxelData.ChunkHeight)
            return null;

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isEditable)
            return chunks[thisChunk.x, thisChunk.z].GetVoxelFromGlobalVector3(pos);

        return new VoxelState (GetVoxel(pos));

    }

    public bool inUI {

        get { return _inUI; }

        set {

            _inUI = value;
            if (_inUI) {
                Cursor.lockState = CursorLockMode.None;
                creativeInventoryWindow.SetActive(true);
                cursorSlot.SetActive(true);
            } else {
                Cursor.lockState = CursorLockMode.Locked;
                creativeInventoryWindow.SetActive(false);
                cursorSlot.SetActive(false);
            }
        }

    }

    public int GetVoxel (Vector3 pos) 
    {

        int yPos = Mathf.FloorToInt(pos.y);

        /* IMMUTABLE PASS */

        // If outside world, return air.
        if (!IsVoxelInWorld(pos))
            return 0;

        // If bottom block of chunk, return bedrock.
        if (yPos == 0)
            return 10;

        /* BASIC TERRAIN PASS */

        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale)) + biome.solidGroundHeight;
        int voxelValue = 0;

        if (yPos == terrainHeight)
            voxelValue = 3;
        else if (yPos <= terrainHeight - 1 && yPos >= terrainHeight - 2)
            voxelValue = 2;
        else if (yPos < terrainHeight -2 && yPos > 0)
            voxelValue = 1;
        else
            return 0;

        /* SECOND PASS (Ores and caves) */

        foreach (Lode lode in biome.lodes)
        {
            if (yPos > lode.minHeight && yPos < lode.maxHeight)
                if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold))
                {
                    if(voxelValue == 1 && lode.blockID == 11)
                        voxelValue = 11;
                    else if(voxelValue == 1 && lode.blockID == 4)
                        voxelValue = 4;
                    else if(voxelValue == 1 && lode.blockID == 2)
                        voxelValue = 2;
                    else if(voxelValue == 1)
                        voxelValue = lode.blockID;
                    
                    if(lode.blockID != 4 && lode.blockID != 11 && voxelValue != 1)
                        voxelValue = lode.blockID;
                }

        }

        /* Tree Pass*/
        if(yPos == terrainHeight)
            if(Noise.Get2DPerlin(new Vector2(pos.x,pos.z), 120.0f, biome.treeZoneScale) > biome.treeZoneThreshold)
                if(voxelValue == 3)
                    if(Noise.Get2DPerlin(new Vector2(pos.x,pos.z), 240.0f, biome.treePlacementScale) > biome.treePlacementThreshold)
                        modifications.Enqueue(Structure.MakeTree(pos,biome.minTreeHeight,biome.maxTreeHeight));

        return voxelValue;


    }

    bool IsChunkInWorld (ChunkCoord coord) {

        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return
                false;

    }

    bool IsVoxelInWorld (Vector3 pos) {

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;

    }

}

[System.Serializable]
public class BlockType 
{

    public string blockName;
    public bool isSolid;
    public bool RenderNeigbourFaces;
    public float transparency;
    public Sprite icon;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID (int faceIndex) {

        switch (faceIndex) {

            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;


        }

    }

}

public class VoxelMod 
{

    public Vector3 position;
    public int id;

    public VoxelMod () {

        position = new Vector3();
        id = 0;

    }

    public VoxelMod (Vector3 _position, int _id) {

        position = _position;
        id = _id;

    }

}

[System.Serializable]
public class Settings
{
    [Header("Game Data")]
    public string Version;

    [Header("Performance")]
    public int viewDistance;
    public bool smoothLighting; // unimped
    public int brightness; // unimped

    [Header("Player Settings")]
    public float mouseSensetivity;
    public bool smoothMovment;  // unimped
    public int FOV;             // unimped
    public bool cameraBobbing;  // unimped

    [Header("World Settings")]
    public int seed;
    public int terrainHeight; // unimped

}