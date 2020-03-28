using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Mode = Helper.Mode;

public class GameController : MonoBehaviour {
    private readonly string STAGES_PATH = "Assets/Stages/";
    private readonly int[] scores = { 0, 40, 100, 300, 1200 };
    private readonly int NUM_OF_STAGES = 20;

    public float fallTime = 0.8f;
    private float N = 20;
    public Vector3 startPos = new Vector3();
    private readonly Vector3[] Pivots = new[] { new Vector3(-0.33f, 0f, 0f), new Vector3(-0.27f, -0.15f, 0f), new Vector3(-0.27f, 0.1f, 0f), new Vector3(-0.12f, -0.1f, 0f), new Vector3(-0.22f, -0.1f, 0f), new Vector3(-0.02f, -0.1f, 0f), new Vector3(-0.2f, 0.1f, 0f) };

    private float previousTime, previousToLeft, previousToRight;
    private int score = 0;
    private int linesDeleted = 0;
    private int numGems = 0;
    private float playTime;
    private int nextLevel;
    private List<int> deletingRow = new List<int>();

    private int currStage = 0;

    private HashSet<int> deck = new HashSet<int>();

    private Block[,] grid = new Block[Helper.HEIGHT, Helper.WIDTH];

    public TetrisBlock[] Blocks;
    
    public GhostBlock[] Ghosts;
    private int nextBlock;
    public TetrisBlock nextBlockObject;
    public TetrisBlock currBlock;
    public TetrisBlock deadBlock;
    public GameObject nextBlockBackground, infoText, restartButton, resumeButton, pauseButton, speakerButton, muteButton;
    public GemBlock gemBlock;
    private GhostBlock ghostBlock;
    private bool hardDropped, gameOver, gameClear, isDestroying, isPaused, isShowingAnimation, isRowDown, isAnimating, isEndTurn;
    private ModeController controller;
    public Text timeValue, levelValue, linesValue, stageValue, scoreValue, gameModeValue;

    void Start() {
        muteButton.SetActive(true);
        speakerButton.SetActive(false);
        InitGame();
    }

    void InitGame() {
        FindObjectOfType<AudioManager>().Play("GameStart");
        controller = GameObject.FindWithTag("ModeController").GetComponent<ModeController>();
        gameModeValue.text = (controller.GetMode() == Mode.stage ? "S T A G E" : "I N F I N I T E") + "  M O D E";
        infoText.SetActive(false);
        restartButton.SetActive(false);
        resumeButton.SetActive(false);
        gameOver = false;
        gameClear = false;
        isShowingAnimation = false;
        isEndTurn = false;
        isAnimating = false;
        playTime = 0;
        linesDeleted = 0;
        score = 0;
        if (currBlock != null) currBlock.Destroy();
        NextBlock();
        if (controller.GetMode() == Mode.stage) SetStage();
        NewBlock();
    }

    public void Pause() {
        isPaused = true;
        pauseButton.SetActive(false);
        resumeButton.SetActive(true);
        FindObjectOfType<AudioManager>().Mute("GameStart", true);
    }

    public void Resume() {
        isPaused = false;
        resumeButton.SetActive(false);
        pauseButton.SetActive(true);
        FindObjectOfType<AudioManager>().Mute("GameStart", false);
    }

    public void Mute(bool isMute) {
        FindObjectOfType<AudioManager>().Mute("GameStart", isMute);
        if (isMute) {
            muteButton.SetActive(false);
            speakerButton.SetActive(true);
        } else {
            muteButton.SetActive(true);
            speakerButton.SetActive(false);
        }
    }

    void NextBlock() {
        print("nextblock start");
        if (deck.Count == Blocks.Length) deck.Clear();
        do nextBlock = Random.Range(0, Blocks.Length);
        while (deck.Contains(nextBlock));
        deck.Add(nextBlock);

        if (nextBlockObject != null) nextBlockObject.Destroy();
        nextBlockObject = Instantiate(Blocks[nextBlock]);
        nextBlockObject.transform.parent = nextBlockBackground.transform;
        nextBlockObject.transform.localPosition = Pivots[nextBlock];
        print("nextblock end");
    }

    void SetStage() {
        for (int y = 0; y < Helper.HEIGHT; y++) {
            for (int x = 0; x < Helper.WIDTH; x++) {
                if (grid[y, x] != null) grid[y, x].Destroy();
                int blockType = Helper.Stages[currStage, Helper.HEIGHT - y - 1, x];
                switch (blockType) {
                    case 0:
                        grid[y, x] = null;
                        break;
                    case 1:
                        grid[y, x] = Instantiate(deadBlock, new Vector3(x, y, 0), Quaternion.identity);
                        break;
                    case 2:
                        numGems++;
                        grid[y, x] = Instantiate(gemBlock, new Vector3(x, y, 0), Quaternion.identity);
                        break;
                }
            }
        }
    }

    void Update() {
        if (isPaused && Input.GetKeyDown(KeyCode.P)) Resume();
        else if (!isEndTurn && !gameOver && !gameClear && !isPaused && !isShowingAnimation) {
            if (Input.GetKey(KeyCode.LeftArrow) && Time.time - previousToLeft > 0.1f) {
                HorizontalMove(Vector3.left);
                previousToLeft = Time.time;
            } else if (Input.GetKey(KeyCode.RightArrow) && Time.time - previousToRight > 0.1f) {
                HorizontalMove(Vector3.right);
                previousToRight = Time.time;
            } else if (Input.GetKeyDown(KeyCode.UpArrow)) {
                Rotate();
            } else if (Input.GetKeyDown(KeyCode.Space)) {
                while (ValidMove(currBlock.transform) && !hardDropped) VerticalMove(Vector3.down);
            } else if (Input.GetKeyUp(KeyCode.Space)) {
                hardDropped = false;
            } else if (Input.GetKeyDown(KeyCode.P)) {
                Pause();
            }

            if (Time.time - previousTime > (Input.GetKey(KeyCode.DownArrow) ? fallTime / 10 : fallTime)) {
                VerticalMove(Vector3.down);
                previousTime = Time.time;
            }
            if (isAnimating && !isEndTurn) {
                EndTurn();
                isEndTurn = false;
            }

            nextLevel = Mathf.RoundToInt((linesDeleted / N) + 1);
            if (Int16.Parse(levelValue.text) < nextLevel) fallTime /= 1f + (Mathf.RoundToInt(linesDeleted / N) * 0.1f);

            playTime += Time.deltaTime;
            int minutes = Mathf.RoundToInt((playTime % (60 * 60 * 60)) / (60 * 60)), seconds = Mathf.RoundToInt((playTime % (60 * 60)) / 60), microseconds = Mathf.RoundToInt(playTime % 60);
            timeValue.text = String.Format("{0}:{1}:{2}", (minutes < 10 ? "0" : "") + minutes.ToString(), (seconds < 10 ? "0" : "") + seconds.ToString(), (microseconds < 10 ? "0" : "") + microseconds.ToString());

            GhostBlockImgUpdate();
            InfoUpdate();
        }
    }

    private void InfoUpdate() {
        levelValue.text = nextLevel.ToString();
        linesValue.text = linesDeleted.ToString();
        stageValue.text = (currStage + 1).ToString();
        scoreValue.text = score.ToString();
    }

    private void GhostBlockImgUpdate() {
        if (!ghostBlock.IsDestroyed()) {
            ghostBlock.transform.position = GhostPosition(currBlock.transform.position);
        }
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
            CreateDeadBlock();
            DestroyCurrBlock();
            CheckForLines();
        }
    }

    private void CreateDeadBlock() {
        foreach (Transform children in currBlock.transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            Color currColor = children.GetComponent<SpriteRenderer>().color;
            if (grid[roundedY, roundedX] == null) {
                TetrisBlock curr = Instantiate(deadBlock, new Vector3(roundedX, roundedY, 0), Quaternion.identity);
                curr.sprite.GetComponent<SpriteRenderer>().color = currColor;
                grid[roundedY, roundedX] = curr;
            }
        }
    }

    private void DestroyCurrBlock() {
        currBlock.Destroy();
        ghostBlock.Destroy();
    }

    private void CheckForLines() {
        isShowingAnimation = true;
        deletingRow.Clear();

        for (int y = Helper.HEIGHT - 1; y >= 0; y--) {
            if (HasLine(y)) {
                deletingRow.Add(y);
            }
        }
        score += scores[deletingRow.Count] * Mathf.RoundToInt((linesDeleted / N) + 1);
        linesDeleted += deletingRow.Count;
        if (deletingRow != null) {
            isAnimating = true;
        }
    }

    private bool HasLine(int y) {
        for (int x = 0; x < Helper.WIDTH; x++) {
            if (grid[y, x] == null) return false;
        }
        return true;
    }

    private void EndTurn() {
        isEndTurn = true;
        print("EndTurn");
        FindObjectOfType<AudioManager>().Play("Blip");
        hardDropped = true;
        foreach (var y in deletingRow) {
            StartCoroutine(DeleteLine(y));
            StartCoroutine(WaitForRowDown(y));
        }
        StartCoroutine(WaitForNewBlock());
        isAnimating = false;
    }

    private IEnumerator DeleteLine(int y) {
        print("deleteline");
        isDestroying = true;
        int[] destroyedBlocks = new int[1];
        destroyedBlocks[0] = 0;
        for (int x = 0; x < Helper.WIDTH; x++) {
            if (grid[y, x] != null) {
                StartCoroutine(DeleteLineEffect(grid[y, x], destroyedBlocks));
            }
        }

        while (destroyedBlocks[0] < 10) {
            yield return new WaitForSeconds(0.1f);
        }
        for (int x = 0; x < Helper.WIDTH; x++) {
            if (grid[y, x] == null) continue;
            if (grid[y, x].transform.GetComponent<GemBlock>() != null) numGems--;
            grid[y, x].Destroy();
            grid[y, x] = null;
        }
        isDestroying = false;
        destroyedBlocks[0] = 0;
    }

    private IEnumerator DeleteLineEffect(Block dead, int[] destroyedBlocks) {
        Color tmp = dead.sprite.GetComponent<SpriteRenderer>().color;
        float _progress = 1f;

        while (_progress > 0.0f) {
            dead.sprite.GetComponent<SpriteRenderer>().color = new Color(tmp.r, tmp.g, tmp.b, tmp.a * _progress);
            _progress -= 0.1f;
            yield return new WaitForSeconds(0.03f);
        }

        if (_progress < 0.0f && dead != null) {
            destroyedBlocks[0]++;
        }
    }

    private IEnumerator WaitForRowDown(int y) {
        while (isDestroying) {
            yield return new WaitForSeconds(0.01f);
        }
        RowDown(y);
    }

    private IEnumerator WaitForNewBlock() {
        while (isDestroying || isRowDown) {
            yield return new WaitForSeconds(0.01f);
        }
        NewBlock();
    }

    void RowDown(int deletedLine) {
        print("rowdown");

        isRowDown = true;
        for (int y = deletedLine; y < Helper.HEIGHT; y++) {
            for (int x = 0; x < Helper.WIDTH; x++) {
                if (y == deletedLine) {
                    grid[y, x] = null;
                }
                if (grid[y, x] != null) {
                    grid[y - 1, x] = grid[y, x];
                    grid[y, x] = null;
                    grid[y - 1, x].transform.position -= Vector3.up;
				}
            }
        }
        isRowDown = false;
    }

    bool ValidMove(Transform transform) {
        foreach (Transform children in transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            if (roundedX < 0 || roundedX >= Helper.WIDTH || roundedY < 0 || roundedY >= Helper.HEIGHT) {
                return false;
            }
            if (grid[roundedY, roundedX] != null) {
                return false;
            }
        }
        return true;
    }

    public Vector3 GhostPosition(Vector3 vec) {
        int x = Mathf.RoundToInt(vec.x), y = Math.Max(Mathf.RoundToInt(vec.y), 0), z = Mathf.RoundToInt(vec.z);
        ghostBlock.transform.position = new Vector3(x, y, z);
        while (ValidMove(ghostBlock.transform)) ghostBlock.transform.position += Vector3.down;
        
        return ghostBlock.transform.position + Vector3.up;
    }

    private void NewBlock() {
        print("newblock start");
        currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
        NewGhost();
        NextBlock();
        isShowingAnimation = false;
        if (grid[18, 4] != null) {
            print("going to gameover");
            gameOver = true;
            GameOver();
        }
        if (controller.GetMode() == Mode.stage && numGems == 0) {
            gameClear = true;
            GameClear();
        }
        print("newblock end");
    }

    private void NewGhost() {
        print("newghost start");
        if (ghostBlock != null) {
            ghostBlock.Destroy();
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        } else {
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        }
        print("newghostend");
}

    private void GameOver() {
        print("GAME OVER!!!");
        if (ghostBlock != null) ghostBlock.Destroy();
        infoText.SetActive(true);
        infoText.GetComponent<TextMeshProUGUI>().text = "GAME OVER";
        FindObjectOfType<AudioManager>().Stop("GameStart");
        restartButton.SetActive(true);
    }

    private void GameClear() {
        print("GameClear");
        if (ghostBlock != null) ghostBlock.Destroy();
        infoText.SetActive(true);
        infoText.GetComponent<TextMeshProUGUI>().text = "GAME CLEAR";
        FindObjectOfType<AudioManager>().Stop("GameStart");
        FindObjectOfType<AudioManager>().Play("GameClear");
        StartCoroutine(CountDown());
    }

    private IEnumerator CountDown() {
        yield return new WaitForSeconds(0.5f);
        infoText.GetComponent<TextMeshProUGUI>().text = "3";
        yield return new WaitForSeconds(0.5f);
        infoText.GetComponent<TextMeshProUGUI>().text = "2";
        yield return new WaitForSeconds(0.5f);
        infoText.GetComponent<TextMeshProUGUI>().text = "1";
        yield return new WaitForSeconds(0.5f);
        currStage++;
        InitGame();
    }

    public void GoBack() {
        SceneManager.LoadScene(0);
    }
}

[Serializable]
public class GameOverException : Exception
{
    public GameOverException() { }

    public GameOverException(string message)
        : base(message) { }
}
