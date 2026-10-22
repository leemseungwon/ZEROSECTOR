using System;

namespace LSW._02._Code.Common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SystemOrderAttribute : Attribute
    {
        public int Order { get; }

        public SystemOrderAttribute(int order)
        {
            Order = order;
        }
    }
    
    [AttributeUsage(AttributeTargets.Class)]
    public class ManagerOrderAttribute : Attribute
    {
        public int Order { get; }

        public ManagerOrderAttribute(int order)
        {
            Order = order;
        }
    }
}