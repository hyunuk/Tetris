using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnTetromino : MonoBehaviour {
    public GameObject[] Tetrominoes;

    void Start() {
        NewTetromino(); 
    }

    // Update is called once per frame
    public void NewTetromino() {
        Instantiate(Tetrominoes[Random.Range(0, Tetrominoes.Length)], transform.position, Quaternion.identity);
    }
}
