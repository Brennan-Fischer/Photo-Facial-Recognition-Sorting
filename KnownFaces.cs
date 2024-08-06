using DlibDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FischBowl_Sorting_Script
{
    internal class KnownFaces
    {
        public string Name { get; set; }
        public List<Matrix<float>> Encodings { get; set; }
    }
}
