using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UI_Visitor : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Slider _moodGauge;      // 공감/안도 통합 게이지
    [SerializeField] private GameObject _iconRoot;   // 아이콘 부모 객체
    [SerializeField] private Image _statusIcon;      // 상태 아이콘 (경청/공감/지혜)

    [Header("Sprites")]
    [SerializeField] private Sprite _spriteListen;
    [SerializeField] private Sprite _spriteHeart;
    [SerializeField] private Sprite _spriteWisdom;

    private Camera _cam;

    public void Init()
    {
        _cam = Camera.main;
        _moodGauge.value = 0;
        _iconRoot.SetActive(false);
    }

    private void LateUpdate()
    {
        // UI가 항상 카메라를 바라보도록 설정 (빌보드)
        if (_cam != null)
        {
            transform.rotation = _cam.transform.rotation;
        }
    }

    public void UpdateGauge(float current, float max)
    {
        _moodGauge.value = current / max;
    }

    // 0.5초 동안 아이콘 보여주기
    public IEnumerator CoShowIcon(string type)
    {
        Sprite targetSprite = null;
        switch (type)
        {
            case "Listen": targetSprite = _spriteListen; break;
            case "Empathy": targetSprite = _spriteHeart; break;
            case "Wisdom": targetSprite = _spriteWisdom; break;
        }

        if (targetSprite != null)
        {
            _statusIcon.sprite = targetSprite;
            _iconRoot.SetActive(true);

            // 0.5초 대기
            yield return new WaitForSeconds(0.5f);

            _iconRoot.SetActive(false);
        }
    }
}