using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinterAdventurer.Library.Models
{
    /// <summary>
    /// Represents a location tag for display in PDF schedules.
    /// Simplified version of database Tag entity for library layer use.
    /// </summary>
    public class LocationTag
    {
        public string Name { get; set; } = string.Empty;
    }
}
