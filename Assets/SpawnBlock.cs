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
        NewBlock();
    }

    // Update is called once per frame
    public void NewBlock() {
        Instantiate(Blocks[nextBlock], transform.position, Quaternion.identity);
        NewGhost();
        nextBlock = Random.Range(0, Blocks.Length);
        print(String.Format("Next Block: {0}", nextBlock));
    }

    void NewGhost() {
        Destroy(ghostBlock);
        ghostBlock = Instantiate(Ghosts[nextBlock], TetrisBlock.GhostPosition(transform.position), Quaternion.identity);
    }
}
