using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InventorySystemCloud.Domain.Entities
{
    public class Calendar
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Description { get; set; } = string.Empty;
        
        public List <Calendar> SubEvents { get; set; } = new List<Calendar>();
    }
}
