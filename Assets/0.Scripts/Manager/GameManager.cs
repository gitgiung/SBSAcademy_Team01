using UnityEngine;
using UnityEngine.Serialization;

public enum GameState
{
    Playing,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    public bool isPlaying =>  state == GameState.Playing;
    
    [SerializeField] private ButtonForEvent deathSource;
    [SerializeField] private ButtonForEvent  victorySource;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    public GameState state { get; private set; } = GameState.Playing;

    private bool playerDead;
    private bool finalBossDefeated;
    private bool isTransitioning;

    private void Awake()
    {
        gameOverPanel.SetActive(false);
        victoryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        deathSource.PlayerDeath += HandlePlayerDeath;
        victorySource.BossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        deathSource.PlayerDeath -= HandlePlayerDeath;
        victorySource.BossDefeated -= HandleBossDefeated;
    }

    private void HandlePlayerDeath()
    {
        if (state != GameState.Playing)
            return;

        playerDead = true;
    }

    public void HandleBossDefeated()
    {
        if (state != GameState.Playing)
            return;

        finalBossDefeated = true;
    }

    private void LateUpdate()
    {
        if (state != GameState.Playing)
            return;

        // 사망을 먼저 검사해서 패배를 우선한다.
        if (playerDead)
        {
            FinishGame(GameState.Defeat);
        }
        else if (finalBossDefeated)
        {
            FinishGame(GameState.Victory);
        }
    }

    private void FinishGame(GameState result)
    {
        state = result;

        Time.timeScale = 0f; // 게임이 끝나면 시간 정지
        
        gameOverPanel.SetActive(result == GameState.Defeat);
        victoryPanel.SetActive(result == GameState.Victory);
    }

    public void RestartGame()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        
        Time.timeScale = 1f; // 재시작시 시간 복구

        Managers.Scene.LoadScene(Define.EScene.LoadTestScene);
    }

    public void ReturnToTitle()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;
        
        Time.timeScale = 1f; // 타이틀로 이동시 시간 복구

        Managers.Scene.LoadScene(Define.EScene.TitleTestScene);
    }
}