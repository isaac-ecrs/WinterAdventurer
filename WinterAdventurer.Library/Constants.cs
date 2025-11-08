namespace WinterAdventurer.Library
{
    public static class Constants
    {
        // Standard worksheet names to look for
        public const string SHEET_CLASS_SELECTION = "ClassSelection";

        // Common column header patterns (case-insensitive matching)
        public const string HEADER_SELECTION_ID = "ClassSelection_Id";
        public const string HEADER_FIRST_NAME = "Name_First";
        public const string HEADER_LAST_NAME = "Name_Last";
        public const string HEADER_EMAIL = "Email";
        public const string HEADER_AGE = "Age";
        public const string HEADER_CHOICE_NUMBER = "ChoiceNumber";

        // Column name patterns for workshop columns (to be detected dynamically)
        public const string PATTERN_CLASS = "Class";
        public const string PATTERN_DAY = "Day";
    }
}