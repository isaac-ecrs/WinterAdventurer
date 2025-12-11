namespace WinterAdventurer.Library
{
    /// <summary>
    /// Defines standard worksheet names and column header patterns for Excel parsing.
    /// These constants serve as defaults when event-specific schemas are not used or as fallback values.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Standard worksheet name for attendee registration data containing participant information.
        /// </summary>
        public const string SHEET_CLASS_SELECTION = "ClassSelection";

        /// <summary>
        /// Column header pattern for unique registration identifier linking attendees to their workshop selections.
        /// </summary>
        public const string HEADER_SELECTION_ID = "ClassSelection_Id";

        /// <summary>
        /// Column header pattern for participant first name.
        /// </summary>
        public const string HEADER_FIRST_NAME = "Name_First";

        /// <summary>
        /// Column header pattern for participant last name.
        /// </summary>
        public const string HEADER_LAST_NAME = "Name_Last";

        /// <summary>
        /// Column header pattern for participant email address.
        /// </summary>
        public const string HEADER_EMAIL = "Email";

        /// <summary>
        /// Column header pattern for participant age.
        /// </summary>
        public const string HEADER_AGE = "Age";

        /// <summary>
        /// Column header pattern for choice number indicating preference rank (1=first choice, 2+=backup choices).
        /// </summary>
        public const string HEADER_CHOICE_NUMBER = "ChoiceNumber";

        /// <summary>
        /// Column name pattern for workshop columns to be detected dynamically during Excel parsing.
        /// </summary>
        public const string PATTERN_CLASS = "Class";

        /// <summary>
        /// Column name pattern for day-specific workshop columns to be detected dynamically during Excel parsing.
        /// </summary>
        public const string PATTERN_DAY = "Day";
    }
}
