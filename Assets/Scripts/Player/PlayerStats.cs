using UnityEngine;
using Photon.Pun; // 포톤 추가

public class PlayerStats : MonoBehaviourPun
{
    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float attackDamage = 10f;

    void Start()
    {
        // 시작 시 체력을 꽉 채워줍니다.
        currentHealth = maxHealth;
    }

    // [핵심] PunRPC 속성을 붙이면, 네트워크를 타고 다른 사람의 화면에서도 이 함수가 실행됩니다.
    [PunRPC]
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log(photonView.Owner.NickName + "의 현재 체력: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(photonView.Owner.NickName + " 사망!");
        // 권희님이 여기에 사망 애니메이션 재생이나 아이템 드롭 로직을 추가할 겁니다.
    }
}