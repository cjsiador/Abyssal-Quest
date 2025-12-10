using UnityEngine;

public class CC_CONTROLLER : MonoBehaviour
{
    public GameObject canvas;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            canvas.SetActive(!canvas.activeSelf);
        }
    }
}
