using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public StateMachine state = new StateMachine();

    private PlayerStats playerStats;

    public BasicState basicState;
    public SkillCanvasState skillCanvasState;

    public Image healthBar;
    private Image healthBarFill;
    public Image lmbWing, rmbWing, dash;

    private const int WingFillSteps = 5, DashFillSteps = 5;

    private Color basicColor;  // Basic color for most UI elements : RGBA 1, 1, 1, 100/255
    
    void Start()
    {
        state.ChangeState(basicState);
        basicState.Initialize(this);
        skillCanvasState.Initialize(this);

        playerStats = GM.GetPlayer().GetComponent<PlayerStats>();
        healthBarFill = healthBar.rectTransform.GetChild(0).GetComponent<Image>();
        basicColor = healthBar.color;
    }

    void Update()
    {
        state.Update();
        HandleDisabledStates();
        HandleHealthBar();
        HandleWings();
        HandleDash();
    }

    void HandleDisabledStates()
    {
        if (!state.CompareType(basicState))
        {
            basicState.Disabled();
        }

        if (!state.CompareType(skillCanvasState))
        {
            skillCanvasState.Disabled();
        }
    }

    void HandleHealthBar()
    {
        float hp = playerStats.health;
        float maxHp = playerStats.maxHealth;

        healthBarFill.fillAmount = hp / maxHp;
        
        // I'm too lazy to make animations for each UI element so I'm gonna hard-code them in. lol
        float speed1 = 0.5f, speed2 = 1f;
        healthBarFill.color = Color.Lerp(healthBarFill.color, Color.white, speed1 * Time.deltaTime);
        healthBar.color = Color.Lerp(healthBar.color, basicColor, speed2 * Time.deltaTime);
    }

    void HandleWings()
    {
        float speed = 1f;
        lmbWing.color = Color.Lerp(lmbWing.color, basicColor, speed * Time.deltaTime);
        rmbWing.color = Color.Lerp(rmbWing.color, basicColor, speed * Time.deltaTime);
    }

    void HandleDash()
    {
        float speed = 1f;
        dash.color = Color.Lerp(dash.color, basicColor, speed * Time.deltaTime);
    }
    
    // Public Methods
    public void TakeDamage()
    {
        healthBarFill.color = Color.red;
        healthBar.color = new Color(1, 0, 0, 100f / 255f);
    }

    public void SetWingFill(bool right, float fill)
    {
        float fillAmount = fill < 1f ? fill - fill%(1f/WingFillSteps) : 1f;
        
        if (right)
        {
            rmbWing.fillAmount = fillAmount;
        }
        else
        {
            lmbWing.fillAmount = fillAmount;
        }
    }

    public void SetWingColor(bool right, Color color)
    {
        if (right)
        {
            rmbWing.color = color;
        }
        else
        {
            lmbWing.color = color;
        }
    }

    public void SetDashFill(float fill)
    {
        dash.fillAmount = fill < 1f ? fill - fill%(1f/DashFillSteps) : 1f;
    }

    public void SetDashColor(Color color)
    {
        dash.color = color;
    }

    [Serializable]
    public class BasicState : IState
    {
        private InGameUI inGameUI;
        private Animator animator;

        [SerializeField]
        private GameObject panel;

        public BasicState(GameObject panel)
        {
            this.panel = panel;
        }
        
        public void Initialize(InGameUI inGameUI)
        {
            this.inGameUI = inGameUI;
            animator = inGameUI.GetComponent<Animator>();
        }
        
        public void Enter()
        {
            panel.SetActive(true);
        }
        
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Trigger skill canvas
                animator.SetTrigger("OnSpacePress");
            }
        }

        public void Exit()
        {
            panel.SetActive(false);
        }

        public void Disabled()
        {
            
        }
    }
    
    [Serializable]
    public class SkillCanvasState : IState
    {
        private InGameUI inGameUI;
        private Animator animator;

        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform[] tiles;
        private Image[] tileImages;

        public Color masterTileColor;

        private const int TileSize = 140;
        [SerializeField] private Vector3 activeTileScale = new Vector3(1.2f, 1.2f, 1f);
        private const float ColorChangeSpeed = 0.25f;
        private Color disabledTileColor;

        private List<int> activeTileIndexes;

        [SerializeField] private LineRenderer lineRenderer;

        [SerializeField] private float setTimeScale;

        private Vector2 prevMousePosition;

        public SkillCanvasState(GameObject panel)
        {
            this.panel = panel;
        }
        
        public void Initialize(InGameUI inGameUI)
        {
            this.inGameUI = inGameUI;
            animator = inGameUI.GetComponent<Animator>();
            tileImages = tiles.Select(t => t.GetComponent<Image>()).ToArray();
            disabledTileColor = tileImages[0].color;
        }

        public void Enter()
        {
            activeTileIndexes = new List<int>();
            panel.SetActive(true);
            GM.Instance.physicsSpeedMutlitplier = setTimeScale;
            prevMousePosition = Input.mousePosition;
        }

        public void Update()
        {
            Vector2 mouseScreenPos = Input.mousePosition;
            int cap = 8;
            List<Vector2> mouseInbetweenPositions = new List<Vector2>(cap);
            for (int i = 1; i <= cap; i++)
            {
                mouseInbetweenPositions.Add(prevMousePosition + i*(mouseScreenPos - prevMousePosition)/cap);
            }
            prevMousePosition = mouseScreenPos;

            if (Input.GetMouseButtonDown(0))
                activeTileIndexes = new List<int>();

            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];

                if (mouseScreenPos.x > Screen.width / 2 + tile.anchoredPosition.x - TileSize / 2f
                    && mouseScreenPos.x < Screen.width / 2 + tile.anchoredPosition.x + TileSize / 2f
                    && mouseScreenPos.y > Screen.height / 2 + tile.anchoredPosition.y - TileSize / 2f
                    && mouseScreenPos.y < Screen.height / 2 + tile.anchoredPosition.y + TileSize / 2f
                    && Input.GetMouseButton(0))
                {
                    // Activate tile
                    if (!activeTileIndexes.Contains(i))
                    {
                        activeTileIndexes.Add(i);
                    }
                }

                
                if (activeTileIndexes.Contains(i) && Input.GetKey(KeyCode.Space))
                {
                    tileImages[i].color = Color.Lerp(tileImages[i].color, Color.white, ColorChangeSpeed);
                    tiles[i].localScale = Vector3.Lerp(tiles[i].localScale, activeTileScale, ColorChangeSpeed);
                }
                else
                {
                    tileImages[i].color = Color.Lerp(tileImages[i].color, disabledTileColor, ColorChangeSpeed);
                    tiles[i].localScale = Vector3.Lerp(tiles[i].localScale, Vector3.one, ColorChangeSpeed);
                }

                
                if (!Input.GetKey(KeyCode.Space))
                {
                    // Fade out
                    // Random botched up solution that's surely gonna cause problems in the future.
                    tileImages[i].color *= new Color(1, 1, 1, 0.5f);
                    GM.Instance.physicsSpeedMutlitplier = 1f;
                }
            }

            if (Input.GetMouseButtonUp(0) && activeTileIndexes.Count > 0)
            {
                print("Skill Triggered: " + activeTileIndexes.Count);
                activeTileIndexes = new List<int>();
            }

            // Handle line renderer
            if (activeTileIndexes.Count > 0)
            {
                lineRenderer.positionCount = activeTileIndexes.Count + 1;
                for(int i = 0; i < activeTileIndexes.Count; i++)
                {
                    lineRenderer.SetPosition(i, Camera.main.ScreenToWorldPoint(tiles[activeTileIndexes[i]].position)
                                                - new Vector3(0,0, Camera.main.transform.position.z));
                }
                lineRenderer.SetPosition(activeTileIndexes.Count, Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                if (activeTileIndexes.Count > 0)
                {
                    print("Skill Triggered: " + activeTileIndexes.Count);
                    activeTileIndexes = new List<int>();
                }
                animator.SetTrigger("OnSpaceRelease");
            }

            
        }

        public void Exit()
        {
            panel.SetActive(false);
            activeTileIndexes = new List<int>();
        }
        
        public void Disabled()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                tileImages[i].color = Color.Lerp(tileImages[i].color, disabledTileColor, 5f * Time.deltaTime);
            }
        }

        public void SetMasterColor(Color color)
        {
            masterTileColor = color;
            for (int i = 0; i < tiles.Length; i++)
            {
                tileImages[i].color = masterTileColor;
            }
        }
    }

    public void BasicToSkillCanvasAnimationStart()
    {
        skillCanvasState.SetMasterColor(new Color(1,1,1,0));
    }

    public void BasicToSkillCanvasAnimationEnd()
    {
        state.ChangeState(skillCanvasState);
    }
    
    public void SkillCanvasToBasicAnimationEnd()
    {
        state.ChangeState(basicState);
    }
}
