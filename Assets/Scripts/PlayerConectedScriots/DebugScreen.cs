using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugScreen : MonoBehaviour {

    World world;
    [SerializeField] private TMPro.TMP_Text text;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;

    void Start() {
        
        world = GameObject.Find("World").GetComponent<World>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;

    }

    void Update() {

        string debugText = "Paper Wing Studios | VBMG";
		debugText += "\n";
		debugText += "Frame Rate [ " + frameRate + " FPS]";
		debugText += "\n\n";
		debugText += "Player Position [X]<" + (Mathf.FloorToInt(world.player.transform.position.x) - halfWorldSizeInVoxels) + "> [Y]<" + Mathf.FloorToInt(world.player.transform.position.y)  + "> [Z]<" + (Mathf.FloorToInt(world.player.transform.position.z) - halfWorldSizeInVoxels)  + ">";
		debugText += "\n";
		debugText += "Chunk Coords  [X]<" + (world.playerChunkCoord.x - halfWorldSizeInChunks) + "> [z]<" + (world.playerChunkCoord.z - halfWorldSizeInChunks) + ">";

        text.text = debugText;

        if (timer > 1f) {

            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0;

        } else
            timer += Time.deltaTime;

    }
}
