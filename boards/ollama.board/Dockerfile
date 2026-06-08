FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish boards/ollama.board/ollama.board.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV BOARD_GRPC_PORT=50061
ENV BOARD_MODEL_KEEP_ALIVE=-1
ENTRYPOINT ["dotnet", "ollama.board.dll"]
