

namespace JobSimulation.Models
{
    public class Section
    {
        public string SectionId { get; set; }
        public string Title { get; set; }
        public string SoftwareId { get; set; }
     
        public int Order { get; set; }
        public string StudentFile { get; set; } // To store the student's submitted file
        public string SimulationId { get;  set; }
    }
}