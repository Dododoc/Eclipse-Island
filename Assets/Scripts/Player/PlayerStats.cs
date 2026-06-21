using UnityEngine;
using Photon.Pun; // 포톤 추가

public class PlayerStats : MonoBehaviourPun, IPunObservable
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Hunger")]
    [Tooltip("\ucd5c\ub300 \ud5c8\uae30 \uc218\uce58")]
    public float maxHunger = 100f;
    public float currentHunger;
    [Tooltip("\ucd08\ub2f9 \ud5c8\uae30 \uac10\uc18c\ub7c9")]
    public float hungerDrainPerSecond = 0.5f;
    [Tooltip("\ud5c8\uae30\uac00 0\uc77c \ub54c \ucd08\ub2f9 \uc785\ub294 \uccb4\ub825 \ub370\ubbf8\uc9c0")]
    public float starveDamagePerSecond = 2f;

    [Header("Combat")]
    public float attackDamage = 10f;

    [Header("Death")]
    public bool isDowned = false;

    public event System.Action<float, float, float, float> OnStatsChanged;

    void Start()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        RaiseStatsChanged();
    }

    void Update()
    {
        if (!photonView.IsMine || isDowned)
        {
            return;
        }

        bool changed = false;

        if (currentHunger > 0f)
        {
            currentHunger = Mathf.Max(0f, currentHunger - hungerDrainPerSecond * Time.deltaTime);
            changed = true;
        }

        if (currentHunger <= 0f && currentHealth > 0f)
        {
            currentHealth = Mathf.Max(0f, currentHealth - starveDamagePerSecond * Time.deltaTime);
            changed = true;

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        if (changed)
        {
            RaiseStatsChanged();
        }
    }

    public void ApplyDamage(float damage)
    {
        photonView.RPC(nameof(TakeDamage), photonView.Owner, damage);
    }

    [PunRPC]
    public void TakeDamage(float damage)
    {
        if (!photonView.IsMine || isDowned)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        RaiseStatsChanged();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (!photonView.IsMine || isDowned)
        {
            return;
        }
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        RaiseStatsChanged();
    }

    public void Eat(float amount)
    {
        if (!photonView.IsMine)
        {
            return;
        }
        currentHunger = Mathf.Min(maxHunger, currentHunger + amount);
        RaiseStatsChanged();
    }

    private void Die()
    {
        if (isDowned)
        {
            return;
        }
        isDowned = true;
        Debug.Log(photonView.Owner.NickName + " \uae30\uc808(\uc804\ud22c \ubd88\ub2a5)!");
    }

    private void RaiseStatsChanged()
    {
        OnStatsChanged?.Invoke(currentHealth, maxHealth, currentHunger, maxHunger);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(currentHealth);
            stream.SendNext(currentHunger);
            stream.SendNext(isDowned);
        }
        else
        {
            currentHealth = (float)stream.ReceiveNext();
            currentHunger = (float)stream.ReceiveNext();
            isDowned = (bool)stream.ReceiveNext();
            RaiseStatsChanged();
        }
    }
}