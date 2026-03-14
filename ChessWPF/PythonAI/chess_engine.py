#!/usr/bin/env python3
"""
chess_engine.py — Python Chess AI Engine
Connects via TCP socket. Receives board FEN / move list,
returns best move in UCI format (e.g. "e2e4").

Protocol:
  Client sends: <fen_string>\n
  Server replies: <best_move_uci>\n

Usage: python chess_engine.py [port=6000]
"""

import socket
import sys
import time
import random
from typing import Optional, Tuple, List

# ──────────────────────────────────────────────────────────────────────────────
# Simple board representation (0x88-style)
# ──────────────────────────────────────────────────────────────────────────────

EMPTY = 0
W_PAWN = 1; W_KNIGHT = 2; W_BISHOP = 3; W_ROOK = 4; W_QUEEN = 5; W_KING = 6
B_PAWN = 7; B_KNIGHT = 8; B_BISHOP = 9; B_ROOK = 10; B_QUEEN = 11; B_KING = 12

PIECE_VALUES = {
    W_PAWN: 100, W_KNIGHT: 320, W_BISHOP: 330,
    W_ROOK: 500, W_QUEEN: 900, W_KING: 20000,
    B_PAWN: -100, B_KNIGHT: -320, B_BISHOP: -330,
    B_ROOK: -500, B_QUEEN: -900, B_KING: -20000,
}

FEN_PIECE_MAP = {
    'P': W_PAWN, 'N': W_KNIGHT, 'B': W_BISHOP,
    'R': W_ROOK, 'Q': W_QUEEN,  'K': W_KING,
    'p': B_PAWN, 'n': B_KNIGHT, 'b': B_BISHOP,
    'r': B_ROOK, 'q': B_QUEEN,  'k': B_KING,
}

PIECE_STR = {v: k for k, v in FEN_PIECE_MAP.items()}
PIECE_STR[EMPTY] = '.'

def is_white(p): return 1 <= p <= 6
def is_black(p): return 7 <= p <= 12
def is_empty(p): return p == 0

STARTING_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"


class Board:
    def __init__(self):
        self.squares = [EMPTY] * 64
        self.side    = 'w'       # 'w' or 'b'
        self.castling= 'KQkq'
        self.ep_sq   = None      # en passant target square index or None
        self.halfmove= 0
        self.fullmove= 1

    def copy(self):
        b = Board()
        b.squares  = self.squares[:]
        b.side     = self.side
        b.castling = self.castling
        b.ep_sq    = self.ep_sq
        b.halfmove = self.halfmove
        b.fullmove = self.fullmove
        return b

    @staticmethod
    def from_fen(fen: str) -> 'Board':
        b = Board()
        parts = fen.strip().split()
        rows  = parts[0].split('/')
        idx   = 0
        for row in rows:
            for ch in row:
                if ch.isdigit():
                    idx += int(ch)
                else:
                    b.squares[idx] = FEN_PIECE_MAP.get(ch, EMPTY)
                    idx += 1
        b.side     = parts[1] if len(parts) > 1 else 'w'
        b.castling = parts[2] if len(parts) > 2 else '-'
        ep         = parts[3] if len(parts) > 3 else '-'
        b.ep_sq    = sq_from_name(ep) if ep != '-' else None
        b.halfmove = int(parts[4]) if len(parts) > 4 else 0
        b.fullmove = int(parts[5]) if len(parts) > 5 else 1
        return b

    def to_fen(self) -> str:
        rows = []
        for r in range(8):
            empty = 0
            row_str = ''
            for c in range(8):
                p = self.squares[r * 8 + c]
                if is_empty(p):
                    empty += 1
                else:
                    if empty:
                        row_str += str(empty)
                        empty = 0
                    row_str += PIECE_STR[p]
            if empty:
                row_str += str(empty)
            rows.append(row_str)
        ep = sq_to_name(self.ep_sq) if self.ep_sq is not None else '-'
        return (f"{'/' .join(rows)} {self.side} "
                f"{self.castling or '-'} {ep} "
                f"{self.halfmove} {self.fullmove}")

    def apply_uci(self, uci: str) -> 'Board':
        """Apply a UCI move string and return new board."""
        fr_sq = sq_from_name(uci[0:2])
        to_sq = sq_from_name(uci[2:4])
        promo = uci[4] if len(uci) > 4 else None
        return self.apply_move(fr_sq, to_sq, promo)

    def apply_move(self, fr: int, to: int, promo: Optional[str] = None) -> 'Board':
        b    = self.copy()
        piece= b.squares[fr]
        captured = b.squares[to]

        b.squares[to] = piece
        b.squares[fr] = EMPTY

        # Pawn promotion
        if promo and is_white(piece) and piece == W_PAWN and to < 8:
            b.squares[to] = FEN_PIECE_MAP[promo.upper()]
        elif promo and is_black(piece) and piece == B_PAWN and to >= 56:
            b.squares[to] = FEN_PIECE_MAP[promo.lower()]

        # En passant
        if piece in (W_PAWN, B_PAWN) and to == self.ep_sq and self.ep_sq is not None:
            ep_capture = to + (8 if is_white(piece) else -8)
            b.squares[ep_capture] = EMPTY

        # Set new ep target
        if piece == W_PAWN and fr - to == 16:
            b.ep_sq = fr - 8
        elif piece == B_PAWN and to - fr == 16:
            b.ep_sq = fr + 8
        else:
            b.ep_sq = None

        # Castling (simplified)
        if piece == W_KING:
            b.castling = b.castling.replace('K','').replace('Q','')
            if fr == 60 and to == 62:   # king-side
                b.squares[63] = EMPTY; b.squares[61] = W_ROOK
            elif fr == 60 and to == 58: # queen-side
                b.squares[56] = EMPTY; b.squares[59] = W_ROOK
        elif piece == B_KING:
            b.castling = b.castling.replace('k','').replace('q','')
            if fr == 4 and to == 6:
                b.squares[7] = EMPTY; b.squares[5] = B_ROOK
            elif fr == 4 and to == 2:
                b.squares[0] = EMPTY; b.squares[3] = B_ROOK

        b.side = 'b' if self.side == 'w' else 'w'
        if b.side == 'w':
            b.fullmove += 1
        return b


def sq_from_name(name: str) -> int:
    if not name or len(name) < 2: return 0
    c = ord(name[0]) - ord('a')
    r = 8 - int(name[1])
    return r * 8 + c

def sq_to_name(sq: int) -> str:
    r, c = divmod(sq, 8)
    return chr(ord('a') + c) + str(8 - r)


# ──────────────────────────────────────────────────────────────────────────────
# Move generation (pseudo-legal)
# ──────────────────────────────────────────────────────────────────────────────

def generate_moves(board: Board) -> List[Tuple[int, int, Optional[str]]]:
    moves = []
    white = board.side == 'w'
    for sq in range(64):
        p = board.squares[sq]
        if is_empty(p): continue
        if white and not is_white(p): continue
        if not white and not is_black(p): continue

        r, c = divmod(sq, 8)

        if p in (W_PAWN, B_PAWN):
            moves.extend(pawn_moves(board, sq, r, c, white))
        elif p in (W_KNIGHT, B_KNIGHT):
            moves.extend(knight_moves(board, sq, r, c, white))
        elif p in (W_BISHOP, B_BISHOP):
            moves.extend(sliding_moves(board, sq, r, c, white, [(1,1),(1,-1),(-1,1),(-1,-1)]))
        elif p in (W_ROOK, B_ROOK):
            moves.extend(sliding_moves(board, sq, r, c, white, [(1,0),(-1,0),(0,1),(0,-1)]))
        elif p in (W_QUEEN, B_QUEEN):
            moves.extend(sliding_moves(board, sq, r, c, white,
                         [(1,1),(1,-1),(-1,1),(-1,-1),(1,0),(-1,0),(0,1),(0,-1)]))
        elif p in (W_KING, B_KING):
            moves.extend(king_moves(board, sq, r, c, white))
    return moves


def pawn_moves(board, sq, r, c, white):
    moves = []
    dir_r = -1 if white else 1
    start = 6 if white else 1
    promo_rank = 0 if white else 7
    my = is_white if white else is_black
    opp= is_black if white else is_white
    pawn = W_PAWN if white else B_PAWN

    nr = r + dir_r
    if 0 <= nr < 8:
        fwd = nr * 8 + c
        if is_empty(board.squares[fwd]):
            if nr == promo_rank:
                for pr in ('q','r','b','n'):
                    moves.append((sq, fwd, pr if white else pr))
            else:
                moves.append((sq, fwd, None))
                if r == start:
                    fwd2 = (nr + dir_r) * 8 + c
                    if is_empty(board.squares[fwd2]):
                        moves.append((sq, fwd2, None))

        for dc in (-1, 1):
            nc = c + dc
            if 0 <= nc < 8:
                diag = nr * 8 + nc
                if opp(board.squares[diag]):
                    if nr == promo_rank:
                        for pr in ('q','r','b','n'):
                            moves.append((sq, diag, pr))
                    else:
                        moves.append((sq, diag, None))
                if board.ep_sq == diag:
                    moves.append((sq, diag, None))
    return moves


def knight_moves(board, sq, r, c, white):
    moves = []
    opp = is_black if white else is_white
    for dr, dc in [(-2,-1),(-2,1),(-1,-2),(-1,2),(1,-2),(1,2),(2,-1),(2,1)]:
        nr, nc = r+dr, c+dc
        if 0 <= nr < 8 and 0 <= nc < 8:
            t = board.squares[nr*8+nc]
            if is_empty(t) or opp(t):
                moves.append((sq, nr*8+nc, None))
    return moves


def sliding_moves(board, sq, r, c, white, dirs):
    moves = []
    opp = is_black if white else is_white
    for dr, dc in dirs:
        nr, nc = r+dr, c+dc
        while 0 <= nr < 8 and 0 <= nc < 8:
            t = board.squares[nr*8+nc]
            if is_empty(t):
                moves.append((sq, nr*8+nc, None))
            elif opp(t):
                moves.append((sq, nr*8+nc, None))
                break
            else:
                break
            nr += dr; nc += dc
    return moves


def king_moves(board, sq, r, c, white):
    moves = []
    opp = is_black if white else is_white
    for dr in (-1,0,1):
        for dc in (-1,0,1):
            if dr == 0 and dc == 0: continue
            nr, nc = r+dr, c+dc
            if 0 <= nr < 8 and 0 <= nc < 8:
                t = board.squares[nr*8+nc]
                if is_empty(t) or opp(t):
                    moves.append((sq, nr*8+nc, None))
    # simplified castling
    if white and sq == 60:
        if 'K' in board.castling:
            if (is_empty(board.squares[61]) and is_empty(board.squares[62])
                    and board.squares[63] == W_ROOK):
                moves.append((60, 62, None))
        if 'Q' in board.castling:
            if (is_empty(board.squares[59]) and is_empty(board.squares[58])
                    and is_empty(board.squares[57]) and board.squares[56] == W_ROOK):
                moves.append((60, 58, None))
    elif not white and sq == 4:
        if 'k' in board.castling:
            if is_empty(board.squares[5]) and is_empty(board.squares[6]):
                moves.append((4, 6, None))
        if 'q' in board.castling:
            if (is_empty(board.squares[3]) and is_empty(board.squares[2])
                    and is_empty(board.squares[1])):
                moves.append((4, 2, None))
    return moves


def is_in_check(board: Board, white: bool) -> bool:
    king = W_KING if white else B_KING
    king_sq = next((i for i, p in enumerate(board.squares) if p == king), -1)
    if king_sq < 0: return False
    # check if any opponent move attacks king square
    opp_board = board.copy()
    opp_board.side = 'b' if white else 'w'
    for fr, to, _ in generate_moves(opp_board):
        if to == king_sq:
            return True
    return False


def get_legal_moves(board: Board):
    pseudo = generate_moves(board)
    legal  = []
    white  = board.side == 'w'
    for move in pseudo:
        b2 = board.apply_move(*move)
        if not is_in_check(b2, white):
            legal.append(move)
    return legal


# ──────────────────────────────────────────────────────────────────────────────
# Evaluation
# ──────────────────────────────────────────────────────────────────────────────

PAWN_TABLE_W = [
     0,  0,  0,  0,  0,  0,  0,  0,
    50, 50, 50, 50, 50, 50, 50, 50,
    10, 10, 20, 30, 30, 20, 10, 10,
     5,  5, 10, 25, 25, 10,  5,  5,
     0,  0,  0, 20, 20,  0,  0,  0,
     5, -5,-10,  0,  0,-10, -5,  5,
     5, 10, 10,-20,-20, 10, 10,  5,
     0,  0,  0,  0,  0,  0,  0,  0
]

def evaluate(board: Board) -> int:
    score = 0
    for sq, p in enumerate(board.squares):
        if is_empty(p): continue
        val = PIECE_VALUES.get(p, 0)
        row, col = divmod(sq, 8)
        if p == W_PAWN:
            val += PAWN_TABLE_W[sq]
        elif p == B_PAWN:
            val -= PAWN_TABLE_W[63 - sq]
        score += val
    return score if board.side == 'w' else -score


def minimax(board: Board, depth: int, alpha: int, beta: int, maximizing: bool) -> int:
    if depth == 0:
        return evaluate(board)

    legal = get_legal_moves(board)
    if not legal:
        if is_in_check(board, board.side == 'w'):
            return -100000 + depth if maximizing else 100000 - depth
        return 0

    if maximizing:
        best = -10**9
        for move in legal:
            b2  = board.apply_move(*move)
            val = minimax(b2, depth - 1, alpha, beta, False)
            best  = max(best, val)
            alpha = max(alpha, val)
            if beta <= alpha: break
        return best
    else:
        best = 10**9
        for move in legal:
            b2  = board.apply_move(*move)
            val = minimax(b2, depth - 1, alpha, beta, True)
            best = min(best, val)
            beta = min(beta, val)
            if beta <= alpha: break
        return best


def get_best_move(board: Board, depth: int = 3) -> Optional[str]:
    legal = get_legal_moves(board)
    if not legal: return None

    best_move  = None
    best_score = -10**9

    for move in legal:
        b2    = board.apply_move(*move)
        score = -minimax(b2, depth - 1, -10**9, 10**9, False)
        if score > best_score:
            best_score = score
            best_move  = move

    if best_move is None: return None
    fr, to, promo = best_move
    uci = sq_to_name(fr) + sq_to_name(to)
    if promo: uci += promo
    return uci


# ──────────────────────────────────────────────────────────────────────────────
# TCP Server
# ──────────────────────────────────────────────────────────────────────────────

def run_server(port: int = 6000):
    print(f"[ChessEngine] Запуск на порту {port}...")
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        s.bind(('127.0.0.1', port))
        s.listen(1)
        print(f"[ChessEngine] Ожидание подключения...")

        while True:
            conn, addr = s.accept()
            print(f"[ChessEngine] Подключён: {addr}")
            with conn:
                buf = b''
                while True:
                    data = conn.recv(1024)
                    if not data: break
                    buf += data
                    while b'\n' in buf:
                        line, buf = buf.split(b'\n', 1)
                        fen = line.decode('utf-8').strip()
                        if not fen: continue
                        print(f"[ChessEngine] FEN: {fen}")
                        try:
                            board     = Board.from_fen(fen)
                            best_move = get_best_move(board, depth=3)
                            response  = (best_move or 'none') + '\n'
                            conn.sendall(response.encode('utf-8'))
                            print(f"[ChessEngine] Ответ: {best_move}")
                        except Exception as e:
                            print(f"[ChessEngine] Ошибка: {e}")
                            conn.sendall(b'none\n')


if __name__ == '__main__':
    port = int(sys.argv[1]) if len(sys.argv) > 1 else 6000
    run_server(port)
