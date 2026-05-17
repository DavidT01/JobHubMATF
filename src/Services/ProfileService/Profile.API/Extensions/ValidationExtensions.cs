using FluentValidation;

namespace Profile.API.Extensions
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string?> ValidUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.Must(url =>
            {
                if (string.IsNullOrWhiteSpace(url))
                    return true;

                return Uri.TryCreate(url, UriKind.Absolute, out Uri? outUri)
                    && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
            }).WithMessage("'{PropertyName}' must be a valid HTTP or HTTPS URL.");
        }

        public static IRuleBuilderOptions<T, IFormFile> ValidPdf<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
        {
            return ruleBuilder.Must(file =>
            {
                if (file == null)
                    return false;

                var hasPdfContentType = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);
                var hasPdfExtension = string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase);

                return hasPdfContentType && hasPdfExtension;
            }).WithMessage("'{PropertyName}' must be a valid PDF file ('.pdf').");
        }

        public static IRuleBuilderOptions<T, IFormFile> ValidImage<T>(this IRuleBuilder<T, IFormFile> ruleBuilder)
        {
            return ruleBuilder.Must(file =>
            {
                if (file == null)
                    return false;

                var contentType = file.ContentType?.ToLower();
                var extension = Path.GetExtension(file.FileName)?.ToLower();

                var hasValidContentType = contentType == "image/png" || contentType == "image/jpg" || contentType == "image/jpeg";
                var hasValidExtension = extension == ".png" || extension == ".jpg" || extension == ".jpeg";

                return hasValidContentType && hasValidExtension;
            }).WithMessage("'{PropertyName}' must be a valid image file ('.png', '.jpg' or '.jpeg').");
        }
    }
}
