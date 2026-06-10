using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TypeGen.Core.TypeAnnotations;

namespace QualityControl.WPF.Models
{
    [ExportTsClass(OutputDir = "../quality-control-vue-dashboard/src/models")]
    public class StreamChunk<T>
    {
        public StreamChunk()
        {
            
        }

        public StreamChunk(string name, int chunkIndex, List<T> items, bool isLastChunk)
        {
            Name = name;
            ChunkIndex = chunkIndex;
            Items = items;
            IsLastChunk = isLastChunk;
        }

        public string Name { get; set; } = "";
        public int ChunkIndex { get; set; }
        public List<T> Items { get; set; } = [];
        public bool IsLastChunk { get; set; }
    }
}
