using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonFootAudios : MonoBehaviour
{
    public AudioClip[] footstepClips;
    [Range(0, 1)] public float volume = 0.5f;
    private float stepDistance = 1.8f;

    private CharacterController _controller;
    private AudioSource _audioSource;
    private float _distanceTravelled;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1.0f;
    }

    void Update()
    {
        // ¡¾Ö±½Ó¶ÁÈ¡ GameData¡¿
        if (GameData.Instance != null) stepDistance = GameData.Instance.StepDistance;

        if (_controller.isGrounded && _controller.velocity.sqrMagnitude > 0.1f)
        {
            _distanceTravelled += _controller.velocity.magnitude * Time.deltaTime;
            if (stepDistance > 0 && _distanceTravelled >= stepDistance)
            {
                PlayFootstep();
                _distanceTravelled = 0f;
            }
        }
    }

    void PlayFootstep()
    {
        if (footstepClips.Length > 0)
        {
            _audioSource.pitch = Random.Range(0.9f, 1.1f);
            _audioSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)], volume);
        }
    }
}