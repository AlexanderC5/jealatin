using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPosition : MonoBehaviour
{
    Camera cam;
    [SerializeField] float minCameraSize = 3;
    [SerializeField] int[] xPadding = new int[2];
    [SerializeField] int[] yPadding = new int[2];

    // Start is called before the first frame update
    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    public void UpdateCamera()
    {
        int xOrigin = GameManager.Instance.StageSize.x - xPadding[0];
        int xWidth = GameManager.Instance.StageSize.size.x + xPadding[0] + xPadding[1];
        int yOrigin = GameManager.Instance.StageSize.y - yPadding[0];
        int yHeight = GameManager.Instance.StageSize.size.y + yPadding[0] + yPadding[1];

        float xMiddle = xOrigin + xWidth / 2f -0.5f;  // Subtract 0.5 since the tiles are offset by -0.5
        float yMiddle = yOrigin + yHeight / 2f -0.5f;

        this.transform.position = new Vector3(xMiddle, yMiddle, -10);
        
        bool fitX = (yHeight * Screen.width < xWidth * Screen.height) ? true : false; // Should the screen stretch to fit along x or y?
        
        if (fitX) cam.orthographicSize = xWidth / 2f * Screen.height / Screen.width; // Fit tilemap to x-width of screen
        else cam.orthographicSize = yHeight / 2f;                                    // Fit tilemap to y-height of screen
        
        if (cam.orthographicSize < minCameraSize) cam.orthographicSize = minCameraSize; // Screen too zoomed-in, set to min
    }
}
