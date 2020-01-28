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
    public enum State { empty, dead };
    public State[,] board = new State[height, width];
    
    public TetrisBlock[] Blocks;
    public GhostBlock[] Ghosts;
    private int nextBlock;
    public TetrisBlock currBlock;
    private GhostBlock ghostBlock;
    public TetrisBlock deadBlock;

    void Start() {
        board.Initialize(); // filled with empty
        nextBlock = Random.Range(0, Blocks.Length);
        NewBlock();
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.LeftArrow)) {
            HorizontalMove(Vector3.left);
        } else if (Input.GetKey(KeyCode.LeftArrow) && Time.time - previousToLeft > 0.08f) {
            HorizontalMove(Vector3.left);
            previousToLeft = Time.time;

        } else if (Input.GetKeyDown(KeyCode.RightArrow) && !Input.GetKey(KeyCode.RightArrow)) {
            HorizontalMove(Vector3.right);
        } else if (Input.GetKey(KeyCode.RightArrow) && Time.time - previousToRight > 0.08f) {
            HorizontalMove(Vector3.right);
            previousToRight = Time.time;

        } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
            Rotate();
        } else if (Input.GetKeyDown(KeyCode.Space)) {
            while (ValidMove()) VerticalMove(Vector3.down);
        }

        if (Time.time - previousTime > (Input.GetKey(KeyCode.DownArrow) ? fallTime / 10 : fallTime)) {
            VerticalMove(Vector3.down);
            previousTime = Time.time;
        }
        //ghostBlock.transform.position = GhostPosition(transform.position);
    }

    void Rotate() {
        Transform currTransform = currBlock.transform;
        currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);
        if (!ValidMove()) {
            currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);
        }
    }

    void HorizontalMove(Vector3 nextMove) {
        currBlock.transform.position += nextMove;
        if (!ValidMove()) {
            currBlock.transform.position -= nextMove;
        }
    }

    void VerticalMove(Vector3 nextMove) {
        currBlock.transform.position += nextMove;
        if (!ValidMove()) {
            currBlock.transform.position -= nextMove;
            EndTurn();
        }
    }

    private void EndTurn() {
        AddToGrid();
        NewBlock();
        CheckForLines();
        DrawBoard();
    }

    void AddToGrid() {
        foreach (Transform children in currBlock.transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            board[roundedY, roundedX] = State.dead;
            //grid[roundedY, roundedX] = 
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
            if (board[y, x] == State.empty) return false;
        }
        return true;
    }

    void DeleteLine(int y) {
        for (int x = 0; x < width; x++) {
            if (grid[y, x] != null) {
                grid[y, x].Destroy();
                grid[y, x] = null;
                board[y, x] = State.empty;
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
                if (board[y, x] == State.dead) {
                    board[y - 1, x] = board[y, x];
                    board[y, x] = State.empty;
                }
            }
        }
    }

    void DrawBoard() {
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (board[y, x] == State.dead) {
                    TetrisBlock curr = Instantiate(deadBlock, new Vector3(x, y, 0), Quaternion.identity);
                    grid[y, x] = curr;
                } else if (board[y, x] == State.empty) {
                    if (grid[y, x] != null) {
                        grid[y, x].Destroy();
                        grid[y, x] = null;
                    }
                }
            }
        }
    }

    bool ValidMove() {
        foreach (Transform children in currBlock.transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);

            if (roundedX < 0 || roundedX >= width || roundedY < 0 || roundedY >= height) {
                return false;
            }
            if (grid[roundedY, roundedX] != null) {
                return false;
            }
            if (board[roundedY, roundedX] != State.empty) {
                return false;
            }
        }
        return true;
    }

    public Vector3 GhostPosition(Vector3 vec) {
        int x = Mathf.RoundToInt(vec.x), y = Mathf.RoundToInt(vec.y), z = Mathf.RoundToInt(vec.z);
        for (; y > 0; y--) {
            if (grid[y - 1, x] != null) break;
        }

        return new Vector3(x, y, z);
    }

    private void NewBlock() {
        if (currBlock != null) {
            TetrisBlock t = currBlock;
            currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
            t.Destroy();
        } else {
            currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
        }
        //NewGhost();
        nextBlock = Random.Range(0, Blocks.Length);
    }

    private void NewGhost() {
        Destroy(ghostBlock);
        ghostBlock = Instantiate(Ghosts[nextBlock], GhostPosition(transform.position), Quaternion.identity);
    }

}
