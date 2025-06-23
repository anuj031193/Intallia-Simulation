using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSimulation.Models

{
    public class CellData
    {
        public string TaskId { get; set; }
        public string Value { get; set; }
        public string Formula { get; set; }
        public List<int> BackgroundColor { get; set; }
        public List<int> FontColor { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public string FontName { get; set; }
        public double FontSize { get; set; }

    }

}
