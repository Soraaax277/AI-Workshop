public static class DialogueSpeechBackendFactory
{
    public static IDialogueSpeechBackend Create()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        var windowsBackend = new WindowsSpeechBackend();
        if (windowsBackend.IsSupported)
            return windowsBackend;

        windowsBackend.Dispose();
        UnityEngine.Debug.Log("DialogueSpeechBackendFactory: Falling back to PowerShell speech.");
        return new PowerShellSpeechBackend();
#else
        return new NullSpeechBackend();
#endif
    }
}
