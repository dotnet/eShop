

namespace IdentityServerHost.Quickstart.UI;
 /// <summary>
 /// Base controller responsible for ConsentController, DeviceController, 
 /// DiagnosticsController, GrantsController for avoidance of duplicated code
 /// </summary>
    [SecurityHeaders]
    [Authorize]
    public class BaseApiController : Controller
    {
        
    }
