using System;
using UnityEngine;

public class DialogueSpeechController : MonoBehaviour
{
    [SerializeField] bool speechEnabled = true;
    [SerializeField] int speechRate = 0;

    IDialogueSpeechBackend backend;

    public bool IsEnabled => speechEnabled;
    public bool IsSupported => backend != null && backend.IsSupported;

    void Awake()
    {
        backend = DialogueSpeechBackendFactory.Create();

        if (speechEnabled && IsSupported)
            Debug.Log($"DialogueSpeechController: Text-to-speech is ready ({backend.GetType().Name}).", this);
        else if (speechEnabled)
            Debug.LogWarning("DialogueSpeechController: Text-to-speech is not supported on this platform.", this);
    }

    void OnDestroy()
    {
        Stop();

        if (backend is IDisposable disposable)
            disposable.Dispose();
        backend = null;
    }

    public void Speak(string text)
    {
        if (!speechEnabled || backend == null || string.IsNullOrWhiteSpace(text))
            return;

        if (!backend.IsSupported)
            return;

        backend.Speak(text, speechRate);
    }

    public void Stop()
    {
        backend?.Stop();
    }
}
