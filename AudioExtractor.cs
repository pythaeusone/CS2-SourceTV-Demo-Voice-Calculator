using Concentus;
using DemoFile;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FaceitDemoVoiceCalc
{
    /// <summary>
    /// Static class responsible for extracting audio segments from a CS2 demo file.
    /// Can be called from a GUI application with progress reporting and asynchronous execution.
    /// </summary>
    internal static class AudioExtractor
    {
        /// <summary>
        /// Extracts voice segments from the specified .dem file.
        /// Reports progress and completes asynchronously.
        /// </summary>
        /// <param name="demoFilePath">Full path to the demo file (.dem)</param>
        /// <param name="progress">Optional progress reporter (0.0 - 1.0)</param>
        /// <returns>True if extraction succeeds, false otherwise</returns>
        public static async Task<bool> ExtractAsync(string demoFilePath, IProgress<float>? progress = null)
        {
            try
            {
                if (!File.Exists(demoFilePath)) return false;

                var demo = new CsDemoParser();
                var demoFileReader = new DemoFileReader<CsDemoParser>(demo, new MemoryStream(File.ReadAllBytes(demoFilePath)));

                var stopwatch = Stopwatch.StartNew();

                // Dictionary to store voice data grouped by SteamID
                Dictionary<ulong, List<(CMsgVoiceAudio audio, int tick, int round)>> voiceDataPerSteamId = new();
                int currentRound = 0;
                int totalVoicePackets = 0;
                int processedVoicePackets = 0;

                // Track round changes in demo
                demo.Source1GameEvents.RoundStart += _ => currentRound++;

                // Collect all voice packets
                demo.PacketEvents.SvcVoiceData += e =>
                {
                    if (e.Audio == null) return;
                    if (e.Audio.Format != VoiceDataFormat_t.VoicedataFormatOpus)
                        throw new ArgumentException($"Invalid voice format: {e.Audio.Format}");

                    if (!voiceDataPerSteamId.TryGetValue(e.Xuid, out var voiceList))
                    {
                        voiceList = new();
                        voiceDataPerSteamId[e.Xuid] = voiceList;
                    }

                    int tick = demo.CurrentDemoTick.Value;
                    voiceList.Add((e.Audio, tick, currentRound));
                    totalVoicePackets++;
                };

                await demoFileReader.ReadAllAsync(CancellationToken.None);

                const int sampleRate = 48000;
                const int numChannels = 1;

                // Create main output folder based on demo filename
                string mainFolderName = Path.GetFileNameWithoutExtension(demoFilePath);
                string mainOutputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Audio", mainFolderName);
                Directory.CreateDirectory(mainOutputDir);

                foreach (var (steamId, segments) in voiceDataPerSteamId)
                {
                    var decoder = OpusCodecFactory.CreateDecoder(sampleRate, numChannels);
                    var player = demo.GetPlayerBySteamId(steamId);
                    var playerName = SanitizeFileName(player?.PlayerName ?? steamId.ToString());
                    var outputDir = Path.Combine(mainOutputDir, steamId.ToString());
                    Directory.CreateDirectory(outputDir);

                    int segmentCount = 0;
                    List<(CMsgVoiceAudio audio, int tick, int round)> currentSegment = new();
                    int lastTick = -10000;

                    // Process and save audio segment
                    void ProcessSegment()
                    {
                        if (currentSegment.Count == 0) return;

                        List<short> allSamples = new List<short>();
                        int startTick = currentSegment[0].tick;

                        foreach (var (audioMsg, tick, round) in currentSegment)
                        {
                            if (audioMsg.VoiceData.Length == 0) continue;

                            byte[] encodedData = audioMsg.VoiceData.ToArray();
                            float[] pcmFloatBuffer = new float[960 * 6];
                            short[] pcmShortBuffer = new short[960 * 6];

                            int decodedSamples;
                            try
                            {
                                decodedSamples = decoder.Decode(
                                    encodedData.AsSpan(),
                                    pcmFloatBuffer.AsSpan(),
                                    pcmFloatBuffer.Length,
                                    false
                                );

                                for (int i = 0; i < decodedSamples; i++)
                                {
                                    pcmShortBuffer[i] = (short)(Math.Clamp(pcmFloatBuffer[i], -1.0f, 1.0f) * short.MaxValue);
                                }
                            }
                            catch
                            {
                                continue;
                            }

                            if (decodedSamples <= 0) continue;
                            allSamples.AddRange(pcmShortBuffer.Take(decodedSamples));

                            processedVoicePackets++;
                            progress?.Report((float)processedVoicePackets / totalVoicePackets);
                        }

                        if (allSamples.Count > 0)
                        {
                            float startSeconds = startTick / (float)CsDemoParser.TickRate;
                            string filename = Path.Combine(outputDir, $"round_{currentSegment[0].round}_t_{(int)startSeconds}s.wav");
                            WriteWavFile(filename, sampleRate, numChannels, allSamples.ToArray());
                            segmentCount++;
                        }

                        currentSegment.Clear();
                    }

                    // Group packets into segments based on silence duration
                    foreach (var segment in segments.OrderBy(s => s.tick))
                    {
                        if (segment.tick - lastTick > (int)(2.0 * CsDemoParser.TickRate))
                        {
                            ProcessSegment();
                        }

                        currentSegment.Add(segment);
                        lastTick = segment.tick;
                    }

                    // Process final segment
                    ProcessSegment();
                }

                progress?.Report(1.0f);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AudioExtractor] Exception: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Sanitizes a string to be safe for use as a filename
        /// </summary>
        /// <param name="name">Input string</param>
        /// <returns>Sanitized filename string</returns>
        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        /// <summary>
        /// Writes a 16-bit PCM WAV file from a short array
        /// </summary>
        /// <param name="filePath">Path to output .wav file</param>
        /// <param name="sampleRate">Audio sample rate</param>
        /// <param name="numChannels">Number of audio channels</param>
        /// <param name="samplesInt16">PCM samples as short array</param>
        private static void WriteWavFile(string filePath, int sampleRate, int numChannels, ReadOnlySpan<short> samplesInt16)
        {
            int numSamples = samplesInt16.Length;
            int sampleSize = sizeof(short);
            WriteWavFile(filePath, numSamples, sampleRate, numChannels, sampleSize, MemoryMarshal.AsBytes(samplesInt16));
        }

        /// <summary>
        /// Writes a WAV file header and audio data to disk
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="numSamples">Total number of audio samples</param>
        /// <param name="sampleRate">Sample rate in Hz</param>
        /// <param name="numChannels">Number of audio channels</param>
        /// <param name="sampleSize">Size in bytes of a single sample</param>
        /// <param name="audioData">Raw PCM audio data</param>
        private static void WriteWavFile(string filePath, int numSamples, int sampleRate, int numChannels, int sampleSize, ReadOnlySpan<byte> audioData)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Write RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + numSamples * sampleSize * numChannels);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // Write fmt subchunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)numChannels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * numChannels * sampleSize);
            writer.Write((short)(numChannels * sampleSize));
            writer.Write((short)(8 * sampleSize));

            // Write data subchunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(numSamples * numChannels * sampleSize);
            writer.Write(audioData);

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, (int)stream.Length, FileOptions.None);
            stream.WriteTo(fileStream);
        }
    }
}
