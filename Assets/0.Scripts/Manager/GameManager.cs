using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    Playing,
    Victory,
    Defeat
}

public class GameManager : MonoBehaviour
{
    public bool isPlaying => state == GameState.Playing;

    [SerializeField] private ButtonForEvent deathSource;
    [SerializeField] private PlayerData playerData;
    [SerializeField] private GameObject bossHealthUI;
    [SerializeField] private ButtonForEvent victorySource;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private Player player;
    [SerializeField] private DamageNumberDisplay damageNumberPrefab;

    // TODO(MainSceneIntegration): GiungScene의 Restart Scene은 현재 테스트를 위해 GiungScene입니다.
    // 정식 게임 연결 시 해당 씬/프리팹의 값을 LoadScene으로 되돌려
    // LoadScene -> MainScene 준비 흐름을 사용하세요. 기본값은 그 정상 경로를 유지합니다.
    [SerializeField] private Define.EScene restartScene = Define.EScene.LoadScene;

    public GameState state { get; private set; } = GameState.Playing;
    public event Action<GameState> StateChanged;

    private bool playerDead;
    private bool finalBossDefeated;
    private bool isTransitioning;

    private void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (damageNumberPrefab != null) Instantiate(damageNumberPrefab, transform);
    }

    private void OnEnable()
    {
        if (deathSource != null) deathSource.PlayerDeath += HandlePlayerDeath;
        if (victorySource != null) victorySource.BossDefeated += HandleBossDefeated;
        if (player != null) player.Died += HandlePlayerDeath;
        if (playerData != null) playerData.PlayerDeath += HandlePlayerDeath;
    }

    private void OnDisable()
    {
        if (deathSource != null) deathSource.PlayerDeath -= HandlePlayerDeath;
        if (victorySource != null) victorySource.BossDefeated -= HandleBossDefeated;
        if (player != null) player.Died -= HandlePlayerDeath;
        if (playerData != null) playerData.PlayerDeath -= HandlePlayerDeath;
    }

    private void HandlePlayerDeath()
    {
        if (state == GameState.Playing) playerDead = true;
    }

    // 최종 승리의 명시적 신호입니다. 일반 보스 한 마리의 사망에는 자동 연결하지 않습니다.
    public void HandleBossDefeated()
    {
        if (state == GameState.Playing) finalBossDefeated = true;
    }

    private void LateUpdate()
    {
        if (state != GameState.Playing) return;
        // 같은 프레임에 두 신호가 오면 기존처럼 사망을 우선합니다.
        if (playerDead) FinishGame(GameState.Defeat);
        else if (finalBossDefeated) FinishGame(GameState.Victory);
    }

    private void FinishGame(GameState result)
    {
        state = result;
        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(result == GameState.Defeat);
        if (victoryPanel != null) victoryPanel.SetActive(result == GameState.Victory);
        if (result == GameState.Defeat && bossHealthUI != null) bossHealthUI.SetActive(false);
        StateChanged?.Invoke(result);

        GameObject panel = result == GameState.Defeat ? gameOverPanel : victoryPanel;
        if (panel == null) return;
        foreach (Button button in panel.GetComponentsInChildren<Button>())
        {
            if (!button.IsInteractable()) continue;
            button.Select();
            break;
        }
    }

    public void RestartGame() { TransitionTo(restartScene); }
    public void ReturnToTitle() { TransitionTo(Define.EScene.TitleScene); }

    private void TransitionTo(Define.EScene target)
    {
        if (isTransitioning) return;
        isTransitioning = true;
        float previousTimeScale = Time.timeScale;
        try
        {
            Time.timeScale = 1f;
            Action loadScene = Managers.Scene.PrepareSceneLoad(target);
            Managers.RunSceneTransition(LoadAfterCleanup(loadScene, previousTimeScale));
        }
        catch
        {
            // 입력 가드와 정지 상태를 되돌려 설정 수정 후 재시도를 허용합니다.
            isTransitioning = false;
            Time.timeScale = previousTimeScale;
            throw;
        }
    }

    private IEnumerator LoadAfterCleanup(Action loadScene, float previousTimeScale)
    {
        // 풀의 SpriteRenderer Destroy가 끝나기 전에 새 씬의 백그라운드 읽기를 시작하면
        // Unity 6000.5에서 PersistentManager/오브젝트 잠금이 교착될 수 있습니다.
        yield return null;
        try { loadScene(); }
        catch
        {
            isTransitioning = false;
            Time.timeScale = previousTimeScale;
            throw;
        }
    }
}
