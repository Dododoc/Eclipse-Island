using UnityEngine;
using Photon.Pun;
using System.Collections;

public class InteractableTree : MonoBehaviourPun
{
    [Header("나무 스탯")]
    public float maxHealth = 30f;
    public float currentHealth;
    public int woodYield = 3;

    [Header("드롭 아이템 (Resources 폴더에 있어야 함!)")]
    public string woodItemPrefabName = "Item_Wood"; 

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
    }

    [PunRPC]
    public void TakeDamage(float damage, Vector3 hitterPosition)
    {
        if (isDead) return;

        currentHealth -= damage;
        StartCoroutine(HitFeedback());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFeedback()
    {
        transform.rotation = Quaternion.Euler(0, 0, 5f);
        yield return new WaitForSeconds(0.1f);
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    private void Die()
    {
        isDead = true;

        if (PhotonNetwork.IsMasterClient)
        {
            for (int i = 0; i < woodYield; i++)
            {
                // 1. 퍼지는 범위를 조금 더 넓혀서(1.0f) 확실하게 흩뿌려지게 합니다.
                Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0);
                Vector3 finalPos = transform.position + randomOffset;
                
                // 2. [핵심] 아이템에게 '나무의 원래 위치(Pivot)'를 알려주기 위해 데이터를 포장합니다.
                object[] initData = new object[] { transform.position };
                
                // 3. 아이템을 소환하면서 데이터를 같이 던져줍니다.
                PhotonNetwork.InstantiateRoomObject(woodItemPrefabName, finalPos, Quaternion.identity, 0, initData);
            }
            
            PhotonNetwork.Destroy(gameObject);
        }
    }
}