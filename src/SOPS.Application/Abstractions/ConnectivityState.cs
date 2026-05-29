namespace SOPS.Application.Abstractions;

public sealed class ConnectivityState
{
    private volatile bool _isOnline = true;

    public bool IsOnline => _isOnline;

    public void SetOnline(bool online) => _isOnline = online;
}
