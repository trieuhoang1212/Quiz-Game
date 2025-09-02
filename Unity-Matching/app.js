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
    console.log("A client connected: ", socket.id);

    socket.on("searchGame", () => {
    // Prevent duplicate queueing for the same socket
    const alreadyQueued = players.some((p) => p.id === socket.id);
    if (!alreadyQueued) {
      if (players.length < 2) {
        players.push(socket);
        const playerNumber = players.length;

        // log ra server
        console.log(`Player ${playerNumber} search game: ${socket.id}`);

        // gửi cho chính client biết mình là số mấy
        socket.emit("playerSearching", { playerNumber });
      } else {
        // do not use reserved 'error' event name; send a custom error event instead
        socket.emit("matchError", { message: "Game is full" });
        return;
      }
    } else {
      // Optionally inform client they're already queued
      socket.emit("playerSearching", { playerNumber: players.findIndex(p => p.id === socket.id) + 1 });
    }

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
    score: data.score || 0,
    socket: socket,
  };

  if (Object.keys(scores).length === 2) {
    const ids = Object.keys(scores);
    const p1 = scores[ids[0]];
    const p2 = scores[ids[1]];

    const result1 = getResult(p1.score, p2.score);
    const result2 = getResult(p2.score, p1.score);
    
    console.log(`Sending to ${p1.socket.id}: myScore=${p1.score}, opponentScore=${p2.score}, result=${result1}`);
    console.log(`Sending to ${p2.socket.id}: myScore=${p2.score}, opponentScore=${p1.score}, result=${result2}`);

    p1.socket.emit("gameResult", {
      myScore: p1.score,
      opponentScore: p2.score,
      result: result1
    });
    p2.socket.emit("gameResult", {
      myScore: p2.score,
      opponentScore: p1.score,
      result: result2
    });

    setTimeout(() => {
      // Notify current players before clearing the arrays
      const toNotify = [...players];
      players = [];
      scores = {};
      console.log("Game reset: players and scores cleared.");
      toNotify.forEach(player => {
        if (player?.connected) {
          player.emit("gameReset", { message: "Game has been reset" });
        }
      });
    }, 10000);
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