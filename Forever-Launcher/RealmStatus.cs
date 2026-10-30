#nullable disable
namespace ForeverLauncher
{
  public sealed class RealmStatus
  {
    public RealmEntry Realm;
    public long Ms = -1;

    public bool Up => this.Ms >= 0L;
  }
}
