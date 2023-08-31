using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NodeMaker
{
    public partial class Main : Form
    {

        #region Init

        int idCounter = 0;
        Dictionary<int, Node> nodes = new Dictionary<int, Node>();
        List<Connection> connections = new List<Connection>();
        List<int> selectionOrder = new List<int>();

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            initEdgeVals();
            renderAll();
        }

        #endregion

        #region Updates and Rendering

        #region Render Properties

        static readonly Color BackdropColor = Color.Black;
        static readonly Color EdgeColor = Color.Cyan;
        static readonly Color NodeColor = Color.DarkSlateGray;

        static readonly int NodeRadius = 20;

        Bitmap frameBuffer = new Bitmap(1, 1);

        Bitmap[] layers = new Bitmap[] { new Bitmap(1, 1), new Bitmap(1, 1), new Bitmap(1, 1) };

        #endregion

        #region Render Wrappers

        private void renderWrapper(RenderParams p, Action<RenderParams, Graphics> f, bool resize = false)
        {
            if (resize)
            {
                frameBuffer = new Bitmap(canvas.Width, canvas.Height);
                layers[(int)p.layer] = new Bitmap(canvas.Width, canvas.Height);
            }

            Graphics g = Graphics.FromImage(frameBuffer);

            f(p, Graphics.FromImage(layers[(int)p.layer]));

            foreach (Bitmap l in layers)
            {
                g.DrawImage(l, 0, 0);
            }

            canvas.Image = frameBuffer;
        }

        #endregion

        #region Edge Rendering Parameters

        int penAlpha;
        int penWidth;

        //const int maxAlpha = 100;
        const int maxAlpha = 255;
        const int maxWidth = 3;

        const int dropDistance = 330;

        double alphaGrad;
        double widthGrad;

        double NodeSeparation;

        private void initEdgeVals()
        {
            alphaGrad = (double)maxAlpha / dropDistance;
            widthGrad = (double)maxWidth / dropDistance;
        }

        #endregion

        #region Render Procedures

        private void drawNode(RenderParams p, Graphics g)
        {
            Point pos = p.mainNode.getPos();
            int left = pos.X - NodeRadius;
            int top = pos.Y - NodeRadius;
            int diameter = 2 * NodeRadius;

            if (p.mainNode.Selected())
            {
                p.color = SelectedNodeColor;
            }

            g.FillEllipse(new Pen(p.color).Brush, left, top, diameter, diameter);
        }

        private void drawScaledEdge(RenderParams p, Graphics g)
        {
            if (p.mainNode == p.secondaryNode)
            {
                return;
            }

            NodeSeparation = getDistance(p.mainNode.getPos(), p.secondaryNode.getPos());
            penAlpha = Math.Max(0, maxAlpha - (int)(alphaGrad * NodeSeparation));
            penWidth = Math.Max(0, maxWidth - (int)(widthGrad * NodeSeparation));

            if (penAlpha > 0 && penWidth > 0)
            {
                g.DrawLine(new Pen(Color.FromArgb(penAlpha, p.color), penWidth), p.mainNode.getPos(), p.secondaryNode.getPos());
            }
        }

        private void drawEdge(RenderParams p, Graphics g)
        {
            if (p.mainNode == p.secondaryNode)
            {
                return;
            }
            g.DrawLine(new Pen(Color.FromArgb(maxAlpha, p.color), maxWidth), p.mainNode.getPos(), p.secondaryNode.getPos());
        }

        private void drawNodeEdges(RenderParams p, Graphics g)
        {
            foreach (Connection c in connections)
            {
                if (c.Node1 == p.mainNode.id || c.Node2 == p.mainNode.id)
                {
                    if (nodes[c.Node1].Selected() || nodes[c.Node2].Selected())
                    {
                        drawEdge(new RenderParams(p.layer, SelectedEdgeColor, nodes[c.Node1], nodes[c.Node2]), g);
                    }
                    else
                    {
                        drawEdge(new RenderParams(p.layer, p.color, nodes[c.Node1], nodes[c.Node2]), g);
                    }
                }
            }
        }

        private void addNewNode(Point location)
        {
            Node n = new Node(idCounter, location);
            nodes.Add(idCounter, n);
            idCounter++;
            renderWrapper(new RenderParams(Layer.Nodes, NodeColor, n), drawNode);
        }

        private void removeNode(int id)
        {
            if (nodes[id].Selected())
            {
                selectionOrder.Remove(id);
            }

            nodes.Remove(id);

            List<Connection> MarkedConnections = new List<Connection>();

            foreach (Connection c in connections)
            {
                if (c.Node1 == id || c.Node2 == id)
                {
                    MarkedConnections.Add(c);
                }
            }

            foreach (Connection c in MarkedConnections)
            {
                connections.Remove(c);
            }

            renderAll();
        }

        private void bindFromPrimary()
        {
            if (selectionOrder.Count == 0)
            {
                return;
            }
            int PrimaryNodeId = selectionOrder[0];
            Connection c;
            foreach (int id in selectionOrder)
            {
                if (id == PrimaryNodeId)
                {
                    continue;
                }
                c = new Connection(PrimaryNodeId, id);
                if (!connections.Contains(c))
                {
                    connections.Add(c);
                }
                nodes[id].selectedState = State.None;
            }
            nodes[PrimaryNodeId].selectedState = State.None;
            selectionOrder.Clear();
            renderWrapper(new RenderParams(Layer.Edges, EdgeColor), renderAllEdges);
            renderWrapper(new RenderParams(Layer.Nodes, NodeColor), renderAllNodes);
        }

        private void bindSelectedNodes()
        {
            if (selectionOrder.Count == 0)
            {
                return;
            }
            Connection c;
            foreach (int id1 in selectionOrder)
            {
                foreach (int id2 in selectionOrder)
                {
                    if (id1 == id2)
                    {
                        continue;
                    }
                    c = new Connection(id1, id2);
                    if (!connections.Contains(c))
                    {
                        connections.Add(c);
                    }
                }
                nodes[id1].selectedState = State.None;
            }
            selectionOrder.Clear();
            
            renderWrapper(new RenderParams(Layer.Edges, EdgeColor), renderAllEdges);
            renderWrapper(new RenderParams(Layer.Nodes, NodeColor), renderAllNodes);
        }

        #region Total Renders

        private void renderBackdrop(RenderParams p, Graphics g)
        {
            g.Clear(p.color);
        }

        private void renderAllEdges(RenderParams p, Graphics g)
        {
            foreach (Connection c in connections)
            {
                if (nodes[c.Node1].Selected() || nodes[c.Node2].Selected())
                {
                    drawEdge(new RenderParams(p.layer, SelectedEdgeColor, nodes[c.Node1], nodes[c.Node2]), g);
                }
                else
                {
                    drawEdge(new RenderParams(p.layer, p.color, nodes[c.Node1], nodes[c.Node2]), g);
                }
            }
        }

        private void renderAllNodes(RenderParams p, Graphics g)
        {
            foreach (Node n in nodes.Values)
            {
                drawNode(new RenderParams(p.layer, p.color, n), g);
            }
        }

        private void renderAll()
        {
            renderWrapper(new RenderParams(Layer.Backdrop, BackdropColor), renderBackdrop, resize: true);
            renderWrapper(new RenderParams(Layer.Edges, EdgeColor), renderAllEdges, resize: true);
            renderWrapper(new RenderParams(Layer.Nodes, NodeColor), renderAllNodes, resize: true);
        }

        #endregion

        #endregion

        #endregion

        #region Calculations
        private int getDistance(Point p1, Point p2)
        {
            return (int)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }

        #endregion

        #region Form Events

        private void FormResize(object sender, EventArgs e)
        {
            renderAll();
        }

        private void FormClick(object sender, EventArgs e)
        {
            bool foundNode = false;
            List<int> idsToRemove = new List<int>();

            MouseEventArgs em = (MouseEventArgs)e;
            if (em.Button == MouseButtons.Right)
            {
                foreach (int id in nodes.Keys)
                {
                    if (nodes[id].hoverState == State.Hover)
                    {
                        idsToRemove.Add(id);
                    }
                }
                foreach (int id in idsToRemove)
                {
                    removeNode(id);
                }
                idsToRemove.Clear();
            }

            if (em.Button == MouseButtons.Left)
            {
                foreach (Node n in nodes.Values)
                {
                    if (n.hoverState == State.Hover)
                    {
                        n.changeSelectedState();
                        clickNode(n);
                        foundNode = true;
                    }
                }
                if (!foundNode)
                {
                    addNewNode(em.Location);
                }
            }

        }

        private void FormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                bindSelectedNodes();
            }
            if (e.KeyCode == Keys.B)
            {
                bindFromPrimary();
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            foreach (Node n in nodes.Values)
            {
                if (getDistance(e.Location, n.getPos()) <= NodeRadius)
                {
                    n.hoverState = State.Hover;
                }
                else
                {
                    n.hoverState = State.None;
                }

                if (n.hoverStateChanged() && !n.Selected())
                {
                    switch (n.hoverState)
                    {
                        case State.Hover:
                            hoverNode(n);
                            break;
                        case State.None:
                            dehoverNode(n);
                            break;
                    }
                }
            }
        }


        #endregion

        #region Node Selection

        readonly static Color HoverNodeColor = Color.Green;
        readonly static Color HoverEdgeColor = Color.Red;
        readonly static Color SelectedNodeColor = Color.Yellow;
        readonly static Color SelectedEdgeColor = Color.Orange;

        private void clickNode(Node n)
        {
            if (n.selectedState == State.Selected)
            {
                selectionOrder.Add(n.id);
                renderWrapper(new RenderParams(Layer.Nodes, SelectedNodeColor, n), drawNode);
                renderWrapper(new RenderParams(Layer.Edges, SelectedEdgeColor, n), drawNodeEdges);
            }
            else
            {
                selectionOrder.Remove(n.id);
                renderWrapper(new RenderParams(Layer.Nodes, NodeColor, n), drawNode);
                renderWrapper(new RenderParams(Layer.Edges, EdgeColor, n), drawNodeEdges);
            }
        }

        private void hoverNode(Node n)
        {
            renderWrapper(new RenderParams(Layer.Nodes, HoverNodeColor, n), drawNode);
            renderWrapper(new RenderParams(Layer.Edges, HoverEdgeColor, n), drawNodeEdges);
        }

        private void dehoverNode(Node n)
        {
            renderWrapper(new RenderParams(Layer.Nodes, NodeColor, n), drawNode);
            renderWrapper(new RenderParams(Layer.Edges, EdgeColor, n), drawNodeEdges);
        }

        
        #endregion

        public class Node
        {
            public int id;

            private DoubleVector location;

            public State selectedState;
            public State hoverState;
            public State oldHoverState;

            public Node(int _id, Point _location)
            {
                id = _id;
                setPos(_location);
                selectedState = State.None;
                hoverState = State.None;
                oldHoverState = State.None;
            }

            public Point getPos()
            {
                return new Point((int)location.X, (int)location.Y);
            }

            public void setPos(Point p)
            {
                location.X = p.X;
                location.Y = p.Y;
            }

            public bool hoverStateChanged()
            {
                if (hoverState != oldHoverState)
                {
                    oldHoverState = hoverState;
                    return true;
                }
                return false;
            }

            public void changeSelectedState()
            {
                switch(selectedState) 
                {
                    case State.None:
                        selectedState = State.Selected;
                        break;
                    case State.Selected:
                        selectedState = State.None;
                        break;
                }
            }

            public bool Selected()
            {
                return selectedState == State.Selected;
            }

        }

        #region Structs

        public struct DoubleVector
        {
            public double X;
            public double Y;
            public DoubleVector(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public struct RenderParams
        {
            public Node mainNode;
            public Node secondaryNode;
            public Color color;
            public Layer layer;

            public RenderParams(Layer l, Color c, Node m = null, Node s = null)
            {
                mainNode = m;
                secondaryNode = s;
                color = c;
                layer = l;
            }
        }

        public struct Connection
        {
            public int Node1;
            public int Node2;

            public Connection(int n1, int n2)
            {
                Node1 = n1;
                Node2 = n2;
            }
        }

        #endregion

        #region Enums

        public enum Layer
        {
            Backdrop,
            Edges,
            Nodes
        }

        public enum State
        {
            None,
            Hover,
            Selected
        }

        #endregion

    }
}

