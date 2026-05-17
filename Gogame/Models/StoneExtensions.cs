using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Models
{
    public static class StoneExtensions
    {
        public static GoGame.Stone Opponent(this GoGame.Stone stone)
        {
            return stone switch
            {
                GoGame.Stone.Black => GoGame.Stone.White,
                GoGame.Stone.White => GoGame.Stone.Black,
                _ => GoGame.Stone.Empty
            };
        }
    }
}
