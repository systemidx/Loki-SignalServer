# Loki-SignalServer
A message router built upon the [Loki WebSocket server](https://github.com/systemidx/Loki)

## Targets
The libraries all target .NET Standard 1.6. The server application targets .NET Core 1.1.

## Project STatus
This project is currently in **active development**. As such, please think twice before using it in production! For a more mature project, please take a look at [WebSocket-Sharp](https://github.com/sta/websocket-sharp).

## Project Rationale
The reason for the creation of this project comes mainly from frustration with existing projects (isn't that always how it is?). The particularly use case for Loki's creation is to implement a WebSocket server which would scale well, accept modular drop-ins for enhancing the engine, and be RFC 6455 compliant.