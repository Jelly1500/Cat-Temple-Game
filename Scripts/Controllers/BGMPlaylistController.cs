using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// BGM 플레이리스트 컨트롤러
/// - 순차 재생 / 반복 재생 관리
/// - GameManager에서 StartPlaylist()를 호출하여 재생 시작
/// </summary>
public class BGMPlaylistController : MonoBehaviour
{
    #region Singleton

    private static BGMPlaylistController _instance;
    public static BGMPlaylistController Instance => _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    #endregion

    [Header("BGM Playlist Settings")]
    [Tooltip("순서대로 재생할 BGM 파일의 이름(Key)을 입력하세요.")]
    public List<string> playlist = new List<string>();

    private int _currentIndex = 0;
    private bool _isPlaying = false;

    /// <summary>
    /// 플레이리스트 재생 시작
    /// GameManager.InitializeGame()에서 호출
    /// </summary>
    public void StartPlaylist()
    {
        if (_isPlaying) return;
        if (playlist.Count == 0)
        {
#if UNITY_EDITOR 
            Debug.LogWarning("[BGMPlaylist] 재생할 음악 목록이 비어있습니다.");   
#endif

            return;
        }

        // 순차 재생을 위해 Loop 비활성화
        SoundManager.Instance.SetBgmLoop(false);

        // 첫 번째 곡 재생
        _currentIndex = 0;
        PlayCurrentTrack();
        _isPlaying = true;
    }

    /// <summary>
    /// 플레이리스트 재생 중지
    /// </summary>
    public void StopPlaylist()
    {
        _isPlaying = false;
        SoundManager.Instance.Stop(ESound.Bgm);
    }

    /// <summary>
    /// 현재 재생 중인지 여부
    /// </summary>
    public bool IsPlaying => _isPlaying;

    private void Update()
    {
        if (!_isPlaying) return;
        if (playlist.Count == 0) return;

        // 현재 곡이 끝났는지 체크 (게임이 멈춘 상태가 아닐 때만)
        if (!SoundManager.Instance.IsBgmPlaying() && Time.timeScale > 0)
        {
            PlayNextTrack();
        }
    }

    private void PlayCurrentTrack()
    {
        string bgmKey = playlist[_currentIndex];
        SoundManager.Instance.Play2D(ESound.Bgm, bgmKey);
        SoundManager.Instance.SetBgmLoop(false);

    }

    private void PlayNextTrack()
    {
        // 다음 곡으로 이동 (마지막이면 처음으로)
        _currentIndex = (_currentIndex + 1) % playlist.Count;
        PlayCurrentTrack();
    }
}