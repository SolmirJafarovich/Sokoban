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
            if (string.IsNullOrWhiteSpace(profileDto.Name))
                continue;

            var profile = new PlayerProfile(profileDto.Name);

            if (profileDto.CompletedLevels != null)
            {
                foreach (var levelId in profileDto.CompletedLevels)
                {
                    if (!string.IsNullOrWhiteSpace(levelId))
                        profile.MarkLevelCompleted(levelId);
                }
            }

            if (profileDto.Levels != null)
            {
                foreach (var levelDto in profileDto.Levels)
                {
                    if (string.IsNullOrWhiteSpace(levelDto.LevelId))
                        continue;

                    profile.UpdateLevelStats(levelDto.LevelId, levelDto.BestTimeMs, levelDto.BestSteps);
                }
            }

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
            var profileDto = new ProfileDto
            {
                Name = profile.Name,
                CompletedLevels = new List<string>(profile.CompletedLevels),
                Levels = new List<LevelDto>()
            };

            foreach (var pair in profile.LevelStatsById)
            {
                var stats = pair.Value;
                profileDto.Levels.Add(new LevelDto
                {
                    LevelId = pair.Key,
                    BestTimeMs = stats.BestTimeMs,
                    BestSteps = stats.BestSteps
                });
            }

            dto.Profiles.Add(profileDto);
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
        public List<string>? CompletedLevels { get; set; }
        public List<LevelDto>? Levels { get; set; }
    }

    private sealed class LevelDto
    {
        public string LevelId { get; set; } = string.Empty;
        public int BestTimeMs { get; set; }
        public int BestSteps { get; set; }
    }
}
