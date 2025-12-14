using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonFootAudios : MonoBehaviour
{
    [Header("走路的音频资源")]
    [Tooltip("这里拖入走路的音频片段，可以放多个，随机播放")]
    public AudioClip[] footstepClips;

    [Header("设置")]
    [Tooltip("走路时的声音大小")]
    [Range(0, 1)] public float volume = 0.5f;

    [Tooltip("步频间隔（米）：每移动多少米播放一次声音")]
    public float stepDistance = 1.8f;

    private CharacterController _controller;
    private AudioSource _audioSource;
    private float _distanceTravelled;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();

        // 确保音频源设置正确
        _audioSource.spatialBlend = 1.0f; // 3D 声音
        _audioSource.playOnAwake = false;
    }

    void Update()
    {
        CheckFootsteps();
    }

    private void CheckFootsteps()
    {
        // 1. 如果角色没有在地面上，或者没有移动，直接返回
        if (!_controller.isGrounded || _controller.velocity.sqrMagnitude < 0.1f)
        {
            return;
        }

        // 2. 累加移动距离
        // 使用 magnitude 获取当前帧移动的距离
        // Time.deltaTime 已经包含在 velocity 计算中了，这里我们直接取速度*时间=距离
        _distanceTravelled += _controller.velocity.magnitude * Time.deltaTime;

        // 3. 达到步频距离，播放声音
        if (_distanceTravelled >= stepDistance)
        {
            PlayFootstepSound();
            _distanceTravelled = 0f; // 重置距离
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepClips.Length == 0) return;

        // 随机选择一个片段
        int index = Random.Range(0, footstepClips.Length);

        // 稍微改变音调，让声音听起来不那么机械
        _audioSource.pitch = Random.Range(0.9f, 1.1f);
        _audioSource.PlayOneShot(footstepClips[index], volume);
    }
}
