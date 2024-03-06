
using UnityEngine;
using UnityEngine.UI;

public class CursorManager : MonoBehaviour
{
    public RectTransform cursorTransform;
    private Camera mainCamera;

    public Sprite mouseHoverSprite, mouseClickSprite;

    public Image cursorImage, leftWingImage, rightWingImage;    

    private PlayerWingsBehaviour playerWingsBehaviour;
    
    // Dot grid
    //public GameObject dotGameObject;
    //public int columns = Screen.width / 40, rows = Screen.height / 40;
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        mainCamera = Camera.main;

        //CreateGrid();
    }

    private void Update()
    {
        Cursor.visible = false;
        
        cursorTransform.position = Input.mousePosition;

        cursorImage.sprite = Input.GetMouseButton(0) || Input.GetMouseButton(1) ? mouseClickSprite : mouseHoverSprite;

        leftWingImage.enabled = Input.GetMouseButton(0);
        rightWingImage.enabled = Input.GetMouseButton(1);

    }
}
