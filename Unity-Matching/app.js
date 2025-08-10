const http = require("http");
const socketIO = require("socket.io");
const express = require("express");

const app = express();
const server = http.createServer(app);
const io = socketIO(server, {
  cors: {
    origin: "*",
  },
});

let players = [];
let scores = {};

io.on("connection", (socket) => {
  console.log("A client connected:", socket.id);

  socket.on("searchGame", () => {
    console.log("Searching...");
    players.push(socket);

    if (players.length === 2) {
      console.log("Start Game!");

      players.forEach((player, index) => {
        player.emit("startGame", { playerNumber: index + 1 });
      });
    }
  });

  socket.on("submitScore", (data) => {
    console.log(`Score received from ${socket.id}: ${data.score}`);

    scores[socket.id] = {
      score: data.score,
      socket: socket,
    };

    if (Object.keys(scores).length === 2) {
      const ids = Object.keys(scores);
      const p1 = scores[ids[0]];
      const p2 = scores[ids[1]];

      // Send result to both players
      p1.socket.emit("finalResult", {
        myScore: p1.score,
        opponentScore: p2.score,
        result: getResult(p1.score, p2.score),
      });

      p2.socket.emit("finalResult", {
        myScore: p2.score,
        opponentScore: p1.score,
        result: getResult(p2.score, p1.score),
      });

      // Reset
      players = [];
      scores = {};
    }
  });

  socket.on("disconnect", () => {
    console.log("Client disconnected:", socket.id);
    players = players.filter((p) => p.id !== socket.id);
    delete scores[socket.id];
  });
});

function getResult(myScore, opponentScore) {
  if (myScore > opponentScore) return "WIN";
  if (myScore < opponentScore) return "LOSE";
  return "DRAW";
}

server.listen(8000, () => {
  console.log("Server is running on port 8000");
});
