using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_RenownEventDialogue : UI_UGUI, IUI_Popup
{
    enum Texts
    {
        Text_Dialogue,
        Text_SpeakerName,
    }

    enum Images
    {
        Image_Portrait_Left,
        Image_Portrait_Right,
        Image_Emotion_Left,
        Image_Emotion_Right,
    }

    enum Buttons
    {
        Btn_Next,
        Btn_Left,
        Btn_Right,
    }

    private Action _onDialogueFinished;
    private Action _onLeftChoice;
    private Action _onRightChoice;

    // 감정 아이콘 원점 위치 (애니메이션 후 복귀용)
    private Vector2 _emotionLeftOrigin;
    private Vector2 _emotionRightOrigin;
    private bool _emotionOriginsRecorded;

    // 감정 아이콘 상승 애니메이션 설정
    private const float EMOTION_ANIM_DURATION = 2f;
    private const float EMOTION_ANIM_RISE_HEIGHT = 80f;

    private Coroutine _emotionLeftCoroutine;
    private Coroutine _emotionRightCoroutine;

    // [추가] 타이핑 효과 관련 변수
    private bool _isTyping = false;
    private string _fullDialogueText = "";
    private Coroutine _typingCoroutine = null;
    private bool _isChoiceDialogue = false; // 선택지 대화 모드 여부
    private const float TYPING_SPEED = 0.05f; // 타자 속도 설정

    public override void Init()
    {
        if (_init) return;
        base.Init();

        Bind<TMP_Text>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<Button>(typeof(Buttons));

        GetButton((int)Buttons.Btn_Next).onClick.AddListener(OnClickNextButton);

        Button btnLeft = GetButton((int)Buttons.Btn_Left);
        Button btnRight = GetButton((int)Buttons.Btn_Right);
        btnLeft.onClick.AddListener(OnClickLeftButton);
        btnRight.onClick.AddListener(OnClickRightButton);

        if (!_emotionOriginsRecorded)
        {
            _emotionLeftOrigin = GetImage((int)Images.Image_Emotion_Left).rectTransform.anchoredPosition;
            _emotionRightOrigin = GetImage((int)Images.Image_Emotion_Right).rectTransform.anchoredPosition;
            _emotionOriginsRecorded = true;
        }

        GetImage((int)Images.Image_Emotion_Left).gameObject.SetActive(false);
        GetImage((int)Images.Image_Emotion_Right).gameObject.SetActive(false);

        HideChoiceButtons();
    }

    public void SetDialogue(string content,
                            string leftName, Sprite leftSprite,
                            string rightName, Sprite rightSprite,
                            SpeakerSide side,
                            Sprite leftEmotionSprite, Sprite rightEmotionSprite,
                            Action onFinished)
    {
        Init();

        _isChoiceDialogue = false;
        HideChoiceButtons();
        GetButton((int)Buttons.Btn_Next).gameObject.SetActive(true);

        // 1. 대사 텍스트 타이핑 효과 설정
        _fullDialogueText = content;
        if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
        _typingCoroutine = StartCoroutine(CoTypewriterText());

        // 2. 화자 이름 설정
        SetSpeakerName(side, leftName, rightName);

        // 3. 캐릭터 초상화 설정
        UpdatePortraitUI(Images.Image_Portrait_Left, leftSprite, side == SpeakerSide.Left);
        UpdatePortraitUI(Images.Image_Portrait_Right, rightSprite, side == SpeakerSide.Right);

        // 4. 감정 아이콘 애니메이션 재생
        PlayEmotionIcon(Images.Image_Emotion_Left, leftEmotionSprite, _emotionLeftOrigin, ref _emotionLeftCoroutine);
        PlayEmotionIcon(Images.Image_Emotion_Right, rightEmotionSprite, _emotionRightOrigin, ref _emotionRightCoroutine);

        _onDialogueFinished = onFinished;
    }

    public void SetChoiceDialogue(string content,
                                  string leftName, Sprite leftSprite,
                                  string rightName, Sprite rightSprite,
                                  SpeakerSide side,
                                  Sprite leftEmotionSprite, Sprite rightEmotionSprite,
                                  string leftChoiceText, string rightChoiceText,
                                  Action onLeft, Action onRight)
    {
        // 일반 대화 세팅 재활용 (타이핑 코루틴이 시작됨)
        SetDialogue(content, leftName, leftSprite, rightName, rightSprite, side,
                    leftEmotionSprite, rightEmotionSprite, null);

        _isChoiceDialogue = true;

        // 선택지 대화라도 타이핑이 끝날 때까지는 선택지 버튼을 숨기고 스킵 버튼(Next)을 활성화
        HideChoiceButtons();
        GetButton((int)Buttons.Btn_Next).gameObject.SetActive(true);

        // 텍스트 인자가 있으므로 버튼 하위의 TMP_Text에 세팅
        TMP_Text leftText = GetButton((int)Buttons.Btn_Left).GetComponentInChildren<TMP_Text>();
        if (leftText != null) leftText.text = leftChoiceText;

        TMP_Text rightText = GetButton((int)Buttons.Btn_Right).GetComponentInChildren<TMP_Text>();
        if (rightText != null) rightText.text = rightChoiceText;

        _onLeftChoice = onLeft;
        _onRightChoice = onRight;
    }

    // [신규] 텍스트 타이핑 코루틴
    private IEnumerator CoTypewriterText()
    {
        _isTyping = true;
        TMP_Text dialogueText = GetText((int)Texts.Text_Dialogue);
        dialogueText.text = "";

        for (int i = 0; i < _fullDialogueText.Length; i++)
        {
            dialogueText.text += _fullDialogueText[i];

            // 일반적인 WaitForSeconds 대신 실제 프레임 간의 시간(unscaledDeltaTime)을 직접 누적하여 대기
            float timer = 0f;
            while (timer < TYPING_SPEED)
            {
                timer += Time.unscaledDeltaTime;
                yield return null; // 프레임 단위로 제어권을 넘겨 UI가 멈추지 않게 함
            }
        }

        CompleteTyping();
    }

    // [신규] 타이핑 완료 혹은 즉시 스킵 시 실행되는 로직
    private void CompleteTyping()
    {
        _isTyping = false;
        GetText((int)Texts.Text_Dialogue).text = _fullDialogueText;

        // 선택지 모드일 경우 타이핑 완료 후 백그라운드 클릭을 비활성화하고 선택지 버튼 노출
        if (_isChoiceDialogue)
        {
            GetButton((int)Buttons.Btn_Next).gameObject.SetActive(false);
            GetButton((int)Buttons.Btn_Left).gameObject.SetActive(true);
            GetButton((int)Buttons.Btn_Right).gameObject.SetActive(true);
        }
    }

    // [수정] 배경 버튼(Next) 클릭 로직 분기
    private void OnClickNextButton()
    {
        if (_isTyping)
        {
            // 타이핑 중이면 즉시 중단하고 모든 글자 출력 (1회차 클릭)
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }
            CompleteTyping();
        }
        else
        {
            // 타이핑이 이미 완료되었고, 일반 대화일 때만 다음 액션 호출 (2회차 클릭)
            if (!_isChoiceDialogue)
            {
                _onDialogueFinished?.Invoke();
            }
        }
    }

    private void SetSpeakerName(SpeakerSide side, string leftName, string rightName)
    {
        TMP_Text speakerText = GetText((int)Texts.Text_SpeakerName);
        string currentName = "";

        switch (side)
        {
            case SpeakerSide.Left: currentName = leftName; break;
            case SpeakerSide.Right: currentName = rightName; break;
            case SpeakerSide.None:
            default: currentName = ""; break;
        }

        if (string.IsNullOrEmpty(currentName))
        {
            speakerText.text = "";
            speakerText.gameObject.SetActive(false);
        }
        else
        {
            speakerText.text = currentName;
            speakerText.gameObject.SetActive(true);
        }
    }

    private void UpdatePortraitUI(Images imgEnum, Sprite sprite, bool isSpeaker)
    {
        Image portrait = GetImage((int)imgEnum);

        if (sprite != null)
        {
            portrait.sprite = sprite;
            portrait.gameObject.SetActive(true);
            portrait.color = isSpeaker ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f);
        }
        else
        {
            portrait.gameObject.SetActive(false);
        }
    }

    private void PlayEmotionIcon(Images imgEnum, Sprite emotionSprite, Vector2 originPos, ref Coroutine runningCoroutine)
    {
        Image emotionImage = GetImage((int)imgEnum);
        RectTransform rt = emotionImage.rectTransform;

        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            runningCoroutine = null;
        }

        rt.anchoredPosition = originPos;
        emotionImage.color = Color.white;
        emotionImage.gameObject.SetActive(false);

        if (emotionSprite == null) return;

        emotionImage.sprite = emotionSprite;
        emotionImage.gameObject.SetActive(true);

        runningCoroutine = StartCoroutine(CoEmotionRiseAndFade(emotionImage, originPos));
    }

    private IEnumerator CoEmotionRiseAndFade(Image emotionImage, Vector2 originPos)
    {
        RectTransform rt = emotionImage.rectTransform;
        float elapsed = 0f;

        Vector2 startPos = originPos;
        Vector2 endPos = new Vector2(originPos.x, originPos.y + EMOTION_ANIM_RISE_HEIGHT);

        while (elapsed < EMOTION_ANIM_DURATION)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / EMOTION_ANIM_DURATION);

            float easedT = 1f - (1f - t) * (1f - t);
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easedT);

            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                emotionImage.color = new Color(1f, 1f, 1f, 1f - fadeT);
            }

            yield return null;
        }

        emotionImage.gameObject.SetActive(false);
        rt.anchoredPosition = startPos;
        emotionImage.color = Color.white;
    }

    private void HideChoiceButtons()
    {
        GetButton((int)Buttons.Btn_Left).gameObject.SetActive(false);
        GetButton((int)Buttons.Btn_Right).gameObject.SetActive(false);
    }

    private void OnClickLeftButton() { _onLeftChoice?.Invoke(); }
    private void OnClickRightButton() { _onRightChoice?.Invoke(); }
}