using board.automata.surf.proto;
using Grpc.Core;

namespace board.automata.surf.grpc.services;

public sealed class ModelSurfnessImpl : ModelSurfness.ModelSurfnessBase
{
  private readonly PreparedSessionRegistry _sessions;
  private readonly ILogger<ModelSurfnessImpl> _logger;

  public ModelSurfnessImpl(PreparedSessionRegistry sessions, ILogger<ModelSurfnessImpl> logger)
  {
    _sessions = sessions;
    _logger = logger;
  }

  /* Implement the surfness 🤙*/
}
