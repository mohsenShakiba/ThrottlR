using System.Collections.Generic;

namespace ThrottlR
{
    public class ThrottlePolicy
    {
        public ThrottlePolicy()
        {
            GeneralRules = new List<ThrottleRule>();
            SafeList = new List<string>();
            SpecificRules = new Dictionary<string, List<ThrottleRule>>();
            Resolver = NoResolver.Instance;
        }

        public List<ThrottleRule> GeneralRules { get; set; }

        public List<string> SafeList { get; set; } = new List<string>();

        public Dictionary<string, List<ThrottleRule>> SpecificRules { get; set; }

        public IResolver Resolver { get; set; }
    }
}
