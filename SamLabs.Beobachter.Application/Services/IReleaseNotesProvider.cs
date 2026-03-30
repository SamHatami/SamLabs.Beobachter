namespace SamLabs.Beobachter.Application.Services;

public interface IReleaseNotesProvider
{
    ReleaseNotesSnapshot GetCurrentReleaseNotes();
}
