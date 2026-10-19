using UnityEngine;
using UnityEngine.UI;
public class Enemy : MonoBehaviour

{

    public float maxHealth = 50f;

    private float currentHealth;

    public Slider healthSlider; // 체력 UI (슬라이더)



    void Start() // 시작 시 체력 초기화

    {

        currentHealth = maxHealth;

        UpdateHealthUI(); // 시작 시 체력바 반영

    }



    public void TakeDamage(float damage) // 데미지 처리

    {

        currentHealth -= damage;

        UpdateHealthUI();



        // 피격 확인용 콘솔 로그 출력

        Debug.Log($"[Enemy 피격] 받은 데미지: {damage} / 남은 체력: {currentHealth}");



        if (currentHealth <= 0)

        {

            Destroy(gameObject);

        }

    }



    void UpdateHealthUI() // 체력 UI 업데이트

    {

        if (healthSlider != null)

        {

            healthSlider.value = currentHealth / maxHealth;

        }

    }

}