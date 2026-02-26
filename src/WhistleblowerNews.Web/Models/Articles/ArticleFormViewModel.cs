using System.ComponentModel.DataAnnotations;

namespace WhistleblowerNews.Web.Models.Articles;

public sealed class ArticleFormViewModel
{
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200, ErrorMessage = "Title must be 200 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Content is required.")]
    [StringLength(10000, ErrorMessage = "Content must be 10,000 characters or fewer.")]
    public string Content { get; set; } = string.Empty;
}
