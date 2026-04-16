using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace todoApp
{
    public class TaskService
    {
        private static readonly string SaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodoCalendarApp"
        );

        private static readonly string SaveFile = Path.Combine(SaveFolder, "tasks.json");

        public List<TaskItem> LoadTasks()
        {
            if (!File.Exists(SaveFile))
                return new List<TaskItem>();

            string json = File.ReadAllText(SaveFile);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }

        public void SaveTasks(List<TaskItem> tasks)
        {
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);

            string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(SaveFile, json);
        }

        public List<TaskItem> GetTasksForDate(string profileId, DateTime date)
        {
            var tasks = LoadTasks();
            return tasks
                .Where(t => t.ProfileId == profileId && t.Date.Date == date.Date)
                .OrderBy(t => t.CreatedAt)
                .ToList();
        }

        public List<DateTime> GetDatesWithTasks(string profileId, int year, int month)
        {
            var tasks = LoadTasks();
            return tasks
                .Where(t => t.ProfileId == profileId
                    && t.Date.Year == year
                    && t.Date.Month == month)
                .Select(t => t.Date.Date)
                .Distinct()
                .ToList();
        }

        public void AddTask(TaskItem task)
        {
            var tasks = LoadTasks();
            tasks.Add(task);
            SaveTasks(tasks);
        }

        public void UpdateTask(TaskItem updatedTask)
        {
            var tasks = LoadTasks();
            var index = tasks.FindIndex(t => t.Id == updatedTask.Id);
            if (index >= 0)
            {
                tasks[index] = updatedTask;
                SaveTasks(tasks);
            }
        }

        public void DeleteTask(string taskId)
        {
            var tasks = LoadTasks();
            tasks.RemoveAll(t => t.Id == taskId);
            SaveTasks(tasks);
        }
    }
}