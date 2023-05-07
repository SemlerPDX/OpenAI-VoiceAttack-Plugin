using OpenAI_NET.service;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net;

namespace OpenAI_NET
{
    /// <summary>
    /// OpenAI_NET App for the OpenAI API VoiceAttack Plugin
    /// <br>Copyright (C) 2023 Aaron Semler</br>
    /// <br><see href="https://github.com/SemlerPDX">github.com/SemlerPDX</see></br>
    /// <br><see href="https://veterans-gaming.com/semlerpdx-avcs">veterans-gaming.com/semlerpdx-avcs</see></br>
    /// <para>
    /// A background console app listening to OpenAI VoiceAttack Plugin requests for OpenAI API Whisper and Dall-E:
    /// <br> -Can process audio via transcription or translation into (English) text using OpenAI Whisper.</br>
    /// <br> -Can generate or work with images using the OpenAI Dall-E API, returning a list of URL's.</br>
    /// </para>
    /// <para>
    /// This program is free software: you can redistribute it and/or modify<br />
    /// it under the terms of the GNU General Public License as published by<br />
    /// the Free Software Foundation, either version 3 of the License, or<br />
    /// (at your option) any later version.<br />
    /// <br />
    /// This program is distributed in the hope that it will be useful,<br />
    /// but WITHOUT ANY WARRANTY; without even the implied warranty of<br />
    /// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the<br />
    /// GNU General Public License for more details.<br />
    /// <br />
    /// You should have received a copy of the GNU General Public License<br />
    /// along with this program.  If not, see <see href="https://www.gnu.org/licenses/">gnu.org/licenses</see>.
    /// </para>
    /// </summary>
    internal class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;


        private static System.Timers.Timer? aTimer;

        static void Main()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);

            Logging.SetErrorLogPath();

            InitializeProcessTimer();

            while (true)
            {
                Run();
            }
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            if (aTimer != null)
            {
                aTimer.Stop();
                aTimer.Dispose();
            }
            if (Environment.ExitCode != 0)
                Logging.WriteToLogFile($"OpenAI_NET Application exited with code {Environment.ExitCode}");
        }

        /// <summary>
        /// This method initiates a 2 second timer to monitor if VoiceAttack is running.
        /// </summary>
        private static void InitializeProcessTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(2000D);

            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += CheckVoiceAttackProcess;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }

        /// <summary>
        /// This method ensures that the OpenAI_NET application cannot continue running if VoiceAttack is closed.<br />
        /// When Plugin Support is disabled in VoiceAttack Options, the OpenAI Plugin can no longer close this app, <br />
        /// this will do so the next time VoiceAttack closes.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The ElapsedEventArgs object that contains the event data.</param>
        private static void CheckVoiceAttackProcess(Object? source, ElapsedEventArgs e)
        {
            if (!Process.GetProcessesByName("VoiceAttack").Any())
            {
                if (aTimer != null && aTimer.Enabled)
                {
                    aTimer.Stop();
                    aTimer.Dispose();
                }
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// This main Run method listens for Whisper and Dall-E commands over the Named Pipe with OpenAI Plugin for VoiceAttack,
        /// and then executes those instructions in the <see cref="Audio"/> or <see cref="Image"/> class.
        /// </summary>
        private static async void Run()
        {
            while (true)
            {
                try
                {
                    string[] args = Piping.ListenForArgsOnNamedPipe();
                    if (args != null && args.Length != 0 && args.Length >= 1)
                    {
                        switch (args[0])
                        {
                            case "transcribe":
                                if (args.Length == 3) { await Audio.Transcribe(args); }
                                break;
                            case "translate":
                                if (args.Length == 3) { await Audio.Translate(args); }
                                break;
                            case "image.generate":
                                if (args.Length >= 3) { await Image.GenerateImage(args); }
                                break;
                            case "image.variation":
                                if (args.Length >= 3) { await Image.VariateImage(args); }
                                break;
                            case "image.variation.bytes":
                                if (args.Length >= 3) { await Image.VariateImage(args, true); }
                                break;
                            case "image.edit":
                                if (args.Length >= 4) { await Image.EditImage(args); }
                                break;
                            case "image.edit.bytes":
                                if (args.Length >= 4) { await Image.EditImage(args, true); }
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    try
                    {
                        try
                        {
                            // Log the error to file
                            Logging.WriteToLogFile(ex.Message);

                            // Send the error message back to the OpenAI VoiceAttack Plugin
                            if (!Piping.SendArgsToNamedPipe(new[] { ex.Message.ToString(), "error" }))
                            {
                                Console.WriteLine($"Failed to send previous error message through pipe: {ex.Message}");
                                throw new Exception($"Failed to send previous error message through pipe: {ex.Message}");
                            }
                        }
                        catch (ArgumentException aex)
                        {
                            Console.WriteLine($"OpenAI_NET Error: {aex.Message} Previous Error: {ex.Message}");
                            Logging.WriteToLogFile($"OpenAI_NET Error: {aex.Message} Previous Error: {ex.Message}");
                        }
                    }
                    catch
                    {
                        Console.WriteLine($"OpenAI_NET Error: Unable to send previous error message through pipe: {ex.Message}");
                        Logging.WriteToLogFile($"OpenAI_NET Error: Unable to send previous error message through pipe: {ex.Message}");
                    }
                }
                GC.Collect();
            }
        }

    }
}