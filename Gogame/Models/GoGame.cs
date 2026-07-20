using Avalonia.Controls;
using Avalonia.Controls.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Gogame.Models.GoGame;

namespace Gogame.Models
{
    
    public class GoGame
    {
        public enum Stone
        {
            Empty,
            Black,
            White,
        }

        public class GoBoard
        {
            public int Size { get; set; }
            public Stone[,] Board { get; }
            public class GameResult
            {
                public double BlackScore { get; set; }
                public double WhiteScore { get; set; }
                public int BlackTerritory { get; set; }
                public int WhiteTerritory { get; set; }
                public int BlackCaptures { get; set; }
                public int WhiteCaptures { get; set; }
                public double Komi { get; set; } = 6.5;
                public string Winner => BlackScore > WhiteScore ? "Black" : "White";
                public double Margin => Math.Abs(BlackScore - WhiteScore);
            }
            public int BlackCaptures { get; set; }
            public int WhiteCaptures { get; set; }
            public bool IsGameOver { get; private set; }
            public int ConsecutivePasses { get; private set; }
            public Stone CurrentPlayer { get; private set; } = Stone.Black;

            // The board
            public GoBoard(int size = 19)
            {
                Size = size;
                Board = new Stone[size, size];
            }

            // Coordinates
            public bool IsOnBoard(int x, int y)
            {
                if (x >= 0)
                {
                    if (y >= 0)
                    {
                        if (x < Size)
                        {
                            if (y < Size)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }



            // Neighbors
            public IEnumerable<(int x, int y)> GetNeighbors(int x, int y)
            {
                if (IsOnBoard(x - 1, y)) yield return (x - 1, y);
                if (IsOnBoard(x + 1, y)) yield return (x + 1, y);
                if (IsOnBoard(x, y - 1)) yield return (x, y - 1);
                if (IsOnBoard(x, y + 1)) yield return (x, y + 1);
            }

            // Searching for groups of stones
            public HashSet<(int x, int y)> GetGroup(int startX, int startY)
            {
                var stone = Board[startX, startY];
                if (stone == Stone.Empty)
                {
                    return new HashSet<(int x, int y)>();
                }

                var visited = new HashSet<(int x, int y)>();
                var stack = new Stack<(int x, int y)>();

                stack.Push((startX, startY));

                while (stack.Count > 0)
                {
                    var (x, y) = stack.Pop();

                    if (!visited.Add((x, y)))
                    {
                        continue;
                    }

                    foreach (var (nx, ny) in GetNeighbors(x, y))
                    {
                        if (Board[nx, ny] == stone)
                        {
                            stack.Push((nx, ny));
                        }
                    }
                }
                return visited;
            }

            // Counting group freedom
            public int CountLiberties(HashSet<(int x, int y)> group)
            {
                var liberties = new HashSet<(int, int)>();
                foreach (var (x, y) in group)
                {
                    foreach (var (nx, ny) in GetNeighbors(x, y))
                    {
                        if (Board[nx, ny] == Stone.Empty)
                        {
                            liberties.Add((nx, ny));
                        }
                    }
                }
                return liberties.Count;
            }
            // Removal
            public int RemoveGroup(HashSet<(int x, int y)> group)
            {
                foreach (var (x, y) in group)
                {
                    Board[x, y] = Stone.Empty;
                }
                return group.Count;
            }

            // coordinates of stones
            public bool PlaceStone(int x, int y, Stone stone)
            {
                if (!IsOnBoard(x, y) || Board[x, y] != Stone.Empty)
                    return false;

                var oldHash = GetBoardHash();

                Board[x, y] = stone;
                bool capturedSomething = false;

                foreach (var (nx, ny) in GetNeighbors(x, y))
                {
                    if (Board[nx, ny] == Opponent(stone))
                    {
                        var group = GetGroup(nx, ny);
                        if (CountLiberties(group) == 0)
                        {
                            int captured = RemoveGroup(group);
                            capturedSomething = true;

                            if (stone == Stone.Black)
                            {
                                BlackCaptures += captured;
                            }
                            else
                            {
                                WhiteCaptures += captured;
                            }
                        }
                    }
                }

                var ownGroup = GetGroup(x, y);
                if (!capturedSomething && CountLiberties(ownGroup) == 0)
                {
                    Board[x, y] = Stone.Empty; 
                    return false;
                }

                // KO check
                var newHash = GetBoardHash();
                if (previousPosition != null && newHash == previousPosition)
                {
                    RestoreBoardFromHash(oldHash);
                    return false;
                }

                previousPosition = oldHash;
                return true;

            }
            // Save board state
            private string? previousPosition;

            // Method to create a hash of the board
            private string GetBoardHash()
            {
                var sb = new StringBuilder(Size * Size);
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        sb.Append((int)Board[x, y]);
                    }
                }

                return sb.ToString();
            }

            // Restore the board from the hash
            private void RestoreBoardFromHash(string hash)
            {
                int i = 0;
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        Board[x, y] = (Stone)(hash[i++] - '0');
                    }
                }
            }
            
            private Stone Opponent (Stone stone)
            {
                return stone switch
                {
                    Stone.Black => Stone.White,
                    Stone.White => Stone.Black,
                    _ => Stone.Empty
                };
            }

            // RESIGN method
            public Stone Resign()
            {
                IsGameOver = true;
                if (CurrentPlayer == Stone.Black)
                {
                    return Stone.White;
                }
                else
                {
                    return Stone.Black;
                }
            }

            //Switch player
            private void SwitchPlayer()
            {
                if (CurrentPlayer == Stone.Black)
                {
                    CurrentPlayer = Stone.White;
                }
                else
                {
                    CurrentPlayer = Stone.Black;
                }
            }

            //PASS for the given player
            public bool Pass(Stone player)
            {
                if (IsGameOver)
                    return false;
                if (player != CurrentPlayer)
                    return false;

                ConsecutivePasses++;
                SwitchPlayer();

                if (ConsecutivePasses >= 2)
                    IsGameOver = true;

                return true;
            }

            // RESIGN for the given player
            public Stone? Resign(Stone player)
            {
                if (IsGameOver)
                    return null;

                if (player != CurrentPlayer)
                    return null;

                IsGameOver = true;
                if (player == Stone.Black)
                {
                    return Stone.White;
                }
                else
                {
                    return Stone.Black;
                }
            }

            //Change player
            public void SwitchTurnAfterMove()
            {
                ConsecutivePasses = 0;
                SwitchPlayer();
            }

            //Reset
            public void Reset()
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        Board[x, y] = Stone.Empty;
                    }
                }
                BlackCaptures = 0;
                WhiteCaptures = 0;
                ConsecutivePasses = 0;
                IsGameOver = false;
                CurrentPlayer = Stone.Black;
                previousPosition = null;
            }

            // Territory highlighting
            public enum TerritoryOwner
            {
                None,
                Black,
                White,
            }

            // Legal Move check
            public bool IsLegalMove(int x, int y, Stone stone)
            {
                if (!IsOnBoard(x, y) || Board[x, y] != Stone.Empty)
                    return false;

                Board[x, y] = stone;

                bool capturedSomething = false;
                foreach (var (nx, ny) in GetNeighbors(x, y))
                {
                    if (Board[nx, ny] == Opponent(stone))
                    {
                        var group = GetGroup(nx, ny);
                        if (CountLiberties(group) == 0)
                            capturedSomething = true;
                    }
                }

                bool legal = true;
                if (!capturedSomething)
                {
                    var ownGroup = GetGroup(x, y);
                    if (CountLiberties(ownGroup) == 0)
                        legal = false; 
                }

                Board[x, y] = Stone.Empty;
                return legal;
            }
        }
    }
}
