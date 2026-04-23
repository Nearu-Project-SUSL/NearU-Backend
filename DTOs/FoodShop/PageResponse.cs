namespace NearU_Backend_Revised.DTO.FoodShop
{
  public class PageResponse<T>
  {
    public IEnumerable<T> Items {get; set;} = new List<T>(); //real data for this page

    public int CurrentPage {get; set;} 

    public int PageSize {get; set;}

    public int TotalPages {get; set;}

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;
  }
}