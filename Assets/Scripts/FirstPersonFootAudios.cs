using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonFootAudios : MonoBehaviour
{
    [Header("第一人称行走音频")]
    [Tooltip("这里拖入走路的音频片段，可以放多个，按顺序循环播放")]
    public AudioClip[] footstepClips;

    [Header("相关设置")]
    [Tooltip("走路时的声音大小（默认值，会被设置面板覆盖）")]
    [Range(0, 1)] public float volume = 0.5f;

    [Tooltip("步频间隔（米）：每移动多少米播放一次声音（默认值，会被设置面板覆盖）")]
    public float stepDistance = 1.8f;

    //角色控制器组件
    private CharacterController _controller;
    //音频播放组件
    private AudioSource _audioSource;
    //累计的移动距离
    private float _distanceTravelled;
    //当前播放的音频索引（用于顺序循环播放）
    private int currentClipIndex = 0;

    void Awake()
    {
        // 注册到设置面板，接收配置更新
        if (SettingPanel.Instance != null)
        {
            SettingPanel.RegisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void OnDestroy()
    {
        // 注销设置应用方法
        if (SettingPanel.Instance != null)
        {
            SettingPanel.UnregisterApplyMethod(ApplyCurrentSettings);
        }
    }

    void Start()
    {
        //初始化获得角色控制组件
        _controller = GetComponent<CharacterController>();
        //初始获得音频播放组件
        _audioSource = GetComponent<AudioSource>();

        // 确保音频源设置正确
        _audioSource.spatialBlend = 1.0f; // 2D声音，直接入耳
        _audioSource.playOnAwake = false;

        // 应用当前设置
        if (SettingPanel.Instance != null)
        {
            ApplyCurrentSettings(SettingPanel.CurrentSettings);
        }
    }

    // 设置应用方法
    private void ApplyCurrentSettings(SettingPanel.SettingDate settings)
    {
        volume = settings.footstepVolume;
        stepDistance = settings.stepDistance;

        Debug.Log($"脚步声: 同步设置 - 音量: {volume}, 步频: {stepDistance}");
    }

    void Update()
    {
        CheckFootsteps();
    }

    private void CheckFootsteps()
    {
        // 1. 如果角色没有在地面上，或者没有移动，直接返回
        if (_controller == null || !_controller.isGrounded || _controller.velocity.sqrMagnitude < 0.1f)
        {
            return;
        }

        // 2. 累加移动距离
        // 使用 magnitude 获取当前帧移动的距离
        _distanceTravelled += _controller.velocity.magnitude * Time.deltaTime;

        // 3. 达到步频距离，播放声音
        if (_distanceTravelled >= stepDistance)
        {
            // 播放音频
            PlayFootstepSound();
            // 重置位移距离记录
            _distanceTravelled = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        // 如果行走音频资源为0，则直接返回，什么都不播放
        if (footstepClips == null || footstepClips.Length == 0)
        {
            return;
        }

        // 确保索引在有效范围内
        if (currentClipIndex >= footstepClips.Length)
        {
            currentClipIndex = 0;
        }

        // 获取当前要播放的音频
        AudioClip currentClip = footstepClips[currentClipIndex];

        // 稍微改变音调，让声音听起来不那么机械 (0.9 ~ 1.1 之间随机)
        _audioSource.pitch = Random.Range(0.9f, 1.1f);

        // 播放一次
        _audioSource.PlayOneShot(currentClip, volume);

        // 索引+1，为下一次做准备
        currentClipIndex++;
    }
}