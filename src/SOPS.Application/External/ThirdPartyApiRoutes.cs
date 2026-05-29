namespace SOPS.Application.External;

/// <summary>
/// REST paths on the external third-party order API (combined with HttpClient BaseAddress).
/// </summary>
public static class ThirdPartyApiRoutes
{
  public const string Health = "/health";
  public const string SubmitOrder = "/api/v1/orders";
}
