using ZSG.Nodes;

namespace ZSG
{
    [NodeInfo("Math/DDX")]
    public class DDXNode : PasstroughNode
    {
        public override Precision DefaultPrecisionOverride => Precision.Float;

        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"ddx({PortData[IN].Name})");
        }
    }
}