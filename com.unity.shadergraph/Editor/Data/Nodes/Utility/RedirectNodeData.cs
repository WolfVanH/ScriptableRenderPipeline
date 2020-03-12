using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using Edge = UnityEditor.Experimental.GraphView.Edge;

namespace UnityEditor.ShaderGraph
{
    class RedirectNodeData : AbstractMaterialNode
    {
        public Edge m_Edge;

        // Maybe think of this in reverse?
        SlotReference m_slotReferenceInput;
        public SlotReference slotReferenceInput
        {
            get => m_slotReferenceInput;
            set => m_slotReferenceInput = value;
        }

        SlotReference m_slotReferenceOutput;
        public SlotReference slotReferenceOutput
        {
            get => m_slotReferenceOutput;
            set => m_slotReferenceOutput = value;
        }

        public RedirectNodeData()
        {
            name = "Redirect Node";
        }

        RedirectNodeView m_nodeView;
        public RedirectNodeView nodeView
        {
            get { return m_nodeView; }
            set
            {
                if (value != m_nodeView)
                    m_nodeView = value;
            }
        }

        // Center the node's position?
        public void SetPosition(Vector2 pos)
        {
            var temp = drawState;
            temp.position = new Rect(pos, Vector2.zero);
            drawState = temp;
        }
        
        public SlotReference GetLeftMostSlotReference()
        {
            if (nodeView != null)
            {
                foreach (var port in nodeView.inputContainer.Children().OfType<ShaderPort>())
                {
                    var slot = port.slot;
                    var graph = slot.owner.owner;
                    var edges = graph.GetEdges(slot.slotReference).ToList();
                    foreach (IEdge edge in edges)
                    {
                        // get the input details
                        var outputSlotRef = edge.outputSlot;
                        var inputNode = graph.GetNodeFromGuid(outputSlotRef.nodeGuid);
                        // If this is a redirect node we continue to look for the top one
                        if (inputNode is RedirectNodeData redirectNode)
                        {
                            return redirectNode.GetLeftMostSlotReference();
                        }
                        // else we return the actual slot reference
                        return outputSlotRef;
                    }
                    // If no edges it is the first redirect node without an edge going into it and we should return the slot ref
                    return slot.slotReference;
                }
            }

            return new SlotReference();
        }
    }
}
