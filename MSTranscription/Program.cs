using System;
using Microsoft.CognitiveServices.Speech;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSTranscription
{
    class Program
    {
        static void Main(string[] args)
        {

            RecognitionWithAudioStreamAsync(args[0], args[1], args[2]).Wait();

        }

        public static async Task RecognitionWithAudioStreamAsync(string subscriptionKey, string subscriptionRegion, string file)
        {
            // <recognitionAudioStream>
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription(subscriptionKey, subscriptionRegion);

            var stopRecognition = new TaskCompletionSource<int>();

            // Create an audio stream from a wav file.
            // Replace with your own audio file name.
            using (var audioInput = Helper.OpenWavFile(file))
            {
                // Creates a speech recognizer using audio stream input.
                using (var recognizer = new SpeechRecognizer(config, audioInput))
                {
                    // Subscribes to events.
                    //recognizer.Recognizing += (s, e) =>
                    //{
                    //    Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                    //};
                    List<Sub> subs = new List<Sub>();
                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            // Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}");
                            TimeSpan end = e.Result.Duration.Add(offset);
                            Console.WriteLine($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            // Console.WriteLine($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}");
                            subs.AddRange(Sub.GetSubs(new Sub
                            {
                                Begin = offset,
                                End = end,
                                Caption = e.Result.Text
                            }));
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nSession stopped event.");
                        Console.WriteLine("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                        Sub.GenerateSrtFile(subs, file);
                        Sub.GenerateWebVTTFile(subs, file);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
            // </recognitionAudioStream>
        }
    }
}
