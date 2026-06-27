#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using UnityEngine;

public sealed class WindowsSpeechBackend : IDialogueSpeechBackend, IDisposable
{
    const string SpeechAssemblyName = "System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";

    enum SpeechCommandType
    {
        Speak,
        Stop,
        Shutdown
    }

    readonly struct SpeechCommand
    {
        public SpeechCommandType Type { get; }
        public string Text { get; }
        public int Rate { get; }

        public SpeechCommand(SpeechCommandType type, string text = null, int rate = 0)
        {
            Type = type;
            Text = text;
            Rate = rate;
        }
    }

    readonly BlockingCollection<SpeechCommand> commandQueue = new BlockingCollection<SpeechCommand>();
    readonly ManualResetEventSlim readyEvent = new ManualResetEventSlim(false);
    readonly Thread speechThread;

    volatile bool isReady;

    public bool IsSupported => isReady;

    public WindowsSpeechBackend()
    {
        speechThread = new Thread(RunSpeechThread)
        {
            IsBackground = true,
            Name = "DialogueSpeechThread"
        };
        speechThread.SetApartmentState(ApartmentState.STA);
        speechThread.Start();

        if (!readyEvent.Wait(TimeSpan.FromSeconds(5f)))
            Debug.LogWarning("WindowsSpeechBackend: Timed out waiting for text-to-speech to initialize.");
    }

    public void Speak(string text, int rate)
    {
        if (!isReady || string.IsNullOrWhiteSpace(text))
            return;

        commandQueue.Add(new SpeechCommand(SpeechCommandType.Speak, text, rate));
    }

    public void Stop()
    {
        if (!isReady)
            return;

        commandQueue.Add(new SpeechCommand(SpeechCommandType.Stop));
    }

    public void Dispose()
    {
        if (speechThread == null || !speechThread.IsAlive)
            return;

        commandQueue.Add(new SpeechCommand(SpeechCommandType.Shutdown));
        speechThread.Join(1000);
        commandQueue.Dispose();
        readyEvent.Dispose();
    }

    void RunSpeechThread()
    {
        object synthesizer = null;
        MethodInfo speakAsyncMethod = null;
        MethodInfo speakAsyncCancelAllMethod = null;
        PropertyInfo rateProperty = null;

        try
        {
            Assembly speechAssembly = LoadSpeechAssembly();
            if (speechAssembly == null)
            {
                Debug.LogWarning("WindowsSpeechBackend: Could not load System.Speech. Text-to-speech is unavailable.");
                return;
            }

            Type synthesizerType = speechAssembly.GetType("System.Speech.Synthesis.SpeechSynthesizer");
            if (synthesizerType == null)
            {
                Debug.LogWarning("WindowsSpeechBackend: SpeechSynthesizer type was not found.");
                return;
            }

            synthesizer = Activator.CreateInstance(synthesizerType);
            synthesizerType.GetMethod("SetOutputToDefaultAudioDevice")?.Invoke(synthesizer, null);

            rateProperty = synthesizerType.GetProperty("Rate");
            synthesizerType.GetProperty("Volume")?.SetValue(synthesizer, 100);

            speakAsyncMethod = synthesizerType.GetMethod("SpeakAsync", new[] { typeof(string) });
            speakAsyncCancelAllMethod = synthesizerType.GetMethod("SpeakAsyncCancelAll", Type.EmptyTypes);

            if (speakAsyncMethod == null || speakAsyncCancelAllMethod == null)
            {
                Debug.LogWarning("WindowsSpeechBackend: Required speech methods were not found.");
                return;
            }

            isReady = true;
            readyEvent.Set();
            Debug.Log("WindowsSpeechBackend: Text-to-speech initialized.");

            foreach (SpeechCommand command in commandQueue.GetConsumingEnumerable())
            {
                switch (command.Type)
                {
                    case SpeechCommandType.Speak:
                        rateProperty?.SetValue(synthesizer, command.Rate);
                        speakAsyncCancelAllMethod.Invoke(synthesizer, null);
                        speakAsyncMethod.Invoke(synthesizer, new object[] { command.Text });
                        break;

                    case SpeechCommandType.Stop:
                        speakAsyncCancelAllMethod.Invoke(synthesizer, null);
                        break;

                    case SpeechCommandType.Shutdown:
                        speakAsyncCancelAllMethod.Invoke(synthesizer, null);
                        if (synthesizer is IDisposable disposable)
                            disposable.Dispose();
                        return;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"WindowsSpeechBackend: Failed to initialize text-to-speech. {exception.Message}");
        }
        finally
        {
            isReady = false;
            readyEvent.Set();
        }
    }

    static Assembly LoadSpeechAssembly()
    {
        try
        {
            return Assembly.Load(SpeechAssemblyName);
        }
        catch (Exception)
        {
        }

        foreach (string runtimePath in GetRuntimeAssemblyPaths())
        {
            if (!File.Exists(runtimePath))
                continue;

            try
            {
                return Assembly.LoadFrom(runtimePath);
            }
            catch (Exception)
            {
            }
        }

        return null;
    }

    static string[] GetRuntimeAssemblyPaths()
    {
        string windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return new[]
        {
            Path.Combine(windowsFolder, "Microsoft.NET", "Framework64", "v4.0.30319", "System.Speech.dll"),
            Path.Combine(windowsFolder, "Microsoft.NET", "Framework", "v4.0.30319", "System.Speech.dll")
        };
    }
}
#endif
