using UnityEngine;

/// <summary>
/// 이동량으로 8방향을 잡아 Idle/Run 블렌드 트리에 전달한다.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public sealed class EnemyDirectionAnimator : MonoBehaviour
{
    private static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
    private static readonly int FacingXParameter = Animator.StringToHash("FacingX");
    private static readonly int FacingYParameter = Animator.StringToHash("FacingY");

    [SerializeField] private Animator animator;

    private Vector3 lastPosition;
    private Vector2 facingDirection = Vector2.down;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled)
            return;

        Vector2 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        bool moving = delta.sqrMagnitude > 0.000001f;
        if (moving)
            facingDirection = SnapOctant(delta);

        animator.SetFloat(FacingXParameter, facingDirection.x);
        animator.SetFloat(FacingYParameter, facingDirection.y);
        animator.SetBool(IsMovingParameter, moving);
    }

    private static Vector2 SnapOctant(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;
        int octant = Mathf.RoundToInt(angle / 45f) % 8;
        float snapped = octant * 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped));
    }
}
