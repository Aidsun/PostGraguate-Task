// 文件：FirstPersonFootAudios.cs
// 模块：玩家控制器 / 音频管理
// 说明：该脚本负责第一人称视角下的脚步声播放。它根据角色移动的距离和步长设置，
//      周期性地播放脚步声效，并随机调整音高以增加真实感。
// 特性：RequireComponent 强制依赖 CharacterController 和 AudioSource，
//      Range 属性限制音量范围，直接从 GameData 单例读取步长设置。

using UnityEngine;

// RequireComponent 特性：当此脚本添加到游戏对象时，Unity 会自动添加缺失的指定组件
// 这里强制要求必须有 CharacterController 和 AudioSource
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonFootAudios : MonoBehaviour
{
    // 脚步声剪辑数组，可在Inspector中配置多个脚步声音频，增加随机性
    public AudioClip[] footstepClips;

    // Range 特性：在Inspector中显示为滑块，限制音量值在0到1之间
    [Range(0, 1)] public float volume = 0.5f;

    // 步长距离（玩家走多少米触发一次脚步声）
    // 初始值为1.8f，但会在Update中从GameData动态更新
    private float stepDistance = 1.8f;

    // 私有引用：角色控制器组件
    private CharacterController _controller;
    // 私有引用：音频源组件
    private AudioSource _audioSource;
    // 记录玩家自上次脚步声后已移动的距离
    private float _distanceTravelled;

    void Start()
    {
        // 获取同一物体上的CharacterController组件
        _controller = GetComponent<CharacterController>();
        // 获取同一物体上的AudioSource组件
        _audioSource = GetComponent<AudioSource>();

        // 设置音频源属性：不在启动时自动播放
        _audioSource.playOnAwake = false;
        // spatialBlend = 1.0 表示完全3D空间化，声音会随距离衰减，适合脚步声
        _audioSource.spatialBlend = 1.0f;
    }

    void Update()
    {
        // 【直接读取 GameData】从GameData单例获取当前的步长设置，实现动态调整
        if (GameData.Instance != null) stepDistance = GameData.Instance.StepDistance;

        // 判断是否在地面上且移动速度足够大（速度的平方大于0.1，避免静止时微小抖动）
        if (_controller.isGrounded && _controller.velocity.sqrMagnitude > 0.1f)
        {
            // 累加当前帧移动的距离（速度大小乘以时间增量）
            _distanceTravelled += _controller.velocity.magnitude * Time.deltaTime;

            // 如果累计距离达到或超过步长，则播放脚步声
            if (stepDistance > 0 && _distanceTravelled >= stepDistance)
            {
                PlayFootstep();
                // 重置累计距离
                _distanceTravelled = 0f;
            }
        }
    }

    // 播放脚步声
    void PlayFootstep()
    {
        // 确保脚步声音频数组不为空
        if (footstepClips.Length > 0)
        {
            // 随机调整音高（0.9 ~ 1.1），使每次脚步声略有不同
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            // 从数组中随机选择一个脚步声剪辑播放，使用指定音量
            _audioSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], volume);
        }
    }
}