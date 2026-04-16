using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace todoApp
{
    public class ProfileService
    {
        private static readonly string SaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodoCalendarApp"
        );

        private static readonly string SaveFile = Path.Combine(SaveFolder, "profiles.json");

        private static readonly List<string> ProfileColors = new List<string>
        {
            "#E50914", "#0071EB", "#E87C1E", "#A020F0",
            "#2ECC71", "#E91E8C", "#00BCD4", "#FF5722"
        };

        public List<Profile> LoadProfiles()
        {
            if (!File.Exists(SaveFile))
                return new List<Profile>();

            string json = File.ReadAllText(SaveFile);
            return JsonSerializer.Deserialize<List<Profile>>(json) ?? new List<Profile>();
        }

        public void SaveProfiles(List<Profile> profiles)
        {
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);

            string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SaveFile, json);
        }

        public Profile CreateProfile(string name)
        {
            var random = new Random();
            string color = ProfileColors[random.Next(ProfileColors.Count)];

            return new Profile
            {
                Name = name,
                Color = color
            };
        }
    }
}
