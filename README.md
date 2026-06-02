# automata.surf
Peer-to-peer AI model hosting infrastructure. Feedback, suggestions and PRs are welcome!
https://automata.surf/

### Goals
The initial aim is to provide a chat interface with three types of self-hosted models: 
- Ollama, via HTTP. Target model is gemma4 because it's absolutely amazing.
- ONNX, via native C#/.NET. Target model is Phi-4.5 primarily for its optimization for CPU.
- Python Sidecar, via Pipelines by HuggingFace. Target model TBD, likely a mini-llm. 

### Architecture
1. gRPC layer for agnostic model inference. The surfness proto is the language all three interfaces above will speak to the ASP.NET server in.
2. Websocket wrapping of outbound proto messages. This is how we facilitate an HTTP/S peer-to-peer connection over internet, without the headaches of direct p2p TCP.
3. Confirmation, authentication via an aggregator (a Beach) if desired. However, for testing and the general philosophy is this project will be self-contained and usable as a locally hosted dockerized AI model with a common API.
