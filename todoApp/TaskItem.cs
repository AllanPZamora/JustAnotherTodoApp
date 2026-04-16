using System;

namespace todoApp
{
    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProfileId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
        public DateTime Date { get; set; } = DateTime.Today;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}