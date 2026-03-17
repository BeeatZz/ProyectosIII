using UnityEngine;

public class PuzzleManager : MonoBehaviour
{
    public GameObject[] puzzleObjects;

    public void ActivateObject(int objPos)
    {
        puzzleObjects[objPos].SetActive(true);
    }
    public void DeactivateObject(int objPos) 
    {
        puzzleObjects[objPos].SetActive(false);

    }
}
