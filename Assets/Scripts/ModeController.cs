using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ModeController : MonoBehaviour
{
    static ModeController instance = null;
    public enum Mode { stage, infinite };
    public Button stageMode, infiniteMode;
    private Mode mode = Mode.stage;
    
    private void Awake()
    {
        if (instance != null) {
            Debug.Log("ModeController instance has been destroyed");
            Destroy(gameObject);
        } else {
            Debug.Log("ModeController instance has been assigned");
            instance = this;
            GameObject.DontDestroyOnLoad(gameObject);
        }
    }

    public void StartGame() {
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }

    public Mode GetMode() {
        return mode;
    }

    public void SetMode(int mode) {
        this.mode = mode == 0 ? Mode.stage : Mode.infinite;
        switch (this.mode) {
            case Mode.stage:
                stageMode.interactable = false;
                infiniteMode.interactable = true;
                break;
            case Mode.infinite:
                stageMode.interactable = true;
                infiniteMode.interactable = false;
                break;
        }
    }
}
