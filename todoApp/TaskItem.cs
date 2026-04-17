using System;
using System.Collections.Generic;

namespace todoApp
{
    public enum RecurrenceType
    {
        None,
        Daily,
        Weekly,
        Monthly
    }

    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProfileId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
        public DateTime Date { get; set; } = DateTime.Today;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Recurrence
        public RecurrenceType Recurrence { get; set; } = RecurrenceType.None;
        public DateTime? RecurrenceEndDate { get; set; } = null;

        // Tracks per-date state for recurring tasks:
        //   key   = "yyyy-MM-dd"
        //   value = true  → completed on that date
        //           false → deleted/skipped on that date
        public Dictionary<string, bool> RecurrenceOverrides { get; set; }
            = new Dictionary<string, bool>();
    }
}