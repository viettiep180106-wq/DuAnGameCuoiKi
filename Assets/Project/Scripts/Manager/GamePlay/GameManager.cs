using System.ComponentModel;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public UnityAction<bool> OnWinGame;
    public UnityAction<bool> OnLoseGame;
    [Header("Setting Element Game")]
    public GameObject Win;
    public GameObject Lose;
    public Image LockBoard ;
    [SerializeField]private FillBarUI TimeLimit;
    [SerializeField]private float time;
    [SerializeField]private int step =1;
    [Header("Setting Button")]
    [SerializeField]private Button StartStep;
    [SerializeField]private Button RollBuff;
    bool isStartStep = false;
    private void Awake()
    {
        instance = this;
        StartStep.onClick.AddListener(() =>
        {
            LockBoard.gameObject.SetActive(true);
            TimeLimit.uiImage.DOKill();
            LevelManager.instance.OnStepStart?.Invoke(step);
            step++;
            isStartStep = true;
        });
        OnLoseGame+=LoseGame;
        OnWinGame+= WinGame;
    }
    private void Start()
    {
        LevelManager.instance.OnEquipPreMatch?.Invoke(step);
        PlayTime(time);
    }
    private void Update()
    {
        if(TimeLimit.GetData() == 0)
        {
            StartStep.onClick.Invoke();
        }
        if(isStartStep == true)
        {
            if(EnemyController.instance.numEnemy == 0)
            {
                OnWinGame?.Invoke(true);
            }
        }
        
    }
    public void PlayTime(float timePre)
    {
        TimeLimit.DoFill(0,timePre);
    }
    public void LoseGame(bool set)
    {
       Lose.SetActive(set);
    }
    public void WinGame(bool set)
    {
        Win.SetActive(set);
    }
}
