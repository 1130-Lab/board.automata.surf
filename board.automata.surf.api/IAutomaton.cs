namespace board.automata.surf.api
{
  public interface IAutomaton
  {
    public string BoardName { get; }
    public string BoardDescription { get; }
    public string BoardVersion { get; }
    public string BoardUrl { get; }
    public string BoardAddress { get; }
  }
}
