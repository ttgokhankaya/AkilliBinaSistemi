using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedObject
{
    public interface IMemoryManager : IDisposable, IBase
    {
        List<AdleMemoryObject> Memories { get; set; }
        void AddMemory(AdleMemoryObject memory);
        bool AnalyzeMemory(AdleMemoryObject memory);
        List<AdleMemoryObject> GetAllMemories();
    }
}
