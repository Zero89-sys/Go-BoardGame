using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gogame.TutorialView;

namespace Gogame.Models
{
    internal class SgfParser
    {
        private class SgfNode
        {
            public Dictionary<string, List<string>> Properties { get; set; } = new();
            public List<SgfNode> Children { get; set; } = new();
        }

        public static TutorialStep LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"SGF file not found: {filePath}");
            }
            string sgfText = File.ReadAllText(filePath);
            return ParseSgf(sgfText);
        }

        // Processes an SGF text string.
        public static TutorialStep ParseSgf(string sgfText)
        {
            var step = new TutorialStep();

            int index = 0;
            var rootContainer = new SgfNode();
            ParseNodes(sgfText, ref index, rootContainer);

            if (rootContainer.Children.Count == 0)
                return step;

            var rootNode = rootContainer.Children[0];

            // Black stones
            if(rootNode.Properties.TryGetValue("AB", out var blackStones))
            {
                foreach(var coords in blackStones)
                {
                    var (x, y) = ConvertCoords(coords);
                    if(x >= 0 && y >= 0)
                    {
                        step.InitialStones.Add(new InitialStone { x = x, y = y, Color = "Black" });
                    }
                }
            }

            // White stones
            if(rootNode.Properties.TryGetValue("AW", out var whiteStones))
            {
                foreach(var coords in whiteStones)
                {
                    var (x, y) = ConvertCoords(coords);
                    if (x >= 0 && y >= 0)
                        step.InitialStones.Add(new InitialStone { x = x, y = y, Color = "White" });
                }
            }

            // Instructions
            if(rootNode.Properties.TryGetValue("C", out var rootComments) && rootComments.Count > 0)
            {
                step.Instructions = rootComments[0];
            }

            step.AllowAnyMove = false;
            step.HideMarkers = true;
            step.ResponseMessage = "Puzzle solved!";

            string playerColor = "Black";
            var firstMoveNode = FindFirstMoveNode(rootNode.Children);
            if(firstMoveNode != null && firstMoveNode.Properties.ContainsKey("W"))
            {
                playerColor = "White";
            }
            step.MoveColor = playerColor;

            step.AllowedMoves = ConvertNodesToMoveOptions(rootNode.Children, playerColor);

            return step;
        }
        private static void ParseNodes(string text, ref int index, SgfNode parent)
        {
            SgfNode? lastNode = null;

            while(index < text.Length)
            {
                char c = text[index];
                if(c == '(')
                {
                    index++;
                    ParseNodes(text, ref index, lastNode ?? parent);
                }else if(c == ')')
                {
                    index++;
                    return;
                }else if(c == ';')
                {
                    index++;
                    var node = new SgfNode();
                    ReadProperties(text, ref index, node.Properties);

                    if(lastNode == null)
                    {
                        parent.Children.Add(node);
                    }
                    else
                    {
                        lastNode.Children.Add(node);
                    }
                    lastNode = node;
                }
                else
                {
                    index++;
                }
            }
        }

        // Reading node properties
        private static void ReadProperties(string text, ref int index, Dictionary<string, List<string>> properties)
        {
            while(index < text.Length)
            {
                while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
                if (index >= text.Length) break;

                char nextChar = text[index];
                if (nextChar == ';' || nextChar == '(' || nextChar == ')') break;

                string key = "";
                while(index < text.Length && char.IsUpper(text[index]))
                {
                    key += text[index];
                    index++;
                }
                if (string.IsNullOrEmpty(key))
                {
                    index++;
                    continue;
                }

                // Reading values ​​in parentheses
                var values = new List<string>();
                while(index < text.Length && text[index] == '[')
                {
                    index++;
                    string val = "";
                    while(index < text.Length && text[index] != ']')
                    {
                        if (text[index] == '\\' && index + 1 < text.Length && text[index + 1] == ']')
                        {
                            val += ']';
                            index += 2;
                        }
                        else
                        {
                            val += text[index];
                            index++;
                        }
                    }
                    if (index < text.Length && text[index] == ']') index++;
                    values.Add(val);
                }
                if(values.Count > 0)
                {
                    properties[key] = values;
                }
            }
        }
        // Converting SGF nodes to MoveOption objects
        private static List<MoveOption> ConvertNodesToMoveOptions(List<SgfNode> nodes, string playerColor)
        {
            var options = new List<MoveOption>();
            string playerTag = playerColor == "Black" ? "B" : "W";
            string opponentTag = playerColor == "Black" ? "W" : "B";

            foreach(var node in nodes)
            {
                if(node.Properties.TryGetValue(playerTag, out var moveVals) && moveVals.Count > 0)
                {
                    var moveOpt = new MoveOption();
                    var (x, y) = ConvertCoords(moveVals[0]);
                    moveOpt.x = x;
                    moveOpt.y = y;
                    moveOpt.Color = playerColor;

                    // Comment on the player's move
                    if(node.Properties.TryGetValue("C", out var cVals) && cVals.Count > 0)
                    {
                        moveOpt.NextInstructions = cVals[0];
                    }

                    // Searching for an immediate response from the opponent among the direct descendants
                    SgfNode? opponentChild = node.Children.FirstOrDefault(c => c.Properties.ContainsKey(opponentTag));
                    
                    if(opponentChild != null)
                    {
                        var oppMoveVals = opponentChild.Properties[opponentTag];
                        if(oppMoveVals.Count > 0)
                        {
                            var (ox, oy) = ConvertCoords(oppMoveVals[0]);
                            moveOpt.HasOpponentResponse = true;
                            moveOpt.OpponentX = ox;
                            moveOpt.OpponentY = oy;
                            moveOpt.OpponentColor = playerColor == "Black" ? "White" : "Black";
                        }
                        if(opponentChild.Properties.TryGetValue("C", out var oppCVals) && oppCVals.Count > 0)
                        {
                            moveOpt.NextInstructions = oppCVals[0];
                        }
                        // The children of the opponent's node are the player's next moves.
                        moveOpt.NextAllowedMoves = ConvertNodesToMoveOptions(opponentChild.Children, playerColor);
                    }
                    // The opponent did not respond, but the branch can continue.
                    else
                    {
                        moveOpt.NextAllowedMoves = ConvertNodesToMoveOptions(node.Children, playerColor);
                    }

                    options.Add(moveOpt);
                }
            }
            return options;
        }

        // Pomocná metoda pro konverzi SGF souřadnic
        private static (int x, int y) ConvertCoords(string sgfCoords)
        {
            if(string.IsNullOrEmpty(sgfCoords) || sgfCoords.Length < 2)
            {
                return (-1, -1);
            }

            int x = sgfCoords[0] - 'a';
            int y = sgfCoords[1] - 'a';
            return (x, y);
        }

        private static SgfNode? FindFirstMoveNode(List<SgfNode> nodes)
        {
            foreach(var node in nodes)
            {
                if(node.Properties.ContainsKey("B") || node.Properties.ContainsKey("W"))
                    return node;

                var childMove = FindFirstMoveNode(node.Children);
                if (childMove != null) return childMove;
            }
            return null;
        }
    }
}
