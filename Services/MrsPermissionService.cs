
using System.Security.Claims;

namespace inventory_api.Services
{
    public class MrsPermissionService
    {
        private readonly UserService _userService;

        public MrsPermissionService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<bool> HasPermissionAsync(
            ClaimsPrincipal principal,
            string accessCode)
        {
            if (principal.Identity?.IsAuthenticated != true)
                return false;

            var userId = principal.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var accessCodes =
                await _userService.GetUserAccessCodesAsync(userId);

            return accessCodes.Contains(
                accessCode,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
