using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gogame.Dto
{
    public class UploadGameDto
    {
        public Guid ClientId {  get; set; }
        public string Sgf { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public DateTime PlayedAtUtc { get; set; }
        public string PlayerColor { get; set; } = "B";
        public bool VsBot { get; set; }
    }

    public class UploadGameResponse
    {
        public int Id { get; set; }
    }
}
