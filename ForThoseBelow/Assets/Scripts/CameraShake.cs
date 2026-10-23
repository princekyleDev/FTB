
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private float shakeInterval = 0.2f;
    [SerializeField] private float shakeForce = 0.3f;

    private Coroutine shakeRoutine;

    private void Awake()
    {
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void StartShake()
    {
        if (shakeRoutine != null)
            return;

        shakeRoutine = StartCoroutine(ShakeLoop());
    }

    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }
    }

    private IEnumerator ShakeLoop()
    {
        while (true)
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulseWithForce(shakeForce);

            yield return new WaitForSeconds(shakeInterval);
        }
    }

    private void OnDisable()
    {
        StopShake();
    }
}
