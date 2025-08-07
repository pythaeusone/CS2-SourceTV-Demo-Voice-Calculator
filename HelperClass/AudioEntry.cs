namespace CS2SourceTVDemoVoiceCalc.HelperClass
{
    /// <summary>
    /// Represents a single audio file's metadata such as round, time, duration, and file path.
    /// </summary>
    public class AudioEntry
    {
        public int Round { get; set; }              // The round number associated with this audio
        public TimeSpan Time { get; set; }           // Time within the round the audio starts
        public double DurationSeconds { get; set; }  // Duration of the audio in seconds
        public string? FilePath { get; set; }        // Full path to the .wav file
    }
}
