namespace NursingBot.Core;

public record Identifier(string Name)
{
    public const string Separator = "__";

    public readonly string Name = Name;

    public Identifier Sub(int subName) => this.Sub(subName.ToString());
    public Identifier Sub(string subName) => new Identifier(string.Join(Separator, this.Name, subName));

    public static implicit operator string(Identifier self) => self.Name;
}
