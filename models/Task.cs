using System;

namespace JobSimulation.Models
{
    public class JobTask
    {
        public string TaskId { get; set; }
        public string SectionId { get; set; }
        public int Order { get; set; }
        public string Description { get; set; }
        public string CompanyId { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string ModifyBy { get; set; }
        public DateTime ModifyDate { get; set; }
       
        public object Details { get; set; } // Added Details property
    }

    public class ExcelTaskDetails
    {
        public string TaskDescription { get; set; }
        public string SheetName { get; set; }
        public string SelectTask { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ResultCellLocation { get; set; }
        public string Hint { get; set; }
        public string SkillName { get; set; }
        public string SkillScore { get; set; }
    }


}