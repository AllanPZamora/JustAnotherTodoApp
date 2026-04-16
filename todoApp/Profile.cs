using System;
using System.Collections.Generic;
using System.Text;

namespace todoApp
{
    public class Profile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#E50914";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
