#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Diagnostics;
using System.Text;

public sealed class PowerShellSpeechBackend : IDialogueSpeechBackend
{
    Process activeProcess;

    public bool IsSupported => true;

    public void Speak(string text, int rate)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        Stop();

        string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(BuildSpeakScript(text, rate)));
        activeProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
            CreateNoWindow = true,
            UseShellExecute = false,
        });

        if (activeProcess == null)
            UnityEngine.Debug.LogWarning("PowerShellSpeechBackend: Failed to start speech process.");
    }

    public void Stop()
    {
        if (activeProcess == null)
            return;

        try
        {
            if (!activeProcess.HasExited)
                activeProcess.Kill();
        }
        catch (Exception)
        {
        }
        finally
        {
            activeProcess.Dispose();
            activeProcess = null;
        }
    }

    public void Dispose()
    {
        Stop();
    }

    static string BuildSpeakScript(string text, int rate)
    {
        var builder = new StringBuilder();
        builder.Append("Add-Type -AssemblyName System.Speech; ");
        builder.Append("$s = New-Object System.Speech.Synthesis.SpeechSynthesizer; ");
        builder.Append("$s.SetOutputToDefaultAudioDevice(); ");
        builder.Append("$s.Rate = ").Append(rate).Append("; ");
        builder.Append("$s.Volume = 100; ");
        builder.Append("$s.Speak(").Append(ToPowerShellString(text)).Append(");");
        return builder.ToString();
    }

    static string ToPowerShellString(string value)
    {
        return "'" + value.Replace("'", "''") + "'";
    }
}
#endif
