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
    
    private int currStage = 0;
    private List<string> Stages = new List<string>();
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
    private bool hardDropped, gameOver, gameClear, isDestroying, isPaused, isShowingAnimation;
    private ModeController controller;
    public Text timeValue, levelValue, linesValue, stageValue, scoreValue, gameModeValue;

    void Start() {
        print("Start");
        muteButton.SetActive(true);
        speakerButton.SetActive(false);
        InitGame();
    }

    void InitGame() {
        print("InitGame");
        FindObjectOfType<AudioManager>().Play("GameStart");
        controller = GameObject.FindWithTag("ModeController").GetComponent<ModeController>();
        gameModeValue.text = (controller.GetMode() == Mode.stage ? "S T A G E" : "I N F I N I T E") + "  M O D E";
        for (int i = 1; i <= NUM_OF_STAGES; i++) {
            string path = Path.GetFullPath(STAGES_PATH + i + ".txt");
            Stages.Add(path);
        }
        infoText.SetActive(false);
        restartButton.SetActive(false);
        resumeButton.SetActive(false);
        gameOver = false;
        gameClear = false;
        isShowingAnimation = false;
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
        print("NextBlock");
        if (deck.Count == Blocks.Length) deck.Clear();
        do nextBlock = Random.Range(0, Blocks.Length);
        while (deck.Contains(nextBlock));
        deck.Add(nextBlock);

        if (nextBlockObject != null) nextBlockObject.Destroy();
        nextBlockObject = Instantiate(Blocks[nextBlock]);
        nextBlockObject.transform.parent = nextBlockBackground.transform;
        nextBlockObject.transform.localPosition = Pivots[nextBlock];
    }

    void SetStage() {
        string textFile = Stages[currStage];

        if (File.Exists(textFile)) {
            string[] lines = File.ReadAllLines(textFile);
            for (int y = 0; y < Helper.HEIGHT; y++) {
                string[] pixels  = lines[y].Split(',');
                for (int x = 0; x < Helper.WIDTH; x++) {
                    if (grid[Helper.HEIGHT - y - 1, x] != null) grid[Helper.HEIGHT - y - 1, x].Destroy();
                    int blockType = Int16.Parse(pixels[x]);
                    switch (blockType) {
                        case 0:
                            grid[Helper.HEIGHT - y - 1, x] = null;
                            break;
                        case 1:
                            grid[Helper.HEIGHT - y - 1, x] = Instantiate(deadBlock, new Vector3(x, Helper.HEIGHT - y - 1, 0), Quaternion.identity);
                            break;
                        case 2:
                            numGems++;
                            grid[Helper.HEIGHT - y - 1, x] = Instantiate(gemBlock, new Vector3(x, Helper.HEIGHT - y - 1, 0), Quaternion.identity);
                            break;
                    }
                }
            }
        } else {
            print(String.Format("File {0} does not exist!", textFile));
        }
        print(String.Format("Initial # of gems: {0}", numGems));
    }

    void Update() {
        if (controller.GetMode() == Mode.stage && numGems == 0) gameClear = true;
        if (isPaused && Input.GetKeyDown(KeyCode.P)) Resume();
        else if (!gameOver && !gameClear && !isPaused && !isShowingAnimation) {
            if (Input.GetKey(KeyCode.LeftArrow) && Time.time - previousToLeft > 0.08f) {
                HorizontalMove(Vector3.left);
                previousToLeft = Time.time;
            } else if (Input.GetKey(KeyCode.RightArrow) && Time.time - previousToRight > 0.08f) {
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
            if (!ghostBlock.IsDestroyed()) ghostBlock.transform.position = GhostPosition(currBlock.transform.position);

            int nextLevel = Mathf.RoundToInt((linesDeleted / N) + 1);

            if (Int16.Parse(levelValue.text) < nextLevel) fallTime /= 1f + (Mathf.RoundToInt(linesDeleted / N) * 0.1f);

            playTime += Time.deltaTime;

            timeValue.text = String.Format("{0}:{1}:{2}", Mathf.RoundToInt((playTime % (60 * 60 * 60)) / (60 * 60)).ToString(), Mathf.RoundToInt((playTime % (60 * 60)) / 60).ToString(), Mathf.RoundToInt(playTime % 60).ToString());
            levelValue.text = nextLevel.ToString();
            linesValue.text = linesDeleted.ToString();
            stageValue.text = (currStage + 1).ToString();
            scoreValue.text = score.ToString();
        }
    }

    void Rotate() {
        print("Rotate");
        Transform currTransform = currBlock.transform;
        currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);
        ghostBlock.transform.RotateAround(ghostBlock.transform.TransformPoint(currBlock.rotationPoint), Vector3.forward, 90);

        if (!ValidMove(currBlock.transform)) {
            currTransform.RotateAround(currTransform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);
            ghostBlock.transform.RotateAround(ghostBlock.transform.TransformPoint(currBlock.rotationPoint), Vector3.forward, -90);

        }
    }

    void HorizontalMove(Vector3 nextMove) {
        print("HorizontalMove");
        currBlock.transform.position += nextMove;
        if (!ValidMove(currBlock.transform)) {
            currBlock.transform.position -= nextMove;
        }
    }

    void VerticalMove(Vector3 nextMove) {
        print("VerticalMove");
        print(currBlock.transform.position);
        currBlock.transform.position += nextMove;
        if (!ValidMove(currBlock.transform)) {
            currBlock.transform.position -= nextMove;
            EndTurn();
        }
    }

    private void EndTurn() {
        print("EndTurn");
        try {
            AddToGrid();
            CheckForLines();
            hardDropped = true;
            FindObjectOfType<AudioManager>().Play("Blip");
        } catch(GameOverException e) {
            GameOver();
        }
    }

    void AddToGrid() {
        print("AddToGrid");
        foreach (Transform children in currBlock.transform) {
            int roundedY = Mathf.RoundToInt(children.transform.position.y);
            int roundedX = Mathf.RoundToInt(children.transform.position.x);
            Color currColor = children.GetComponent<SpriteRenderer>().color;
            TetrisBlock curr = Instantiate(deadBlock, new Vector3(roundedX, roundedY, 0), Quaternion.identity);
            curr.sprite.GetComponent<SpriteRenderer>().color = currColor;
            grid[roundedY, roundedX] = curr;
            currBlock.Destroy();
            ghostBlock.Destroy();

            if (roundedX == 4 && roundedY == 18) gameOver = true;
        }
        if (gameOver) throw new GameOverException();
    }

    void CheckForLines() {
        isShowingAnimation = true;

        int numLines = 0;
        for (int y = Helper.HEIGHT - 1; y >= 0; y--) {
            if (HasLine(y)) {
                numLines++;
                StartCoroutine(DeleteLine(y));
                StartCoroutine(WaitForRowDown(y));
            }
        }
        score += scores[numLines] * Mathf.RoundToInt((linesDeleted / N) + 1);
        linesDeleted += numLines;
        StartCoroutine(WaitForNewBlock());
        print(isShowingAnimation);
    }

    private IEnumerator WaitForNewBlock() {
        print("WaitForNewBlock");
        while (isDestroying) {
            yield return new WaitForSeconds(0.03f);
        }
        NewBlock();
    }

    private IEnumerator WaitForRowDown(int y) {
        print("WaitForRowDown");
        while (isDestroying) {
            yield return new WaitForSeconds(0.03f);
        }
        RowDown(y);
    }

    bool HasLine(int y) {
        for (int x = 0; x < Helper.WIDTH; x++) {
            if (grid[y, x] == null) return false;
        }
        return true;
    }                           

    private IEnumerator DeleteLine(int y) {
        print("DeleteLine");
        isDestroying = true;
        int[] destroyedBlocks = new int[1];
        destroyedBlocks[0] = 0;
        for (int x = 0; x < Helper.WIDTH; x++) {
            if (grid[y, x] != null) {
                StartCoroutine(DeleteLineEffect(grid[y, x], destroyedBlocks));
                yield return new WaitForSeconds(0.05f);
            }
        }

        while (destroyedBlocks[0] < 10) {
            yield return new WaitForSeconds(0.05f);
        }
        for (int x = 0; x < Helper.WIDTH; x++) {
            if (grid[y, x] != null) {
                if (grid[y, x].transform.GetComponent<GemBlock>() != null) numGems--;
                grid[y, x].Destroy();
                grid[y, x] = null;
            }
        }
        isDestroying = false;
        destroyedBlocks[0] = 0;
    }

    private IEnumerator DeleteLineEffect(Block dead, int[] destroyedBlocks) {
        print("DeleteLineEffect");
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

    void RowDown(int deletedLine) {
        for (int y = deletedLine; y < Helper.HEIGHT; y++) {
            for (int x = 0; x < Helper.WIDTH; x++) {
                if (grid[y, x] != null) {
                    grid[y - 1, x] = grid[y, x];
                    grid[y, x] = null;
                    grid[y - 1, x].transform.position -= Vector3.up;
				}
            }
        }
    }

    bool ValidMove(Transform transform) {
        print("ValidMove");
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
        print("GhostPosition");
        int x = Mathf.RoundToInt(vec.x), y = Math.Max(Mathf.RoundToInt(vec.y), 0), z = Mathf.RoundToInt(vec.z);
        ghostBlock.transform.position = new Vector3(x, y, z);
        while (ValidMove(ghostBlock.transform)) ghostBlock.transform.position += Vector3.down;
        
        return ghostBlock.transform.position + Vector3.up;
    }

    private void NewBlock() {
        print("NewBlock");
        if (gameClear) GameClear();
        currBlock = Instantiate(Blocks[nextBlock], startPos, Quaternion.identity);
        NewGhost();
        NextBlock();
        isShowingAnimation = false;
        print(isShowingAnimation);

    }

    private void NewGhost() {
        print("NewGhost");
        if (ghostBlock != null) {
            ghostBlock.Destroy();
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
        } else {
            ghostBlock = Instantiate(Ghosts[nextBlock], currBlock.transform.position, Quaternion.identity);
    }
}

    private void GameOver() {
        print("GameOver");
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
