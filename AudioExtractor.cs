using Concentus;
using DemoFile;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CS2VoiceSegmenter
{
    /// <summary>
    /// Static class for extracting audio segments from a CS2 demo file.
    /// </summary>
    public static class AudioExtractor
    {
        /// <summary>
        /// Extracts voice data from a demo file into WAV segments organized by player.
        /// </summary>
        /// <param name="demoPath">Full path to the .dem file.</param>
        /// <param name="progress">Optional progress reporter (0 to 1).</param>
        /// <returns>True if extraction succeeds, otherwise false.</returns>
        public static async Task<bool> ExtractAsync(string demoPath, IProgress<float>? progress = null)
        {
            try
            {
                var demo = new CsDemoParser();
                var demoFileReader = new DemoFileReader<CsDemoParser>(demo, new MemoryStream(File.ReadAllBytes(demoPath)));

                Dictionary<ulong, List<(CMsgVoiceAudio audio, int tick, int round)>> voiceDataPerSteamId = new();
                int currentRound = 0;

                demo.Source1GameEvents.RoundStart += _ => currentRound++;

                int totalPackets = 0;

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

                    totalPackets++;
                    if (totalPackets % 200 == 0)
                    {
                        // Report partial progress during parsing (max 40%)
                        progress?.Report(Math.Min(0.4f, totalPackets / 5000f * 0.4f));
                    }
                };

                await demoFileReader.ReadAllAsync(CancellationToken.None);

                // Start actual decoding phase (progress 40% to 100%)
                const int sampleRate = 48000;
                const int numChannels = 1;

                string demoName = Path.GetFileNameWithoutExtension(demoPath);
                string baseOutputDir = Path.Combine(AppContext.BaseDirectory, "Audio", demoName);

                int totalPlayers = voiceDataPerSteamId.Count;
                int playerIndex = 0;

                foreach (var (steamId, segments) in voiceDataPerSteamId)
                {
                    var decoder = OpusCodecFactory.CreateDecoder(sampleRate, numChannels);
                    var player = demo.GetPlayerBySteamId(steamId);
                    var playerName = SanitizeFileName(player?.PlayerName ?? steamId.ToString());
                    var outputDir = Path.Combine(baseOutputDir, playerName);
                    Directory.CreateDirectory(outputDir);

                    int lastTick = -10000;
                    List<(CMsgVoiceAudio audio, int tick, int round)> currentSegment = new();

                    void ProcessSegment()
                    {
                        if (currentSegment.Count == 0) return;

                        List<short> allSamples = new();
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
                        }

                        if (allSamples.Count > 0)
                        {
                            float startSeconds = startTick / (float)CsDemoParser.TickRate;
                            string filename = Path.Combine(outputDir, $"round_{currentSegment[0].round}_t_{(int)startSeconds}s.wav");
                            WriteWavFile(filename, sampleRate, numChannels, allSamples.ToArray());
                        }

                        currentSegment.Clear();
                    }

                    foreach (var segment in segments.OrderBy(s => s.tick))
                    {
                        if (segment.tick - lastTick > (int)(2.0 * CsDemoParser.TickRate))
                        {
                            ProcessSegment();
                        }

                        currentSegment.Add(segment);
                        lastTick = segment.tick;
                    }

                    ProcessSegment();

                    playerIndex++;
                    float decodingProgress = 0.4f + 0.6f * (playerIndex / (float)totalPlayers);
                    progress?.Report(decodingProgress);
                }

                progress?.Report(1.0f);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during extraction: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes invalid characters from file names.
        /// </summary>
        static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        /// <summary>
        /// Writes raw PCM data into a WAV file format.
        /// </summary>
        static void WriteWavFile(string filePath, int sampleRate, int numChannels, ReadOnlySpan<short> samplesInt16)
        {
            int numSamples = samplesInt16.Length;
            int sampleSize = sizeof(short);
            WriteWavFile(filePath, numSamples, sampleRate, numChannels, sampleSize, MemoryMarshal.AsBytes(samplesInt16));
        }

        /// <summary>
        /// Writes a WAV file to disk with the given PCM data.
        /// </summary>
        static void WriteWavFile(string filePath, int numSamples, int sampleRate, int numChannels, int sampleSize, ReadOnlySpan<byte> audioData)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + numSamples * sampleSize * numChannels);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)numChannels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * numChannels * sampleSize);
            writer.Write((short)(numChannels * sampleSize));
            writer.Write((short)(8 * sampleSize));

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(numSamples * numChannels * sampleSize);
            writer.Write(audioData);

            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, (int)stream.Length, FileOptions.None);
            stream.WriteTo(fileStream);
        }
    }
}
