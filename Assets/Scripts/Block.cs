using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block: MonoBehaviour
{
    public void Destroy() {
        foreach (Transform children in this.transform) {
            Destroy(children.gameObject);
        }
    }
}
