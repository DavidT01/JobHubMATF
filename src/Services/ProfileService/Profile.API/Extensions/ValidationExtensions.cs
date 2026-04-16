using FluentValidation;

namespace Profile.API.Extensions
{
    public static class ValidationExtensions
    {
        public static IRuleBuilderOptions<T, string?> MustBeValidUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.Must(url =>
            {
                if (string.IsNullOrWhiteSpace(url))
                    return true;

                return Uri.TryCreate(url, UriKind.Absolute, out Uri? outUri)
                    && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
            }).WithMessage("'{PropertyName}' must be a valid HTTP or HTTPS URL.");
        }
    }
}
