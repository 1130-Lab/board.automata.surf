# automata.surf
Peer-to-peer AI model hosting infrastructure. Feedback, suggestions and PRs are welcome!
https://automata.surf/

### Goals
The initial aim is to provide a chat interface with three types of self-hosted models: 
- Ollama, via HTTP. Target model is gemma4 because it's absolutely amazing.
- ONNX, via native C#/.NET. Target model is Phi-4.5 primarily for its CPU optimized version.
- Python Sidecar, via Pipelines by HuggingFace. Target model TBD, likely a mini-llm. 

An end user should be able to use Ollama-compatible tooling (GUIs, TUIs, broker APIs) to communicate with them.

### Architecture / Design
WebAPI for local/p2p communication. 
- Replicate Ollama's REST API. This gives us access to existing Ollama tooling.
- CORS for filtering requests. When a new peer is added, they are first added to the CORS list. Peers will be authenticated by a common aggregation server ("Beach") if enabled.

The Beach is also the first address to be permitted by CORS if one is added, otherwise it will be localhost.
gRPC layer for Model <-> API communication. 
- Model agnostic for routing data to/from the ASP.NET web API. 
- Easily extended for additional model and io formats.

Dockerized model + gRPC layer + API
- Allow spin-up of models based on demand and resource allocation. 
- Use a model manifest to ascertain the model's resource requirements, and tenant limitations.
