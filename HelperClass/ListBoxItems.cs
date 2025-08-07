namespace CS2SourceTVDemoVoiceCalc.HelperClass
{
    /// <summary>
    /// Internal class for mapping display name to Steam ID in the ListBox.
    /// </summary>
    public class ListBoxItems
    {
        public string? DisplayName { get; set; }
        public string? SteamId { get; set; }
        public override string ToString() => DisplayName ?? "Unknown";
    }
}
