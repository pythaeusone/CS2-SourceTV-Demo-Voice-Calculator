/// <summary>
/// Holds basic player information captured at round start.
/// </summary>
namespace CS2SourceTVDemoVoiceCalc.HelperClass
{
    public class PlayerSnapshot
    {
        public int UserId { get; set; }             // User-ID + 1 = spec_player ID
        public string? PlayerName { get; set; }     // PlayerName = Name of the Player in this Game.
        public int TeamNumber { get; set; }         // TeamNumber = 2 = T-Side, 3 = CT-Side
        public string? TeamName { get; set; }       // TeamName = TeamClanName from Faceit team_xxxxx
        public ulong? PlayerSteamID { get; set; }   // Get the SteamID64 number

        public override string ToString()
        {
            return PlayerName ?? "Unknown Player";
        }
    }
}
