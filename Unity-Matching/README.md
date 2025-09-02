# Unity Matching Server

Simple Socket.IO matchmaking server for the Unity quiz game.

- Listens on http://localhost:8000
- Events:
  - Client -> server: `searchGame`, `submitScore { score }`
  - Server -> client: `playerSearching { playerNumber }`, `startGame { playerNumber }`, `gameResult { myScore, opponentScore, result }`, `gameReset { message }`, `matchError { message }`

How to run (PowerShell):

```powershell
# from folder: Unity-Matching
npm install
node app.js
```

Keep this running before starting your Unity play mode.