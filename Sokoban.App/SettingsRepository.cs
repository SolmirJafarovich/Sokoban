using System;
using System.IO;
using System.Text.Json;

namespace Sokoban.App;

public sealed class SettingsRepository
{
    private readonly string filePath;

    public SettingsRepository(string baseDirectory)
    {
        if (baseDirectory == null)
            throw new ArgumentNullException(nameof(baseDirectory));

        var directory = Path.Combine(baseDirectory, "Config");
        Directory.CreateDirectory(directory);

        filePath = Path.Combine(directory, "settings.json");
    }

    public GameSettings Load()
    {
        if (!File.Exists(filePath))
            return new GameSettings();

        var json = File.ReadAllText(filePath);
        var dto = JsonSerializer.Deserialize<SettingsDto>(json);
        if (dto == null)
            return new GameSettings();

        return new GameSettings(dto.MusicVolume, dto.EffectsVolume, dto.IsFullScreen);
    }

    public void Save(GameSettings settings)
    {
        var dto = new SettingsDto
        {
            MusicVolume = settings.MusicVolume,
            EffectsVolume = settings.EffectsVolume,
            IsFullScreen = settings.IsFullScreen
        };

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    private sealed class SettingsDto
    {
        public int MusicVolume { get; set; }
        public int EffectsVolume { get; set; }
        public bool IsFullScreen { get; set; }
    }
}