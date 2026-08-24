namespace NearU_Backend_Revised.Enums
{
	public static class FoodCategory
	{
		public static readonly IReadOnlyList<string> AllowedCategories = new List<string>
		{
			"Restaurant",
            "Cafe",
            "Bakery",
            "Juice Bar",
            "Fast Food",
            "Other"
		};

		//default category
		public const string Default = "Other";


		//check if the category is valid
		public static bool IsValid(string? category)
		{
			if(string.IsNullOrWhiteSpace(category)) return false;
			return AllowedCategories.Contains(category);
		}
	}
}