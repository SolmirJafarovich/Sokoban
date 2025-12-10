using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Sokoban.App;

public sealed class ProfileRepository
{
    private readonly string filePath;

    public ProfileRepository(string baseDirectory)
    {
        if (baseDirectory == null)
            throw new ArgumentNullException(nameof(baseDirectory));

        var directory = Path.Combine(baseDirectory, "Profiles");
        Directory.CreateDirectory(directory);
        filePath = Path.Combine(directory, "profiles.json");
    }

    public IReadOnlyList<PlayerProfile> Load()
    {
        if (!File.Exists(filePath))
            return new List<PlayerProfile>();

        var json = File.ReadAllText(filePath);
        var dto = JsonSerializer.Deserialize<ProfilesDto>(json);
        if (dto == null || dto.Profiles == null)
            return new List<PlayerProfile>();

        var result = new List<PlayerProfile>();

        foreach (var profileDto in dto.Profiles)
        {
            var settings = new GameSettings(
                profileDto.MusicVolume,
                profileDto.EffectsVolume,
                profileDto.IsFullScreen);

            var completed = profileDto.CompletedLevels ?? new List<string>();

            var profile = new PlayerProfile(profileDto.Name, settings, completed);
            result.Add(profile);
        }

        return result;
    }

    public void Save(IReadOnlyList<PlayerProfile> profiles)
    {
        var dto = new ProfilesDto
        {
            Profiles = new List<ProfileDto>()
        };

        foreach (var profile in profiles)
        {
            var settings = profile.Settings;

            dto.Profiles.Add(new ProfileDto
            {
                Name = profile.Name,
                MusicVolume = settings.MusicVolume,
                EffectsVolume = settings.EffectsVolume,
                IsFullScreen = settings.IsFullScreen,
                CompletedLevels = new List<string>(profile.CompletedLevels)
            });
        }

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private sealed class ProfilesDto
    {
        public List<ProfileDto> Profiles { get; set; } = new();
    }

    private sealed class ProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public int MusicVolume { get; set; }
        public int EffectsVolume { get; set; }
        public bool IsFullScreen { get; set; }
        public List<string>? CompletedLevels { get; set; }
    }
}
