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

        // Change from object to a base class or interface
        public TaskDetailsBase Details { get; set; }
    }

    public abstract class TaskDetailsBase
    {
        public string TaskDescription { get; set; }
        public string Hint { get; set; }
        public string SkillName { get; set; }
        public string SkillScore { get; set; }
    }

    public class ExcelTaskDetails : TaskDetailsBase
    {
        public string SheetName { get; set; }
        public string SelectTask { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string ResultCellLocation { get; set; }
    }

    public class WordTaskDetails : TaskDetailsBase
    {
        public string TaskLocation { get; set; }
    }

    public class PowerPointTaskDetails : TaskDetailsBase
    {
       
        public string SlideName { get; set; }
        public string ObjectName { get; set; }
        public int SlideNo { get; set; }
        //public string Content { get; set; }
    }

    public class PowerBiDetails : TaskDetailsBase
    {
       
        public string SelectTask { get; set; }
        public string Hint { get; set; }
     
        //public string CreateBy { get; set; }
        //public DateTime CreateDate { get; set; }
        //public string ModifyBy { get; set; }
        //public DateTime ModifyDate { get; set; }
    }

}