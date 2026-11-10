using UnityEngine;

/// <summary>CharacterController용 단순 중력. 평탄 맵에서 떠다니지 않게 한다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class SoulGroundGravity : MonoBehaviour
{
    [SerializeField] private float gravity = -20f;
    private CharacterController character;
    private float vertical;

    private void Awake() => character = GetComponent<CharacterController>();

    private void Update()
    {
        if (character == null || !character.enabled) return;
        if (character.isGrounded && vertical < 0f)
            vertical = -1f;
        else
            vertical += gravity * Time.deltaTime;

        character.Move(new Vector3(0f, vertical * Time.deltaTime, 0f));
    }
}
