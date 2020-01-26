using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TetrisBlock : MonoBehaviour {
    public Vector3 rotationPoint;
    private float previousTime;
    public float fallTime = 0.8f;
    public static int height = 20;
    public static int width = 10;
    private static Transform[,] grid = new Transform[width, height];
    private static int score = 0;
    private static int linesDeleted = 0;

    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            HorizontalMove(Vector3.left);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            HorizontalMove(Vector3.right);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            Rotate();

        if (Time.time - previousTime > (Input.GetKey(KeyCode.DownArrow) ? fallTime / 10 : fallTime)) {
            VerticalMove(Vector3.down);
            previousTime = Time.time;
        }
    }

    void HorizontalMove(Vector3 nextMove) {
        transform.position += nextMove;
        SpawnBlock.ghostBlock.transform.position += nextMove;
        if (!ValidMove()) {
            transform.position -= nextMove;
            SpawnBlock.ghostBlock.transform.position -= nextMove;
        }
    }

    void VerticalMove(Vector3 nextMove) {
        transform.position += nextMove;
        if (!ValidMove()) {
            transform.position -= nextMove;
            AddToGrid();
            CheckForLines();
            this.enabled = false;
            FindObjectOfType<SpawnBlock>().NewBlock();
        }
    }

    // void Move(Vector3 nextMove) {
    //     transform.position += nextMove;
    //     // if (!nextMove.Equals(Vector3.down)) SpawnBlock.ghostBlock.transform.position += nextMove;
    //     if (!ValidMove()) {
    //         transform.position -= nextMove;
    //         // if (!nextMove.Equals(Vector3.down)) SpawnBlock.ghostBlock.transform.position -= nextMove;
    //         if (nextMove.Equals(Vector3.down)) {
    //             AddToGrid();
    //             CheckForLines();
    //             this.enabled = false;
    //             FindObjectOfType<SpawnBlock>().NewBlock();
    //         }
    //     }
    //     // SpawnBlock.ghostBlock.transform.position = GhostPosition(SpawnBlock.ghostBlock.transform.position);
    // }

    void Rotate() {
        transform.RotateAround(transform.TransformPoint(rotationPoint), Vector3.forward, 90);
        if (!ValidMove()) transform.RotateAround(transform.TransformPoint(rotationPoint), Vector3.forward, -90);
    }

    void CheckForLines() {
        for (int i = height - 1; i >= 0; i--) {
            if (HasLine(i)) {
                DeleteLine(i);
                RowDown(i);
			}
		}
	}

    bool HasLine(int i) {
        for (int j = 0; j < width; j++) {
            if (grid[j, i] == null) return false;
		}
        return true;
	}

    void DeleteLine(int i) {
        for (int j = 0; j < width; j++) {
            Destroy(grid[j, i].gameObject);
            grid[j, i] = null;
            score++;
		}
        linesDeleted++;
        print(String.Format("Score: {0}", score));
        print(String.Format("Line(s) deleted: {0}", linesDeleted));
	}

    void RowDown(int i) {
        for (int y = i; y < height; y++) {
            for (int j = 0; j < width; j++) {
                if (grid[j, y] != null) {
                    grid[j, y - 1] = grid[j, y];
                    grid[j, y] = null;
                    grid[j, y - 1].transform.position -= new Vector3(0, 1, 0);
				}
			}
		}
	}

    void AddToGrid() {
        foreach (Transform children in transform) {
            // print(children.name);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            int roundedY = Mathf.RoundToInt(children.transform.position.y);

            grid[roundedX, roundedY] = children;
        }
	}

    bool ValidMove() {
        foreach (Transform children in transform) {
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            int roundedY = Mathf.RoundToInt(children.transform.position.y);

            if (roundedX < 0 || roundedX >= width || roundedY < 0 || roundedY >= height) {
                return false;
			}

            if (grid[roundedX, roundedY] != null) {
                return false;
			}
		}
        return true;
	}

    public static Vector3 GhostPosition(Vector3 vec) {
        int x = Mathf.RoundToInt(vec.x), y = Mathf.RoundToInt(vec.y), z = Mathf.RoundToInt(vec.z);

        for (; y > 0; y--) {
            if (grid[x, y - 1] != null) break;
        }

        return new Vector3(x, y, z);
    }
}
