using UnityEngine;

/// <summary>
/// SoulArena가 빌려 쓰는 Main 2D 아트 참조. 원본 컨트롤러가 Resources 밖에 있어
/// 이 에셋(Resources/SoulPipeline/SoulArenaSpriteSet)이 직렬화 참조로 연결한다.
/// </summary>
[CreateAssetMenu(menuName = "Soul Pipeline/Arena Sprite Set", fileName = "SoulArenaSpriteSet")]
public sealed class SoulArenaSpriteSet : ScriptableObject
{
    public const string ResourcePath = "SoulPipeline/SoulArenaSpriteSet";

    [Tooltip("MainScene BeastZombie의 Animator Controller")]
    [SerializeField] private RuntimeAnimatorController bossController;
    [Tooltip("MainScene BeastZombie Transform 스케일")]
    [SerializeField] private float bossScale = 4f;

    [Tooltip("MainScene 일반 좀비(Enemy 프리팹)의 Animator Controller")]
    [SerializeField] private RuntimeAnimatorController zombieController;
    [Tooltip("MainScene Enemy Transform 스케일")]
    [SerializeField] private float zombieScale = 3f;

    [Tooltip("2D 뼈대 리깅 시험판 원본: MainScene 플레이어 정면 대기(Idle/south_01)")]
    [SerializeField] private Sprite rigPrototypeSprite;

    public RuntimeAnimatorController BossController => bossController;
    public Sprite RigPrototypeSprite => rigPrototypeSprite;
    public float BossScale => bossScale;
    public RuntimeAnimatorController ZombieController => zombieController;
    public float ZombieScale => zombieScale;
}
