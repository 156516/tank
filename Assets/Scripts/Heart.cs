using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heart : MonoBehaviour
{
    private SpriteRenderer sr;

    public GameObject explosionPrefab;

    public AudioClip dieAudio;

    public Sprite BrokenSprite;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void Die()
    {
        sr.sprite = BrokenSprite;
        Instantiate(explosionPrefab, transform.position, transform.rotation);
        // 通过 PlayerManager 的方法触发失败(双人模式下也由其统一管理)
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.TriggerDefeat();
        }
        AudioSource.PlayClipAtPoint(dieAudio, transform.position);
    }
}
