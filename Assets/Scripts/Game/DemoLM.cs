using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using System.Resources;
using TMPro;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DemoLM : MonoBehaviour
{
    [System.Serializable]
    public struct TriggerEvent
    {
        public string name;
        public TriggerObject triggerObject;
    }

    public List<TriggerEvent> triggerEvents;

    public GameObject[] flies;
    public GameObject[] mimics;
    public Animator bossRoomGateAnimator;

    public float bossFightTime = 120f;
    public TextMeshProUGUI timer;
    public bool bossFightStarted = false;
    private List<MobStats> mimicStats = new List<MobStats>();
    private PlayerStats playerStats;
    private bool endFlag = false;

    public float score = 0f;

    public TextMeshProUGUI tutorialText, tipText;

    public GameObject mimicHealthBar;
    public Image mimicHealthBarFill;
    
    void Start()
    {
        SetupTriggerActions();
        timer.gameObject.SetActive(false);
        playerStats = GM.PlayerInstance.GetComponent<PlayerStats>();
        tutorialText.text = "Press [W,A,S,D] to move";
        tipText.text = "([S] does nothing)";
        mimicHealthBar.SetActive(false);
    }

    void Update()
    {
        int n = 0;
        foreach (var fly in flies)
        {
            if (fly.activeSelf && fly.GetComponent<MobStatsInterface>().stats.Dead)
                n++;
        }

        if (n == 3)
        {
            bossRoomGateAnimator.SetTrigger("Open Gate");
        }

        if (bossFightStarted && !endFlag)
        {
            mimicHealthBarFill.fillAmount = (mimicStats[0].health / mimicStats[0].maxHealth);
            bool mimicsAlive = false;
            foreach (var mimicStat in mimicStats)
            {
                if (!mimicStat.Dead)
                    mimicsAlive = true;
            }
            if(mimicsAlive && bossFightTime > 0f && playerStats.health > 0f)
                HandleTimer();
            else
            {
                float mimicHealthScoreDeduction = 0f;
                foreach (var mimicStat in mimicStats)
                {
                    mimicHealthScoreDeduction += mimicStat.health * 0.5f;
                }
                score = playerStats.health + 100f - mimicHealthScoreDeduction;
                if (!mimicsAlive)
                    score += bossFightTime;
                timer.text = "Score: " + Mathf.FloorToInt(score).ToString();
                DisplayEndLevelText();
                endFlag = true;
            }
        }

        HandleReloadLevel();
    }

    void SetupTriggerActions()
    {
        triggerEvents[0].triggerObject.triggerAction = SpawnFlies;
        triggerEvents[0].triggerObject.destroyOnTrigger = true;
        triggerEvents[1].triggerObject.triggerAction = EnterBossRoom;
        triggerEvents[1].triggerObject.destroyOnTrigger = true;
        triggerEvents[2].triggerObject.triggerAction = DashTutorial;
        triggerEvents[2].triggerObject.destroyOnTrigger = true;
    }

    void SpawnFlies()
    {
        foreach (var fly in flies)
        {
            fly.SetActive(true);
        }

        tutorialText.text = "Hold [Left+Right Click] to control wings";
        tipText.text = "Wings deal additional damage with higher velocity";
    }

    void EnterBossRoom()
    {
        for (int i = 0; i < mimics.Length; i++)
        {
            var mimic = mimics[i];
            mimic.SetActive(true);
            mimicStats.Add(mimic.GetComponent<MobStatsInterface>().stats);
        }

        CloseBossRoomDoor();
        bossFightStarted = true;
        timer.gameObject.SetActive(true);
        
        mimicHealthBar.SetActive(true);
        tutorialText.text = "Good luck.";
        tipText.text = "Defeat the Mimic in under 2 minutes";
        StartCoroutine(DisableTutorialText(3f));
        playerStats.Heal(100f);
    }

    void CloseBossRoomDoor()
    {
        bossRoomGateAnimator.SetTrigger("Close Gate");
    }

    void HandleTimer()
    {
        string timerText = "0";
        bossFightTime -= Time.deltaTime;
        string minute = bossFightTime >= 60f ? "1" : "0";
        string seconds = Mathf.FloorToInt(bossFightTime % 60f).ToString();
        if (seconds.Length == 1)
            seconds = "0" + seconds;
        timerText += minute + ":" + seconds;
        timer.text = timerText;
    }

    void DashTutorial()
    {
        tutorialText.text = "Press [F or Left Shift] to dash towards mouse";
        tipText.text = "Cooldown: 3 seconds";
    }

    IEnumerator DisableTutorialText(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // After delay
        // I have no idea how this code works -> ask GPT.
        tutorialText.text = string.Empty;
        tipText.text = string.Empty;
    }

    void HandleReloadLevel()
    {
        if (Input.GetKey(KeyCode.F5))
        {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKey(KeyCode.F6))
        {
            SceneManager.LoadScene(1);
        }
    }

    void DisplayEndLevelText()
    {
        tutorialText.gameObject.SetActive(true);
        tutorialText.text = "Press [F5] to restart level";
    }
}
