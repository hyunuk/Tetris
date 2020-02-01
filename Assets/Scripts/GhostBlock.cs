using UnityEngine;

public class GhostBlock : MonoBehaviour {
    internal void Destroy() {
        foreach (Transform children in this.transform) {
            Destroy(children.gameObject);
        }
    }

    internal bool IsDestroyed() {
        return transform.childCount == 0;
    }
}
