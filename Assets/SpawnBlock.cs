using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnBlock : MonoBehaviour {
    public GameObject[] Blocks;
    public GameObject[] Ghosts;
    private int nextBlock;
    public static GameObject ghostBlock;

    void Start() {
        nextBlock = Random.Range(0, Blocks.Length);
        // ghostBlock = nextBlock;
        InitGhost();
        NewBlock();
    }

    void InitGhost() {
        ghostBlock = Instantiate(Ghosts[nextBlock], TetrisBlock.GhostPosition(transform.position), Quaternion.identity);
    }

    // Update is called once per frame
    public void NewBlock() {
        // GameObject currBlock = Blocks[nextBlock], ghostBlock = Blocks[nextBlock];

        // TetrisBlock.SetGhostBlock(ghostBlock);

        Instantiate(Blocks[nextBlock], transform.position, Quaternion.identity);

        nextBlock = Random.Range(0, Blocks.Length);
        print(String.Format("Next Block: {0}", nextBlock));
    }
}
