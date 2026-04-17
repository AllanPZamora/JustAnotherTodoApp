using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace todoApp
{
    public class TaskService
    {
        private static readonly string SaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodoCalendarApp"
        );

        private static readonly string SaveFile = Path.Combine(SaveFolder, "tasks.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public List<TaskItem> LoadTasks()
        {
            if (!File.Exists(SaveFile))
                return new List<TaskItem>();

            string json = File.ReadAllText(SaveFile);
            return JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOptions)
                   ?? new List<TaskItem>();
        }

        public void SaveTasks(List<TaskItem> tasks)
        {
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);

            File.WriteAllText(SaveFile, JsonSerializer.Serialize(tasks, JsonOptions));
        }

        // ─── Query ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns all tasks (one-time + recurring occurrences) visible on a given date.
        /// For recurring tasks a lightweight "virtual" copy is returned so the UI can
        /// display the correct per-date completion state.
        /// </summary>
        public List<TaskItem> GetTasksForDate(string profileId, DateTime date)
        {
            var tasks = LoadTasks();
            var result = new List<TaskItem>();

            foreach (var task in tasks.Where(t => t.ProfileId == profileId))
            {
                if (task.Recurrence == RecurrenceType.None)
                {
                    if (task.Date.Date == date.Date)
                        result.Add(task);
                }
                else
                {
                    // Recurring: check if this date is an occurrence
                    if (!IsOccurrence(task, date.Date))
                        continue;

                    string key = date.Date.ToString("yyyy-MM-dd");

                    // If explicitly deleted on this date, skip
                    if (task.RecurrenceOverrides.TryGetValue(key, out bool val) && val == false)
                        continue;

                    // Build a virtual snapshot for this date
                    var snapshot = new TaskItem
                    {
                        Id = task.Id,
                        ProfileId = task.ProfileId,
                        Title = task.Title,
                        Notes = task.Notes,
                        Date = date.Date,
                        CreatedAt = task.CreatedAt,
                        Recurrence = task.Recurrence,
                        RecurrenceEndDate = task.RecurrenceEndDate,
                        RecurrenceOverrides = task.RecurrenceOverrides,
                        IsCompleted = task.RecurrenceOverrides.TryGetValue(key, out bool done) && done
                    };
                    result.Add(snapshot);
                }
            }

            return result.OrderBy(t => t.CreatedAt).ToList();
        }

        /// <summary>
        /// Returns all calendar dates in a month that have at least one visible task.
        /// </summary>
        public List<DateTime> GetDatesWithTasks(string profileId, int year, int month)
        {
            var tasks = LoadTasks().Where(t => t.ProfileId == profileId).ToList();
            var dates = new HashSet<DateTime>();

            var firstDay = new DateTime(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            foreach (var task in tasks)
            {
                if (task.Recurrence == RecurrenceType.None)
                {
                    if (task.Date.Year == year && task.Date.Month == month)
                        dates.Add(task.Date.Date);
                }
                else
                {
                    // Walk each day of the month to find occurrences
                    for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
                    {
                        if (!IsOccurrence(task, d)) continue;
                        string key = d.ToString("yyyy-MM-dd");
                        // Only add if not explicitly deleted
                        if (!task.RecurrenceOverrides.TryGetValue(key, out bool v) || v != false)
                            dates.Add(d);
                    }
                }
            }

            return dates.ToList();
        }

        // ─── Mutations ──────────────────────────────────────────────────

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

        /// <summary>Deletes a non-recurring task entirely.</summary>
        public void DeleteTask(string taskId)
        {
            var tasks = LoadTasks();
            tasks.RemoveAll(t => t.Id == taskId);
            SaveTasks(tasks);
        }

        /// <summary>
        /// For recurring tasks: marks a specific date as deleted (false override).
        /// </summary>
        public void DeleteRecurringOccurrence(string taskId, DateTime date)
        {
            var tasks = LoadTasks();
            var task = tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;
            task.RecurrenceOverrides[date.ToString("yyyy-MM-dd")] = false;
            SaveTasks(tasks);
        }

        /// <summary>
        /// For recurring tasks: deletes all occurrences from a date onward by
        /// setting RecurrenceEndDate to the day before.
        /// </summary>
        public void DeleteRecurringFromDate(string taskId, DateTime fromDate)
        {
            var tasks = LoadTasks();
            var task = tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;

            var newEnd = fromDate.Date.AddDays(-1);
            if (newEnd < task.Date.Date)
            {
                // Deleting from the very first occurrence → remove entirely
                tasks.RemoveAll(t => t.Id == taskId);
            }
            else
            {
                task.RecurrenceEndDate = newEnd;
            }
            SaveTasks(tasks);
        }

        /// <summary>Marks a specific occurrence of a recurring task as complete/incomplete.</summary>
        public void SetRecurringOccurrenceCompleted(string taskId, DateTime date, bool completed)
        {
            var tasks = LoadTasks();
            var task = tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null) return;
            task.RecurrenceOverrides[date.ToString("yyyy-MM-dd")] = completed;
            SaveTasks(tasks);
        }

        // ─── Helper ─────────────────────────────────────────────────────

        private static bool IsOccurrence(TaskItem task, DateTime date)
        {
            if (date < task.Date.Date) return false;
            if (task.RecurrenceEndDate.HasValue && date > task.RecurrenceEndDate.Value.Date) return false;

            return task.Recurrence switch
            {
                RecurrenceType.Daily   => true,
                RecurrenceType.Weekly  => date.DayOfWeek == task.Date.DayOfWeek,
                RecurrenceType.Monthly => date.Day == task.Date.Day,
                _                      => false
            };
        }
    }
}