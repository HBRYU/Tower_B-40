using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public StateMachine state = new StateMachine();

    public BasicState basicState;
    public SkillCanvasState skillCanvasState;

    public Queue<IState> stateQueue = new Queue<IState>();
    
    void Start()
    {
        state.ChangeState(basicState);
        basicState.Initialize(this);
        skillCanvasState.Initialize(this);
    }

    void Update()
    {
        state.Update();

        if (!state.CompareType(basicState))
        {
            basicState.Disabled();
        }

        if (!state.CompareType(skillCanvasState))
        {
            skillCanvasState.Disabled();
        }
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
        }

        public void Update()
        {
            Vector2 mouseScreenPos = Input.mousePosition;

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
