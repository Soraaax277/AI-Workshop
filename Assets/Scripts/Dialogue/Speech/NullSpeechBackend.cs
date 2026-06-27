public sealed class NullSpeechBackend : IDialogueSpeechBackend
{
    public bool IsSupported => false;

    public void Speak(string text, int rate)
    {
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
}
