using Microsoft.AspNetCore.Http;

namespace RealEstateApi.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId
        {
            get
            {
                return _httpContextAccessor.HttpContext?
                    .User?
                    .FindFirst("id")?
                    .Value;
            }
        }
    }
}
