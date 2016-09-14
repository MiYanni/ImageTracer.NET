using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracerNet
{
    internal abstract class Point<T>
    {
        public T X { get; set; }
        public T Y { get; set; }
    }
}
