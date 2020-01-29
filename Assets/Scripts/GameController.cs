using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameController : MonoBehaviour {
    private float previousTime;
    private float previousToLeft;
    private float previousToRight;
    public float fallTime = 0.8f;
    public Vector3 startPos = new Vector3();
    public static int height = 20;
    public static int width = 10;
    private static int score = 0;
    private static int linesDeleted = 0;

    private static TetrisBlock[,] grid = new TetrisBlock[height, width];    
    public TetrisBlock[] Blocks;
    public GhostBlock[] Ghosts;
    private int nextBlock;
    public TetrisBlock currBlock;
    private GhostBlock ghostBlock;
    public TetrisBlock deadBlock;
    private bool hardDropped = false;

    void Start() {
        nextBlock = Random.Range(0, Blocks.Length);
        NewBlock();
    }

    void Update() {
        if (Input.GetKey(KeyCode.LeftArrow) && Time.time - previousToLeft > 0.08f) {
            HorizontalMove(Vector3.left);
            previousToLeft = Time.time;
        } else if (Input.GetKey(KeyCode.RightArrow) && Time.time - previousToRight > 0.08f) {
            HorizontalMove(Vector3.right);
            previousToRight = Time.time;
        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            Rotate();
        } else if (Input.GetKeyDown(KeyCode.Space)) {
            while (ValidMove(currBlock.transform)) VerticalMove(Vector3.down);
        } else if (Input.GetKeyUp(KeyCode.Space)) {
            hardDropped = false;
        }

        if (Time.time - previousTime > (Input.GetKey(KeyCode.DownArrow) ? fallTime / 10 : fallTime)) {
            VerticalMove(Vector3.down);
            previousTime = Time.time;
        }

        ghostBlock.transform.position = GhostPosition(currBlock.transform.position);
    }

    void Rotate() {
        Transform currTransform = currBlock.transform;
        currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);
        ghostBlock.transform.RotateAround(ghostBlock.transform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);

        if (!ValidMove(currBlock.transform)) {
            currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);
            ghostBlock.transform.RotateAround(ghostBlock.transform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);

        }
    }

    void HorizontalMove(Vector3 nextMove) {
        currBlock.transform.position += nextMove;
        if (!ValidMove(currBlock.transform)) {
            currBlock.transform.position -= nextMove;
        }
    }

    void VerticalMove(Vector3 nextMove) {
        currBlock.transform.position += nextMove;
        if (!ValidMove(currBlock.transform)) {
            currBlock.transform.position -= nextMove;
            EndTurn();
        }
    }

    private void EndTurn() {
        AddToGrid();
        NewBlock();
        CheckForLines();
        hardDropped = true;
    }

    void AddToGrid() {
        foreach (Transform children in currBlock.transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            TetrisBlock curr = Instantiate(deadBlock, new Vector3(roundedX, roundedY, 0), Quaternion.identity);
            grid[roundedY, roundedX] = curr;
        }
    }

    void CheckForLines() {
        for (int y = height - 1; y >= 0; y--) {
            if (HasLine(y)) {
                DeleteLine(y);
                RowDown(y);
            }
        }
    }

    bool HasLine(int y) {
        for (int x = 0; x < width; x++) {
            if (grid[y, x] == null) return false;
        }
        return true;
    }

    void DeleteLine(int y) {
        for (int x = 0; x < width; x++) {
            if (grid[y, x] != null) {
                grid[y, x].Destroy();
                grid[y, x] = null;
            }
            score++;
        }
        linesDeleted++;
        print(String.Format("Score: {0}", score));
        print(String.Format("Line(s) deleted: {0}", linesDeleted));
    }

    void RowDown(int deletedLine) {
        for (int y = deletedLine; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (grid[y, x] != null) {
                    grid[y - 1, x] = grid[y, x];
                    grid[y, x] = null;
                    grid[y - 1, x].transform.position -= Vector3.up;
				}
            }
        }
    }

    bool ValidMove(Transform transform) {
        foreach (Transform children in transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);

            if (roundedX < 0 || roundedX >= width || roundedY < 0 || roundedY >= height) {
                return false;
            }
            if (grid[roundedY, roundedX] != null) {
                return false;
            }
        }
        return true;
    }

    public Vector3 GhostPosition(Vector3 vec) {
        int x = Mathf.RoundToInt(vec.x), y = Mathf.RoundToInt(vec.y), z = Mathf.RoundToInt(vec.z);
        ghostBlock.transform.position = new Vector3(x, -1, z);
        while (!ValidMove(ghostBlock.transform)) ghostBlock.transform.position += Vector3.up;

        return ghostBlock.transform.position;
    }

    private void NewBlock() {
        if (currBlock != null) {
            TetrisBlock t = currBlock;
            currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
            t.Destroy();
        } else {
            currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
        }
        NewGhost();
        nextBlock = Random.Range(0, Blocks.Length);
    }

    private void NewGhost() {
        if (ghostBlock != null) {
            ghostBlock.Destroy();
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        } else {
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        }
    }
}
