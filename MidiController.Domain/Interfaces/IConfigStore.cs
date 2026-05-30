using MidiController.Domain.Models;

namespace MidiController.Domain.Interfaces;

/// <summary>
/// Persistiert und lädt Mapping-Profile.
/// </summary>
public interface IConfigStore
{
    Task<IEnumerable<string>> ListProfileIdsAsync(CancellationToken ct = default);
    Task<Profile?> LoadProfileAsync(string profileId, CancellationToken ct = default);
    Task SaveProfileAsync(Profile profile, CancellationToken ct = default);
    Task DeleteProfileAsync(string profileId, CancellationToken ct = default);
}
