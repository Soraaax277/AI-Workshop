public interface IDialogueSpeechBackend : System.IDisposable
{
    bool IsSupported { get; }
    void Speak(string text, int rate);
    void Stop();
}
