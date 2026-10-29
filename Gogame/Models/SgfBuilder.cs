using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gogame.Models.GoGame;
using static Gogame.Models.GoGame.GoBoard;

namespace Gogame.Models
{
    public static class SgfBuilder
    {
        public static string Build(
            IEnumerable<MoveRecord> moves, int boardSize, double komi,
            string blackName, string whiteName, string result, DateTime playedAtUtc)
        {
            var sb = new StringBuilder();
            sb.Append("(;GM[1]FF[4]CA[UTF-8]AP[Gogame]");
            sb.Append($"SZ[{boardSize}]KM[{komi.ToString(CultureInfo.InvariantCulture)}]");
            sb.Append($"PB[{Escape(blackName)}]PW[{Escape(whiteName)}]");
            sb.Append($"DT[{playedAtUtc:yyyy-MM-dd}]RE[{Escape(result)}]");

            foreach (var m in moves)
            {
                char color = m.Color == Stone.Black ? 'B' : 'W';
                string point = m.IsPass ? "" : $"{Coord(m.X)}{Coord(m.Y)}";

                sb.Append(';').Append(color).Append('[').Append(point).Append(']');
            }

            sb.Append(')');
            return sb.ToString();
        }

        private static char Coord(int i) => (char)('a' + i);

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("]", "\\]");
    }
}
