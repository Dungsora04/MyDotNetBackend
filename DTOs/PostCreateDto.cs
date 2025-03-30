using System.ComponentModel.DataAnnotations;

namespace DotNetBackend.Dtos
{
    public class PostCreateDto
{
    [Required(ErrorMessage = "Text is required.")]
    [MaxLength(500, ErrorMessage = "Text must be less than or equal to 500 characters.")]
    public string Text { get; set; }

    public string? Img { get; set; }
}
}