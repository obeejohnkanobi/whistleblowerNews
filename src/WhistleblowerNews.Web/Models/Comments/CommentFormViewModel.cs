using System.ComponentModel.DataAnnotations;

namespace WhistleblowerNews.Web.Models.Comments;

public sealed class CommentFormViewModel
{
    [Required(ErrorMessage = "Comment content is required.")]
    [StringLength(1000, ErrorMessage = "Comment must be 1000 characters or fewer.")]
    public string Content { get; set; } = string.Empty;
}
