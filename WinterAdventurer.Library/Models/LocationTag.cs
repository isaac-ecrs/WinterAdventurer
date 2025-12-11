// <copyright file="LocationTag.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

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
