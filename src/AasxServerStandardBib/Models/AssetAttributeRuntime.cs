using System;
using System.Collections.Generic;

namespace AasxServerStandardBib.Models
{
    public class AssetAttributeRuntime
    {
        public Guid? TriggerAttributeId { get; set; }
        public IEnumerable<Guid> TriggerAttributeIds { get; set; } = new List<Guid>();
        public string? Expression { get; set; }
        public bool? EnabledExpression { get; set; }
        public string? ExpressionCompile { get; set; }
    }
}
