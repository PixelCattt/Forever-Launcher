#nullable disable
namespace ForeverLauncher
{
  public sealed class StatusMessage
  {
    public readonly string Key;
    public readonly object[] Args;
    public readonly MessageKind Kind;

    public StatusMessage(MessageKind kind, string key, params object[] args)
    {
      this.Kind = kind;
      this.Key = key;
      this.Args = args;
    }

    public override string ToString() => Localization.Get(this.Key, this.Args);

    public string ToString(string lang) => Localization.GetIn(lang, this.Key, this.Args);
  }
}
