CREATE DATABASE pointgame;
\c pointgame;

CREATE TABLE games (
    id SERIAL PRIMARY KEY,
    grid_width INT NOT NULL,
    grid_height INT NOT NULL,
    player1_color VARCHAR(50) NOT NULL,
    player2_color VARCHAR(50) NOT NULL,
    current_turn INT NOT NULL DEFAULT 1,
    status VARCHAR(20) NOT NULL DEFAULT 'InProgress',
    player1_score INT NOT NULL DEFAULT 0,
    player2_score INT NOT NULL DEFAULT 0
);

CREATE TABLE moves (
    id SERIAL PRIMARY KEY,
    game_id INT REFERENCES games(id) ON DELETE CASCADE,
    x INT NOT NULL,
    y INT NOT NULL,
    player_number INT NOT NULL,
    move_order INT NOT NULL
);
