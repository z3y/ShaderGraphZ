using ZSG.Nodes;

namespace ZSG
{
    [NodeInfo("DDY")]
    public class DDYNode : PasstroughNode
    {
        public override Precision DefaultPrecisionOverride => Precision.Float;

        protected override void Generate(NodeVisitor visitor)
        {
            base.Generate(visitor);
            Output(visitor, OUT, $"ddy({PortData[IN].Name})");
        }
    }

}