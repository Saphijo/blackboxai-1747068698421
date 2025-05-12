using System.Collections.Generic;

namespace AEMSApp.Models
{
    public class CurriculumNode
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Unit, Module, Chapter, Topic, etc.
        public int? ParentId { get; set; }
        public int Level { get; set; }
        public string Description { get; set; } = string.Empty;
        public string StandardCode { get; set; } = string.Empty;
        public List<CurriculumNode> Children { get; set; } = new List<CurriculumNode>();
    }
}
