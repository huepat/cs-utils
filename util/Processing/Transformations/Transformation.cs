using System;

namespace HuePat.Util.Processing.Transformations {
    public class Transformation<T> where T : class {
        public bool UseParallel { get; set; }
        public Action<T> Function { get; set; }
    }
}