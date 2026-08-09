namespace Recruitment.API.DTOs
{
    public class SelectionRoundDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
    }
}
