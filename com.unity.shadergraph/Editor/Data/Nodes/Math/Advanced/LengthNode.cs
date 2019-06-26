using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Advanced", "Length")]
    class LengthNode : CodeFunctionNode
    {
        public LengthNode()
        {
            name = "Length";
        }

        [HlslCodeGen]
        static void Unity_Length(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] out Float Out)
        {
            Out = length(In);
        }
    }
}
