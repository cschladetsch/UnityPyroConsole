# Network
This folder contains scripts in C# and Rho that allow for networked interaction between two Unity3d apps.

## Architecture
The system is based on Pyro. Once two nodes are connected, one can ask to subscribe to changes of a Transform in another.

```rho
ConnectGameObject("/SharedCube/Cube")
```

