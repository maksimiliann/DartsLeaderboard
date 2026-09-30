namespace DartsLeaderboard.Web.Speech;

public sealed class SpeechOptions
{
    public const string SectionName = "Speech";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = "http://127.0.0.1:8765";

    public string Speaker { get; set; } = "eugene";
}
